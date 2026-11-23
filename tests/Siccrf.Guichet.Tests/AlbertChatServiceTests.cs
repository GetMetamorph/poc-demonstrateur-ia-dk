using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Siccrf.Guichet.Features.Conversation;
using Siccrf.Guichet.Features.Demandes;
using Xunit;

namespace Siccrf.Guichet.Tests;

public sealed class AlbertChatServiceTests : IDisposable
{
    private readonly AlbertTestApi _http = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly AlbertOptions _options = new() { ApiKey = "cle-de-test-factice" };

    [Fact]
    public async Task LeModeleEstVerifieEtLaCleResteDansLEnteteServeur()
    {
        _http.Catalogue();
        _http.Completion(AlbertTestApi.Proposition);
        var fiche = new FicheBrouillon();
        await Conversation().EnvoyerAsync(fiche, "IRIS Géo : double saisie", "modele-test", CancellationToken.None);

        Assert.Equal("IRIS Géo", fiche.Champs["application"]);
        Assert.Equal("Double saisie", fiche.Champs["difficultes"]);
        Assert.Equal(1, fiche.Revision);
        Assert.Equal(2, fiche.Messages.Count);
        Assert.Equal(["/v1/models", "/v1/chat/completions"], _http.Requetes.Select(r => r.Path));
        Assert.All(_http.Requetes, r => Assert.Equal("Bearer cle-de-test-factice", r.Authorization));
        Assert.DoesNotContain(_options.ApiKey, _http.Requetes[1].Body);
        using var body = JsonDocument.Parse(_http.Requetes[1].Body);
        Assert.False(body.RootElement.GetProperty("stream").GetBoolean());
        Assert.Equal("modele-test", body.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task LaListeExclutLesModelesQuiNeGenerentPasDuTexteEtEstMiseEnCache()
    {
        _http.Catalogue();
        var catalogue = Catalogue();
        Assert.Equal("modele-test", Assert.Single(await catalogue.DecouvrirAsync(CancellationToken.None)).Id);
        await catalogue.DecouvrirAsync(CancellationToken.None);
        Assert.Single(_http.Requetes);
    }

    [Fact]
    public async Task UnIdentifiantInventeNeDeclenchePasDeCompletion()
    {
        _http.Catalogue();
        var exception = await Assert.ThrowsAsync<ChatServiceException>(() =>
            Conversation().EnvoyerAsync(new FicheBrouillon(), "IRIS Géo", "modele-invente", CancellationToken.None));
        Assert.Equal("modele", exception.Code);
        Assert.Single(_http.Requetes);
    }

    [Fact]
    public async Task SansCleAucuneRequeteNestEmise()
    {
        _options.ApiKey = "";
        var exception = await Assert.ThrowsAsync<ChatServiceException>(() => Catalogue().DecouvrirAsync(CancellationToken.None));
        Assert.Equal("configuration", exception.Code);
        Assert.Empty(_http.Requetes);
    }

    [Theory]
    [InlineData("http://albert.api.etalab.gouv.fr/v1/")]
    [InlineData("https://user:password@albert.api.etalab.gouv.fr/v1/")]
    [InlineData("https://albert.api.etalab.gouv.fr/v1/?cle=inconnue")]
    public async Task UneAdresseNonSecuriseeNeRecoitPasLaCle(string adresse)
    {
        _options.BaseUrl = adresse;
        await Assert.ThrowsAsync<ChatServiceException>(() => Catalogue().DecouvrirAsync(CancellationToken.None));
        Assert.Empty(_http.Requetes);
    }

    [Fact]
    public async Task UneReponseMalFormeeEstRepareeUneFois()
    {
        _http.Catalogue();
        _http.Completion("pas du JSON");
        _http.Completion(AlbertTestApi.Proposition);
        var fiche = new FicheBrouillon();
        await Conversation().EnvoyerAsync(fiche, "IRIS Géo et double saisie", "modele-test", CancellationToken.None);
        Assert.Equal(3, _http.Requetes.Count);
        Assert.Equal(2, fiche.Messages.Count);
        Assert.Equal(1, fiche.Revision);
    }

    [Fact]
    public async Task DeuxReponsesInvalidesLaissentLaFicheEtLHistoriqueIntacts()
    {
        _http.Catalogue();
        _http.Completion("{}");
        _http.Completion("{}");
        var fiche = new FicheBrouillon();
        fiche.Champs["application"] = "Valeur conservée";
        await Assert.ThrowsAsync<ChatServiceException>(() =>
            Conversation().EnvoyerAsync(fiche, "IRIS Géo", "modele-test", CancellationToken.None));
        Assert.Equal("Valeur conservée", Assert.Single(fiche.Champs).Value);
        Assert.Empty(fiche.Messages);
        Assert.Equal(0, fiche.Revision);
        Assert.Equal(3, _http.Requetes.Count);
    }

    [Fact]
    public async Task UneReponseTronqueeNePeutPasMettreLaFicheAJour()
    {
        _http.Catalogue();
        _http.Completion(AlbertTestApi.Proposition, "length");
        _http.Completion(AlbertTestApi.Proposition, "length");
        var fiche = new FicheBrouillon();
        await Assert.ThrowsAsync<ChatServiceException>(() =>
            Conversation().EnvoyerAsync(fiche, "IRIS Géo et double saisie", "modele-test", CancellationToken.None));
        Assert.Empty(fiche.Champs);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "acces")]
    [InlineData(HttpStatusCode.Forbidden, "acces")]
    [InlineData(HttpStatusCode.NotFound, "modele")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "modele")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "indisponible")]
    public async Task LesErreursHttpNeDivulguentPasLeCorpsFournisseur(HttpStatusCode statut, string code)
    {
        _http.Ajouter("DONNEE_A_NE_PAS_DIVULGUER", statut);
        var exception = await Assert.ThrowsAsync<ChatServiceException>(() => Catalogue().DecouvrirAsync(CancellationToken.None));
        Assert.Equal(code, exception.Code);
        Assert.DoesNotContain("DONNEE_A_NE_PAS_DIVULGUER", exception.ToString());
        Assert.DoesNotContain(_options.ApiKey, exception.ToString());
    }

    [Fact]
    public async Task UneLimitationRespecteRetryAfterSansNouvelleTentativeAutomatique()
    {
        _http.Ajouter("{}", HttpStatusCode.TooManyRequests, 25);
        var exception = await Assert.ThrowsAsync<ChatServiceException>(() => Catalogue().DecouvrirAsync(CancellationToken.None));
        Assert.Equal(429, exception.Statut);
        Assert.Equal(25, exception.RetryAfter);
        Assert.Single(_http.Requetes);
    }

    [Fact]
    public async Task LAnnulationInterromptLAppelSansMuterLeBrouillon()
    {
        _http.Catalogue();
        _http.Bloquer();
        var fiche = new FicheBrouillon();
        using var annulation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Conversation().EnvoyerAsync(fiche, "IRIS Géo", "modele-test", annulation.Token));
        Assert.Empty(fiche.Champs);
        Assert.Empty(fiche.Messages);
    }

    [Fact]
    public async Task UneCorrectionRemplaceEtPeutEffacerUneValeurSansPerdreLesAutres()
    {
        _http.Catalogue();
        _http.Completion("""
            {"message":"D'accord, la date est retirée.","modifications":[
              {"champ":"application","valeur":"Nouveau service","source":"nouveau service"},
              {"champ":"echeance","valeur":null,"source":"retire la date"}]}
            """);
        var fiche = new FicheBrouillon();
        fiche.Champs["application"] = "Ancien outil";
        fiche.Champs["echeance"] = "2027";
        fiche.Champs["benefices"] = "Réduire les erreurs";
        await Conversation().EnvoyerAsync(fiche, "C'est un nouveau service et retire la date", "modele-test", CancellationToken.None);
        Assert.Equal("Nouveau service", fiche.Champs["application"]);
        Assert.Empty(fiche.Champs["echeance"]);
        Assert.Equal("Réduire les erreurs", fiche.Champs["benefices"]);
    }

    [Fact]
    public async Task LaFicheRecoitLaReformulationEtLeChatConserveLeMessageOriginal()
    {
        var texte = "ben on se renvoie les excel par mail et on sait plus lequel est le bon";
        _http.Catalogue();
        _http.Completion(JsonSerializer.Serialize(new
        {
            message = "Informations reformulées.",
            modifications = new[]
            {
                new { champ = "situationActuelle", valeur = "Le suivi repose sur l'échange de fichiers Excel par courriel.", source = "on se renvoie les excel par mail" },
                new { champ = "difficultes", valeur = "La version de référence des fichiers est difficile à identifier.", source = "on sait plus lequel est le bon" }
            }
        }));
        var fiche = new FicheBrouillon();
        await Conversation().EnvoyerAsync(fiche, texte, "modele-test", CancellationToken.None);

        Assert.Equal("Le suivi repose sur l'échange de fichiers Excel par courriel.", fiche.Champs["situationActuelle"]);
        Assert.Equal("La version de référence des fichiers est difficile à identifier.", fiche.Champs["difficultes"]);
        Assert.Equal(texte, fiche.Messages[0].Content);
        Assert.DoesNotContain("ben on", fiche.Champs["situationActuelle"]);
    }

    private AlbertCatalogue Catalogue() => new(new AlbertApiClient(_http, Options.Create(_options)), _cache);
    private ConversationService Conversation() => new(new AlbertChatService(
        new AlbertApiClient(_http, Options.Create(_options)), Catalogue(), new ChatPropositionLecteur()),
        new FicheRegles(), new EntretienGuide(new FicheRegles()));

    public void Dispose()
    {
        _http.Dispose();
        _cache.Dispose();
    }
}
