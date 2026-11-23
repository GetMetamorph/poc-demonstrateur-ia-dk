using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Siccrf.Guichet.Features.Demandes;
using Xunit;

namespace Siccrf.Guichet.Tests;

public sealed class SoumissionHttpTests
{
    [Fact]
    public async Task UneFicheIncompleteNePeutPasPasserALaConfirmation()
    {
        using var stockage = new StockageSqliteTest();
        using var app = Application(stockage);
        using var client = app.CreateClient();
        var page = await client.GetStringAsync("/");
        using var resultat = await client.PostAsync("/demande/preparer", Formulaire(Jeton(page), ("revision", "0")));
        Assert.Equal(HttpStatusCode.BadRequest, resultat.StatusCode);
        await using var db = await stockage.OuvrirAsync();
        Assert.Equal(0, await db.Demandes.CountAsync());
    }

    [Fact]
    public async Task LaRelectureNeVautPasConfirmationEtTouteCorrectionInvalideLaVersionRelue()
    {
        using var stockage = new StockageSqliteTest();
        using var app = Application(stockage);
        using var client = app.CreateClient();
        var page = await Remplir(client);
        var token = Jeton(page);
        var relecture = await Preparer(client, token, Revision(page));
        Assert.Contains("Je confirme que cette fiche", relecture);
        var id = Valeur(relecture, "brouillonId");
        var revision = Revision(relecture);
        using var sansAccord = await client.PostAsync("/demande/soumettre", Formulaire(token,
            ("brouillonId", id), ("revision", revision), ("confirme", "false")));
        Assert.Equal(HttpStatusCode.Conflict, sansAccord.StatusCode);

        using var correction = await client.PostAsync("/demande/corriger", Formulaire(token,
            ("revision", revision), ("champs[intitule]", "Titre corrigé")));
        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
        using var ancienne = await client.PostAsync("/demande/soumettre", Formulaire(token,
            ("brouillonId", id), ("revision", revision), ("confirme", "true")));
        Assert.Equal(HttpStatusCode.Conflict, ancienne.StatusCode);
        await using var db = await stockage.OuvrirAsync();
        Assert.Equal(0, await db.Demandes.CountAsync());
    }

    [Fact]
    public async Task LeParcoursBoutonPuisAdministrationPersisteEtValideLaDemande()
    {
        using var stockage = new StockageSqliteTest();
        using var app = Application(stockage);
        using var client = app.CreateClient();
        var page = await Remplir(client);
        var token = Jeton(page);
        var relecture = await Preparer(client, token, Revision(page));
        using var resultat = await Soumettre(client, token, relecture);
        Assert.Equal(HttpStatusCode.OK, resultat.StatusCode);
        Assert.Contains("EB-", await resultat.Content.ReadAsStringAsync());
        await using var db = await stockage.OuvrirAsync();
        var demande = await db.Demandes.AsNoTracking().SingleAsync();
        Assert.Equal(StatutsDemande.Soumise, demande.Statut);
        Assert.Equal("Bouton", demande.ModeConfirmation);

        // Le rôle demandeur ne peut ni consulter les autres fiches ni valider.
        using var interdit = await client.PostAsync($"/administration/demandes/{demande.Id}/valider", Formulaire(token));
        Assert.Equal(HttpStatusCode.Forbidden, interdit.StatusCode);
        using var detailInterdit = await client.GetAsync($"/administration/demandes/{demande.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, detailInterdit.StatusCode);

        using var autreClient = app.CreateClient();
        await Remplir(autreClient, "Brouillon non soumis à masquer");
        await Administrateur(client, token);
        var liste = WebUtility.HtmlDecode(await client.GetStringAsync("/administration"));
        Assert.Contains(demande.Reference, liste);
        Assert.DoesNotContain("Brouillon non soumis à masquer", liste);
        using var sansAntiforgery = await client.PostAsync($"/administration/demandes/{demande.Id}/valider", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, sansAntiforgery.StatusCode);
        using var validee = await client.PostAsync($"/administration/demandes/{demande.Id}/valider", Formulaire(token));
        Assert.Equal(HttpStatusCode.OK, validee.StatusCode);
        Assert.Contains("Validée", WebUtility.HtmlDecode(await validee.Content.ReadAsStringAsync()));
        var relue = await db.Demandes.AsNoTracking().SingleAsync();
        Assert.Equal(StatutsDemande.Validee, relue.Statut);
        Assert.NotNull(relue.DateValidationUtc);
        Assert.NotNull(relue.ValidePar);
    }

    [Fact]
    public async Task DoubleSoumissionEtAncienFormulaireApresNouvelleDemandeNeCreentPasDeDoublon()
    {
        using var stockage = new StockageSqliteTest();
        using var app = Application(stockage);
        using var client = app.CreateClient();
        var page = await Remplir(client);
        var token = Jeton(page);
        var relecture = await Preparer(client, token, Revision(page));
        using var premier = await Soumettre(client, token, relecture);
        using var second = await Soumettre(client, token, relecture);
        Assert.Equal(HttpStatusCode.OK, premier.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        using var correction = await client.PostAsync("/demande/corriger", Formulaire(token,
            ("revision", "2"), ("champs[intitule]", "Modification interdite")));
        Assert.Equal(HttpStatusCode.Conflict, correction.StatusCode);
        using var nouvelle = await client.PostAsync("/demande/nouvelle", Formulaire(token));
        Assert.Equal(HttpStatusCode.OK, nouvelle.StatusCode);
        Assert.Contains("0 / 10", await nouvelle.Content.ReadAsStringAsync());
        using var ancienFormulaire = await Soumettre(client, token, relecture);
        Assert.Equal(HttpStatusCode.Conflict, ancienFormulaire.StatusCode);
        await using var db = await stockage.OuvrirAsync();
        Assert.Equal(1, await db.Demandes.CountAsync());
        Assert.Equal("Centraliser le suivi des dossiers", (await db.Demandes.SingleAsync()).Intitule);
    }

    [Fact]
    public async Task LeChatSoumetUniquementApresSaQuestionExpliciteEtUnAccordAutonome()
    {
        using var stockage = new StockageSqliteTest();
        using var api = new AlbertTestApi();
        api.Catalogue();
        api.Completion("""{"message":"Fiche complète.","modifications":[]}""");
        api.Completion("""{"message":"Correction comprise.","modifications":[{"champ":"intitule","valeur":"Titre modifié","source":"le titre doit changer"}]}""");
        using var app = Application(stockage, api);
        using var client = app.CreateClient();
        var page = await Remplir(client);
        var token = Jeton(page);
        // Le premier oui répond à une autre question : il provoque la demande de confirmation, pas un envoi.
        using var avantQuestion = await Message(client, token, "oui", long.Parse(Revision(page)));
        var etat = await avantQuestion.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(etat.GetProperty("confirmationEnAttente").GetBoolean());
        Assert.Equal(JsonValueKind.Null, etat.GetProperty("demande").ValueKind);
        Assert.Contains("Confirmez-vous", etat.GetProperty("messages").EnumerateArray().Last().GetProperty("content").GetString());
        using var correction = await Message(client, token, "oui mais le titre doit changer", etat.GetProperty("revision").GetInt64());
        etat = await correction.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, etat.GetProperty("demande").ValueKind);
        Assert.Equal("Titre modifié", etat.GetProperty("champs").GetProperty("intitule").GetString());
        using var confirmation = await Message(client, token, "Je confirme", etat.GetProperty("revision").GetInt64());
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        etat = await confirmation.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(etat.GetProperty("demande").GetProperty("id").GetInt64() > 0);
        Assert.False(etat.GetProperty("confirmationEnAttente").GetBoolean());
        Assert.Equal(3, api.Requetes.Count); // La confirmation n'appelle pas le modèle.
        await using var db = await stockage.OuvrirAsync();
        var demande = await db.Demandes.SingleAsync();
        Assert.Equal("Chat", demande.ModeConfirmation);
        Assert.Equal("Titre modifié", demande.Intitule);
        using var apresEnvoi = await Message(client, token, "Autre modification", etat.GetProperty("revision").GetInt64());
        Assert.Equal(HttpStatusCode.Conflict, apresEnvoi.StatusCode);
    }

    [Fact]
    public async Task UnRefusDansLeChatNeSoumetPasEtAnnuleLaConfirmationEnAttente()
    {
        using var stockage = new StockageSqliteTest();
        using var api = new AlbertTestApi();
        api.Catalogue();
        api.Completion("""{"message":"Fiche complète.","modifications":[]}""");
        using var app = Application(stockage, api);
        using var client = app.CreateClient();
        var page = await Remplir(client);
        var token = Jeton(page);
        using var question = await Message(client, token, "Voilà", long.Parse(Revision(page)));
        var etat = await question.Content.ReadFromJsonAsync<JsonElement>();
        using var refus = await Message(client, token, "non", etat.GetProperty("revision").GetInt64());
        etat = await refus.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(etat.GetProperty("confirmationEnAttente").GetBoolean());
        Assert.Equal(JsonValueKind.Null, etat.GetProperty("demande").ValueKind);
        Assert.Equal(2, api.Requetes.Count);
        await using var db = await stockage.OuvrirAsync();
        Assert.Equal(0, await db.Demandes.CountAsync());
    }

    [Fact]
    public async Task LesDemandesRestentDisponiblesApresRedemarrageDuServeur()
    {
        using var stockage = new StockageSqliteTest();
        string reference;
        using (var app = Application(stockage))
        using (var client = app.CreateClient())
        {
            var page = await Remplir(client);
            var relecture = await Preparer(client, Jeton(page), Revision(page));
            using var soumission = await Soumettre(client, Jeton(page), relecture);
            Assert.Equal(HttpStatusCode.OK, soumission.StatusCode);
            await using var db = await stockage.OuvrirAsync();
            reference = (await db.Demandes.SingleAsync()).Reference;
        }
        using var redemarre = Application(stockage);
        using var admin = redemarre.CreateClient();
        var accueil = await admin.GetStringAsync("/");
        await Administrateur(admin, Jeton(accueil));
        Assert.Contains(reference, await admin.GetStringAsync("/administration"));
    }

    private static WebApplicationFactory<Program> Application(StockageSqliteTest stockage, AlbertTestApi? api = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseContentRoot(Path.GetFullPath("../../../../../src/Siccrf.Guichet", AppContext.BaseDirectory));
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Path"] = stockage.Chemin, ["Database:Pooling"] = "false",
                ["Albert:ApiKey"] = api is null ? "" : "cle-factice-tests", ["Albert:Model"] = "modele-test"
            }));
            if (api is not null) builder.ConfigureServices(services => services.AddSingleton<IHttpClientFactory>(api));
        });

    private static string Jeton(string html) => Valeur(html, "__RequestVerificationToken");
    private static string Revision(string html) => Valeur(html, "revision");
    private static string Valeur(string html, string nom) => WebUtility.HtmlDecode(Regex.Match(html,
        $"name=\"{Regex.Escape(nom)}\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
    private static FormUrlEncodedContent Formulaire(string token, params (string Cle, string Valeur)[] valeurs) =>
        new(valeurs.Select(v => new KeyValuePair<string, string>(v.Cle, v.Valeur))
            .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token)));

    private static async Task<string> Remplir(HttpClient client, string titre = "Centraliser le suivi des dossiers")
    {
        var page = await client.GetStringAsync("/");
        var champs = FicheCatalogue.Champs.Where(c => c.Obligatoire).Select(c =>
            ($"champs[{c.Cle}]", c.Cle == "intitule" ? titre : $"Information métier pour {c.Libelle}"))
            .Append(("revision", Revision(page))).ToArray();
        using var resultat = await client.PostAsync("/demande/corriger", Formulaire(Jeton(page), champs));
        resultat.EnsureSuccessStatusCode();
        return await resultat.Content.ReadAsStringAsync();
    }
    private static async Task<string> Preparer(HttpClient client, string token, string revision)
    {
        using var resultat = await client.PostAsync("/demande/preparer", Formulaire(token, ("revision", revision)));
        resultat.EnsureSuccessStatusCode();
        return await resultat.Content.ReadAsStringAsync();
    }
    private static Task<HttpResponseMessage> Soumettre(HttpClient client, string token, string relecture) =>
        client.PostAsync("/demande/soumettre", Formulaire(token, ("brouillonId", Valeur(relecture, "brouillonId")),
            ("revision", Revision(relecture)), ("confirme", "true")));
    private static Task<HttpResponseMessage> Administrateur(HttpClient client, string token) =>
        client.PostAsync("/compte-demo/changer", Formulaire(token, ("role", "administrateur")));
    private static async Task<HttpResponseMessage> Message(HttpClient client, string token, string texte, long revision)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/conversation/messages")
        {
            Content = JsonContent.Create(new { texte, revision })
        };
        message.Headers.Add("RequestVerificationToken", token);
        return await client.SendAsync(message);
    }
}
