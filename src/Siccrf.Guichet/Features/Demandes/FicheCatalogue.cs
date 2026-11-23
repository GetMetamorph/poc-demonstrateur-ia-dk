namespace Siccrf.Guichet.Features.Demandes;

public static class FicheCatalogue
{
    // Les obligations sont des décisions du POC : le Word ne porte aucun astérisque.
    public static IReadOnlyList<FicheChamp> Champs { get; } = Array.AsReadOnly<FicheChamp>(
    [
        new("application", "Identification du besoin", "Application ou service numérique concerné",
            "Nom de l'application, ou « nouveau service ».", true, 200),
        new("intitule", "Identification du besoin", "Intitulé court de la demande",
            "Un titre qui décrit le besoin.", true, 150),
        new("synthese", "1. Synthèse de la demande", "Résumé en quelques lignes",
            "Problème, objectif et bénéfice principal.", true),
        new("situationActuelle", "2. Contexte et problème à résoudre", "Situation actuelle",
            "Comment le travail est réalisé aujourd'hui.", true),
        new("difficultes", "2. Contexte et problème à résoudre", "Difficultés rencontrées",
            "Limites, tâches manuelles et conséquences concrètes.", true),
        new("exemple", "2. Contexte et problème à résoudre", "Exemple représentatif",
            "Facultatif. Sans données sensibles inutiles.", false),
        new("resultat", "3. Objectifs et résultats attendus", "Résultat recherché",
            "Ce qui doit devenir possible ou plus simple.", true),
        new("criteresReussite", "3. Objectifs et résultats attendus", "Critères de réussite",
            "Comment constater que le besoin est satisfait. Aucun chiffre n'est imposé.", true),
        new("solution", "3. Objectifs et résultats attendus", "Solution envisagée par le demandeur",
            "Facultatif. Une piste, sans prescription technique.", false),
        new("beneficiaires", "4. Périmètre et bénéficiaires", "Utilisateurs ou services concernés",
            "Population concernée ; volumétrie seulement si connue.", true),
        new("processus", "4. Périmètre et bénéficiaires", "Processus métier concerné",
            "L'activité ou la tâche concernée.", true),
        new("liens", "4. Périmètre et bénéficiaires", "Liens avec d'autres outils ou demandes",
            "À préciser uniquement si des liens sont connus.", false),
        new("benefices", "5. Bénéfices, échéance et contraintes", "Bénéfices attendus",
            "Temps, qualité, fiabilité, conformité ou service rendu.", true),
        new("echeance", "5. Bénéfices, échéance et contraintes", "Échéance souhaitée et justification",
            "À renseigner en cas de contrainte réelle, avec sa justification.", false),
        new("contraintes", "5. Bénéfices, échéance et contraintes", "Contraintes identifiées",
            "Réglementation, sécurité, données, organisation, accessibilité, si connues.", false),
        new("references", "6. Pièces et précisions complémentaires", "Documents joints ou références utiles",
            "Facultatif. Références textuelles uniquement pour ce POC.", false),
        new("pointsAClarifier", "6. Pièces et précisions complémentaires", "Points restant à clarifier",
            "Incertitudes identifiées ; cette rubrique ne bloque pas à elle seule la transmission.", false)
    ]);
}
