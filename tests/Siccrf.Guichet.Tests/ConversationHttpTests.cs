using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Siccrf.Guichet.Tests;

public sealed class ConversationHttpTests
{
    [Fact]
    public async Task UnMessageMetAJourLaFicheEtUneCorrectionEstTransmiseAuTourSuivant()
    {
        using var api = new AlbertTestApi();
        api.Catalogue();
        api.Completion(AlbertTestApi.Proposition);
        api.Completion("""{"message":"Quel résultat attendez-vous ?","modifications":[]}""");
        using var app = Application(api);
        using var client = app.CreateClient();
        var token = await Jeton(client);

        var catalogue = await client.GetFromJsonAsync<JsonElement>("/conversation/modeles");
        Assert.Equal("modele-test", catalogue.GetProperty("modeles")[0].GetProperty("id").GetString());
        using var reponse = await Envoyer(client, token, "IRIS Géo : double saisie", 0);
        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        var etat = await reponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, etat.GetProperty("revision").GetInt64());
        Assert.Equal(8, etat.GetProperty("manquants").GetArrayLength());

        using var correction = await client.PostAsync("/demande/corriger", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["revision"] = "1", ["champs[application]"] = "Nouveau service"
        }));
        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
        using var suite = await Envoyer(client, token, "Pour tous les agents", 2);
        Assert.Equal(HttpStatusCode.OK, suite.StatusCode);
        using var requete = JsonDocument.Parse(api.Requetes[^1].Body);
        Assert.Contains("Nouveau service", requete.RootElement.GetProperty("messages")[1].GetProperty("content").GetString());
        Assert.Contains("IRIS Géo", WebUtility.HtmlDecode(await client.GetStringAsync("/")));
    }

    [Fact]
    public async Task UnChoixDeModeleEstVerifieEtUneRevisionPerimeeEstRefusee()
    {
        using var api = new AlbertTestApi();
        api.Catalogue();
        using var app = Application(api, modele: "");
        using var client = app.CreateClient();
        var token = await Jeton(client);
        using var choix = await Poster(client, token, "/conversation/modele", new { modele = "modele-test", revision = 0 });
        Assert.Equal(HttpStatusCode.OK, choix.StatusCode);
        using var conflit = await Envoyer(client, token, "IRIS Géo", 0);
        Assert.Equal(HttpStatusCode.Conflict, conflit.StatusCode);
        var contenu = await conflit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, contenu.GetProperty("etat").GetProperty("revision").GetInt64());
        Assert.Single(api.Requetes);
    }

    [Fact]
    public async Task DeuxMessagesConcurrentsDeMemeRevisionNeSecrasentPas()
    {
        using var api = new AlbertTestApi();
        api.Catalogue();
        api.Completion(AlbertTestApi.Proposition);
        using var app = Application(api);
        using var client = app.CreateClient();
        var token = await Jeton(client);
        var reponses = await Task.WhenAll(
            Envoyer(client, token, "IRIS Géo : double saisie", 0),
            Envoyer(client, token, "IRIS Géo : double saisie", 0));
        try
        {
            Assert.Equal(1, reponses.Count(r => r.StatusCode == HttpStatusCode.OK));
            Assert.Equal(1, reponses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
            Assert.Equal(2, api.Requetes.Count);
        }
        finally
        {
            foreach (var reponse in reponses) reponse.Dispose();
        }
    }

    [Fact]
    public async Task UnJsonInvalidePreserveLaFicheEtNeCreePasDeTourFantome()
    {
        using var api = new AlbertTestApi();
        api.Catalogue();
        api.Completion("{}");
        api.Completion("{}");
        using var app = Application(api);
        using var client = app.CreateClient();
        var token = await Jeton(client);
        using var reponse = await Envoyer(client, token, "IRIS Géo", 0);
        Assert.Equal(HttpStatusCode.BadGateway, reponse.StatusCode);
        var page = await client.GetStringAsync("/");
        Assert.Contains("0 / 10", page);
        Assert.DoesNotContain("class=\"history-message", page);
        Assert.Contains("data-revision value=\"0\"", page);
    }

    [Fact]
    public async Task UneLimitationEstRenvoyeeAvecRetryAfterEtSansDonneeTechnique()
    {
        using var api = new AlbertTestApi();
        api.Ajouter("SECRET_FOURNISSEUR", HttpStatusCode.TooManyRequests, 12);
        using var app = Application(api);
        using var client = app.CreateClient();
        var token = await Jeton(client);
        using var reponse = await Envoyer(client, token, "IRIS Géo", 0);
        Assert.Equal(HttpStatusCode.TooManyRequests, reponse.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(12), reponse.Headers.RetryAfter!.Delta);
        Assert.DoesNotContain("SECRET_FOURNISSEUR", await reponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LeBudgetDeDelaiRetourne504EtConserveLeBrouillon()
    {
        using var api = new AlbertTestApi();
        api.Bloquer();
        using var app = Application(api, delai: "1");
        using var client = app.CreateClient();
        var token = await Jeton(client);
        using var reponse = await Envoyer(client, token, "IRIS Géo", 0);
        Assert.Equal(HttpStatusCode.GatewayTimeout, reponse.StatusCode);
        Assert.Contains("0 / 10", await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task LaCleAbsenteLaisseLInterfaceAccessibleEtNeSortPasDuServeur()
    {
        using var api = new AlbertTestApi();
        using var app = Application(api, cle: "");
        using var client = app.CreateClient();
        var page = await client.GetStringAsync("/");
        Assert.Contains("data-configure=\"false\"", page);
        using var reponse = await client.GetAsync("/conversation/modeles");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, reponse.StatusCode);
        Assert.Empty(api.Requetes);
    }

    [Fact]
    public async Task LeChatRefuseUnPostSansAntiforgeryEtUnMessageVide()
    {
        using var api = new AlbertTestApi();
        using var app = Application(api);
        using var client = app.CreateClient();
        using var sansJeton = await client.PostAsJsonAsync("/conversation/messages", new { texte = "IRIS", revision = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, sansJeton.StatusCode);
        var token = await Jeton(client);
        using var vide = await Envoyer(client, token, "  ", 0);
        Assert.Equal(HttpStatusCode.BadRequest, vide.StatusCode);
        Assert.Empty(api.Requetes);
    }

    [Fact]
    public async Task LAdministrateurNePeutPasUtiliserLeChatEtLesSessionsSontIsolees()
    {
        using var api = new AlbertTestApi();
        api.Catalogue();
        api.Completion(AlbertTestApi.Proposition);
        using var app = Application(api);
        using var client = app.CreateClient();
        var token = await Jeton(client);
        using var message = await Envoyer(client, token, "IRIS Géo : double saisie", 0);
        Assert.Equal(HttpStatusCode.OK, message.StatusCode);
        using var autreClient = app.CreateClient();
        Assert.Contains("0 / 10", await autreClient.GetStringAsync("/"));
        using var changement = await client.PostAsync("/compte-demo/changer", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token, ["role"] = "administrateur"
        }));
        using var interdit = await Envoyer(client, token, "IRIS Géo", 1);
        Assert.Equal(HttpStatusCode.Forbidden, interdit.StatusCode);
        Assert.Equal(2, api.Requetes.Count);
    }

    [Fact]
    public async Task UneFicheCompleteEtUnOuiNeSoumettentRienALetapeDeux()
    {
        using var api = new AlbertTestApi();
        api.Catalogue();
        api.Completion("""{"message":"Vous pouvez relire la fiche.","modifications":[]}""");
        using var app = Application(api);
        using var client = app.CreateClient();
        var token = await Jeton(client);
        var donnees = new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["revision"] = "0" };
        foreach (var champ in Siccrf.Guichet.Features.Demandes.FicheCatalogue.Champs.Where(c => c.Obligatoire))
            donnees[$"champs[{champ.Cle}]"] = $"Description métier pour {champ.Libelle}";
        using var correction = await client.PostAsync("/demande/corriger", new FormUrlEncodedContent(donnees));
        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
        using var oui = await Envoyer(client, token, "oui", 1);
        Assert.Equal(HttpStatusCode.OK, oui.StatusCode);
        using var soumission = await client.PostAsync("/demande/soumettre", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Conflict, soumission.StatusCode);
    }

    [Fact]
    public async Task LeRecueilContinueApresUneSyntheseJusquaTousLesChampsObligatoires()
    {
        using var api = new AlbertTestApi();
        api.Catalogue();
        var debut = "Il me faut un suivi centralisé des dossiers.";
        static string Proposition(string champ, string valeur) => JsonSerializer.Serialize(new
        {
            message = "OK, auriez-vous d'autres informations ?",
            modifications = new[] { new { champ, valeur, source = valeur } }
        });
        api.Completion(Proposition("synthese", debut));
        var reponses = new (string Champ, string Texte)[]
        {
            ("application", "Nouveau service"),
            ("situationActuelle", "Chaque bureau tient un tableau Excel et l'envoie par mail."),
            ("difficultes", "Les versions divergent et des dossiers sont oubliés."),
            ("resultat", "Partager un suivi unique accessible aux bureaux."),
            ("beneficiaires", "Les agents chargés du suivi et les chefs de bureau."),
            ("processus", "Le suivi de l'instruction des dossiers."),
            ("criteresReussite", "Un agent retrouve le statut à jour sans demander un tableau par mail."),
            ("benefices", "Moins de relances et une information plus fiable."),
            ("intitule", "Centraliser le suivi des dossiers")
        };
        foreach (var reponse in reponses)
            api.Completion(Proposition(reponse.Champ, reponse.Texte));

        using var app = Application(api);
        using var client = app.CreateClient();
        var token = await Jeton(client);
        using var premier = await Envoyer(client, token, debut, 0);
        Assert.Equal(HttpStatusCode.OK, premier.StatusCode);
        var etat = await premier.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(9, etat.GetProperty("manquants").GetArrayLength());
        Assert.Equal("application", etat.GetProperty("question").GetProperty("champ").GetString());
        var page = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        Assert.Contains("À préciser : Application ou service numérique concerné", page);

        foreach (var (champ, texte) in reponses)
        {
            Assert.Equal(champ, etat.GetProperty("question").GetProperty("champ").GetString());
            using var reponse = await Envoyer(client, token, texte, etat.GetProperty("revision").GetInt64());
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            etat = await reponse.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(texte, etat.GetProperty("champs").GetProperty(champ).GetString());
            Assert.DoesNotContain("autres informations", etat.GetProperty("messages").EnumerateArray().Last().GetProperty("content").GetString());
        }

        Assert.Equal(0, etat.GetProperty("manquants").GetArrayLength());
        Assert.Equal(JsonValueKind.Null, etat.GetProperty("question").ValueKind);
        Assert.Contains("Relisez", etat.GetProperty("messages").EnumerateArray().Last().GetProperty("content").GetString());
        Assert.Equal(11, api.Requetes.Count);

        // Le dernier message court reste rattaché au titre, même après retrait des anciens tours.
        using var requete = JsonDocument.Parse(api.Requetes[^1].Body);
        var contexte = requete.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!;
        using var contexteJson = JsonDocument.Parse(contexte[contexte.IndexOf('{')..]);
        Assert.Equal("intitule", contexteJson.RootElement.GetProperty("questionEnCours").GetProperty("champ").GetString());
    }

    private static WebApplicationFactory<Program> Application(AlbertTestApi api, string cle = "cle-factice-tests-http",
        string modele = "modele-test", string delai = "5") => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Development");
        builder.UseContentRoot(Path.GetFullPath("../../../../../src/Siccrf.Guichet", AppContext.BaseDirectory));
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Albert:ApiKey"] = cle, ["Albert:Model"] = modele, ["Albert:TimeoutSeconds"] = delai
        }));
        builder.ConfigureServices(services => services.AddSingleton<IHttpClientFactory>(api));
    });

    private static async Task<string> Jeton(HttpClient client)
    {
        var html = await client.GetStringAsync("/");
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success);
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static Task<HttpResponseMessage> Envoyer(HttpClient client, string token, string texte, long revision) =>
        Poster(client, token, "/conversation/messages", new { texte, revision });

    private static async Task<HttpResponseMessage> Poster(HttpClient client, string token, string path, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Add("RequestVerificationToken", token);
        return await client.SendAsync(request);
    }
}
