using Siccrf.Guichet.Features.Demandes;

namespace Siccrf.Guichet.Features.Conversation;

// Le choix des questions est déterministe. Albert extrait les informations ;
// le serveur décide de ce qu'il reste à demander après application des changements.
public sealed class EntretienGuide(FicheRegles regles)
{
    private sealed record Question(string Champ, string Texte, string Precision);

    private static readonly IReadOnlyList<Question> Questions = Array.AsReadOnly<Question>(
    [
        new("application",
            "Quelle application ou quel service numérique est concerné ? S'il s'agit d'un nouvel outil, vous pouvez simplement préciser « nouveau service ».",
            "Quel est le nom de l'application concernée, ou s'agit-il d'un nouveau service à créer ?"),
        new("situationActuelle",
            "Comment réalisez-vous ce travail aujourd'hui ? Décrivez les principales étapes et les outils utilisés.",
            "Pour décrire la situation actuelle, pouvez-vous raconter comment vous réalisez concrètement cette tâche aujourd'hui, du début à la fin ?"),
        new("difficultes",
            "Qu'est-ce qui vous pose problème dans cette façon de travailler, et quelles en sont les conséquences concrètes ?",
            "À quelle étape rencontrez-vous une difficulté : perte de temps, ressaisie, erreurs ou autre obstacle ? Expliquez ce qui se passe dans votre cas."),
        new("resultat",
            "Qu'aimeriez-vous pouvoir faire, ou faire plus simplement, une fois ce besoin satisfait ?",
            "Quel changement concret attendez-vous dans votre travail ? Décrivez le résultat souhaité, même si vous ne connaissez pas encore la solution technique."),
        new("beneficiaires",
            "Qui utilisera ce service ou bénéficiera de cette amélioration : quels agents, bureaux ou services ?",
            "Quels utilisateurs ou services sont concernés par ce besoin ? Leur nombre n'est nécessaire que si vous le connaissez."),
        new("processus",
            "Dans quelle activité ou quel processus métier ce besoin s'inscrit-il ? Par exemple, le suivi des dossiers ou la préparation des contrôles, selon votre situation.",
            "Quelle tâche métier cherchez-vous à améliorer ? Une description simple de l'activité concernée suffit."),
        new("criteresReussite",
            "Comment constaterez-vous que l'amélioration fonctionne ? Quel résultat observable vous permettrait de dire que le besoin est satisfait ?",
            "Lors d'un essai du futur service, que faudrait-il réussir à faire pour considérer le besoin satisfait ? Un constat qualitatif suffit, sans chiffre imposé."),
        new("benefices",
            "Quel bénéfice principal attendez-vous pour les utilisateurs ou le service : temps gagné, moins d'erreurs, meilleure fiabilité ou autre gain ?",
            "Qu'est-ce que cette amélioration apportera concrètement au travail des agents ou au service rendu ? Aucun gain chiffré n'est nécessaire."),
        new("intitule",
            "Quel titre court décrirait le mieux votre demande d'évolution ?",
            "Comment nommeriez-vous cette demande en quelques mots pour qu'un collègue en comprenne le sujet ?"),
        new("synthese",
            "Pour résumer votre demande, comment présenteriez-vous en quelques phrases le problème, le résultat recherché et le bénéfice principal ?",
            "Quelle formulation courte souhaitez-vous retenir pour résumer le problème rencontré, l'objectif et le principal bénéfice attendu ?")
    ]);

    public ChatQuestion? QuestionCourante(FicheBrouillon fiche) =>
        fiche.QuestionEnCours is { } question && regles.Manquants(fiche).Any(c => c.Cle == question.Champ)
            ? question : null;

    public ChatQuestion? QuestionSuivante(FicheBrouillon fiche)
    {
        var manquants = regles.Manquants(fiche);
        if (manquants.Count == 0)
            return null;

        // Une réponse ambiguë ne remplit pas arbitrairement la rubrique : la préciser.
        var courante = QuestionCourante(fiche);
        var definition = courante is not null
            ? Questions.FirstOrDefault(q => q.Champ == courante.Champ)
            : Questions.FirstOrDefault(q => manquants.Any(c => c.Cle == q.Champ));
        var champ = manquants.First(c => c.Cle == (definition?.Champ ?? courante?.Champ ?? manquants[0].Cle));
        var texte = definition is null
            ? $"Pour préciser « {champ.Libelle} », pouvez-vous décrire l'information suivante : {champ.Aide}"
            : courante is null ? definition.Texte : definition.Precision;
        return new ChatQuestion(champ.Cle, champ.Libelle, texte);
    }

    public string PreparerReponse(FicheBrouillon fiche)
    {
        fiche.QuestionEnCours = QuestionSuivante(fiche);
        if (fiche.QuestionEnCours is not null)
        {
            ConfirmationDemande.Annuler(fiche);
            return fiche.QuestionEnCours.Texte;
        }
        return ConfirmationDemande.Preparer(fiche, regles, dansChat: true);
    }
}
