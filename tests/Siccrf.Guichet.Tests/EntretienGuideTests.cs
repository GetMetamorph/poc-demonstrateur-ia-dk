using Siccrf.Guichet.Features.Conversation;
using Siccrf.Guichet.Features.Demandes;
using Xunit;

namespace Siccrf.Guichet.Tests;

public sealed class EntretienGuideTests
{
    private readonly FicheRegles _regles = new();
    private readonly EntretienGuide _guide = new(new FicheRegles());

    [Fact]
    public void LeGuideCouvreTousLesChampsObligatoiresEtAucuneRubriqueFacultative()
    {
        var fiche = new FicheBrouillon();
        var demandes = new HashSet<string>();
        while (!_regles.EstComplete(fiche))
        {
            var question = _guide.QuestionSuivante(fiche);
            Assert.NotNull(question);
            Assert.True(demandes.Add(question.Champ), "Un champ renseigné ne doit plus être demandé.");
            Assert.Contains('?', question.Texte);
            Assert.DoesNotContain("autre information", question.Texte, StringComparison.OrdinalIgnoreCase);
            fiche.Champs[question.Champ] = $"Information exprimée pour {question.Libelle}";
            fiche.QuestionEnCours = question;
        }

        Assert.Equal(FicheCatalogue.Champs.Where(c => c.Obligatoire).Select(c => c.Cle).Order(), demandes.Order());
        Assert.Null(_guide.QuestionSuivante(fiche));
        Assert.Equal(10, demandes.Count);
    }

    [Fact]
    public void LesQuestionsDeFondPrecedentLeTitreEtLaSyntheseSiCesDerniersManquent()
    {
        var fiche = new FicheBrouillon();
        fiche.Champs["application"] = "IRIS Géo";
        Assert.Equal("situationActuelle", _guide.QuestionSuivante(fiche)!.Champ);
    }

    [Fact]
    public void UnChampEncoreInconnuRecoitUneQuestionDePrecision()
    {
        var fiche = new FicheBrouillon();
        var initiale = _guide.QuestionSuivante(fiche)!;
        fiche.QuestionEnCours = initiale;
        fiche.Champs["application"] = "À préciser";

        var precision = _guide.QuestionSuivante(fiche)!;
        Assert.Equal(initiale.Champ, precision.Champ);
        Assert.NotEqual(initiale.Texte, precision.Texte);
        Assert.False(_regles.EstComplete(fiche));
    }

    [Fact]
    public void UnCritereQualitatifSuffitEtLesChampsFacultatifsVidesNeProvoquentPasDeRelance()
    {
        var fiche = Complete();
        fiche.Champs["criteresReussite"] = "Les agents retrouvent un dossier sans ressaisir son numéro.";
        fiche.QuestionEnCours = new("criteresReussite", "Critères de réussite", "Quel résultat observable attendez-vous ?");

        var reponse = _guide.PreparerReponse(fiche);
        Assert.Null(fiche.QuestionEnCours);
        Assert.Contains("Relisez", reponse);
        Assert.Contains("Confirmez-vous", reponse);
        Assert.True(ConfirmationDemande.EnAttente(fiche));
    }

    [Fact]
    public void UneCorrectionQuiEffaceUnChampObligatoireReouvreUneQuestionCiblee()
    {
        var fiche = Complete();
        _guide.PreparerReponse(fiche);
        Assert.True(_regles.Appliquer(fiche, new Dictionary<string, string?> { ["beneficiaires"] = null }, out _));
        var reponse = _guide.PreparerReponse(fiche);
        Assert.Equal("beneficiaires", fiche.QuestionEnCours!.Champ);
        Assert.Contains("Qui", reponse);
    }

    [Fact]
    public async Task UneRelanceGeneriqueDAlbertEstRemplaceeParUneQuestionSurLeChampManquant()
    {
        var fiche = new FicheBrouillon();
        var chat = new ChatSimule(new("D'accord, auriez-vous d'autres informations ?",
            [new("synthese", "Simplifier une tâche manuelle.", "tâche manuelle")]));
        var service = new ConversationService(chat, _regles, _guide);
        await service.EnvoyerAsync(fiche, "Je veux simplifier une tâche manuelle", "test", CancellationToken.None);

        Assert.Equal("application", fiche.QuestionEnCours!.Champ);
        Assert.Equal(fiche.QuestionEnCours.Texte, fiche.Messages[^1].Content);
        Assert.DoesNotContain("autres informations", fiche.Messages[^1].Content);
        Assert.Equal(9, _regles.Manquants(fiche).Count);
    }

    [Fact]
    public async Task UneReponseQuiRenseignePlusieursChampsLesFaitTousSauterDansLEntretien()
    {
        var fiche = new FicheBrouillon();
        var chat = new ChatSimule(new("Informations analysées.",
        [
            new("application", "IRIS Géo", "IRIS Géo"),
            new("situationActuelle", "Export manuel.", "export manuel"),
            new("difficultes", "Double saisie.", "double saisie")
        ]));
        await new ConversationService(chat, _regles, _guide).EnvoyerAsync(fiche,
            "IRIS Géo : export manuel et double saisie", "test", CancellationToken.None);

        Assert.Equal("resultat", fiche.QuestionEnCours!.Champ);
        Assert.Equal(7, _regles.Manquants(fiche).Count);
    }

    [Fact]
    public async Task LaQuestionExactePoseeEstTransmisePourInterpreterLaReponseCourte()
    {
        var fiche = new FicheBrouillon();
        fiche.QuestionEnCours = _guide.QuestionSuivante(fiche);
        var chat = new ChatSimule(new("Information comprise.", [new("application", "IRIS Géo", "IRIS Géo")]));
        await new ConversationService(chat, _regles, _guide).EnvoyerAsync(fiche, "IRIS Géo", "test", CancellationToken.None);

        Assert.Equal("application", chat.Contexte!.QuestionEnCours!.Champ);
        Assert.Equal("situationActuelle", fiche.QuestionEnCours!.Champ);
        var prompt = ChatPrompt.Construire(chat.Contexte, false);
        Assert.Contains("questionEnCours", prompt[1].Content);
        Assert.Contains("application", prompt[1].Content);
    }

    [Fact]
    public async Task UneReponseSansInformationNInventeraPasUneValeurPourAvancer()
    {
        var fiche = new FicheBrouillon();
        fiche.QuestionEnCours = _guide.QuestionSuivante(fiche);
        var premiere = fiche.QuestionEnCours;
        var chat = new ChatSimule(new("Information inconnue.", []));
        await new ConversationService(chat, _regles, _guide).EnvoyerAsync(fiche, "Je ne sais pas", "test", CancellationToken.None);

        Assert.Empty(fiche.Champs);
        Assert.Equal(premiere!.Champ, fiche.QuestionEnCours!.Champ);
        Assert.NotEqual(premiere.Texte, fiche.Messages[^1].Content);
    }

    [Fact]
    public async Task UneCorrectionSurUnAutreSujetNestPasCopieeDansLeChampEnCours()
    {
        var fiche = new FicheBrouillon();
        fiche.Champs["application"] = "IRIS Géo";
        fiche.QuestionEnCours = _guide.QuestionSuivante(fiche);
        var chat = new ChatSimule(new("Correction prise en compte.", [new("application", "Nouveau service", "nouveau service")]));
        await new ConversationService(chat, _regles, _guide).EnvoyerAsync(fiche, "En fait, c'est un nouveau service", "test", CancellationToken.None);

        Assert.Equal("Nouveau service", fiche.Champs["application"]);
        Assert.False(fiche.Champs.ContainsKey("situationActuelle"));
        Assert.Equal("situationActuelle", fiche.QuestionEnCours!.Champ);
    }

    private static FicheBrouillon Complete() => new()
    {
        Champs = FicheCatalogue.Champs.Where(c => c.Obligatoire).ToDictionary(c => c.Cle, c => $"Information pour {c.Libelle}")
    };

    private sealed class ChatSimule(ChatProposition proposition) : IChatService
    {
        public ChatContexte? Contexte { get; private set; }
        public Task<ChatProposition> RepondreAsync(ChatContexte contexte, string modele, CancellationToken cancellationToken)
        {
            Contexte = contexte;
            return Task.FromResult(proposition);
        }
    }
}
