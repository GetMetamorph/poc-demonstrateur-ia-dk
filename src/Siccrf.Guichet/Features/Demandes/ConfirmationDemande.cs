namespace Siccrf.Guichet.Features.Demandes;

public static class ConfirmationDemande
{
    private static readonly HashSet<string> Accords = new(StringComparer.OrdinalIgnoreCase)
    {
        "oui", "je confirme", "je confirme la transmission", "oui je confirme", "oui, je confirme"
    };
    private static readonly HashSet<string> Refus = new(StringComparer.OrdinalIgnoreCase)
    {
        "non", "pas maintenant", "annuler", "je ne confirme pas"
    };

    public static bool EnAttente(FicheBrouillon fiche) =>
        fiche.DemandeSoumiseId is null && fiche.RevisionAConfirmer == fiche.Revision;

    public static bool EnAttenteDansChat(FicheBrouillon fiche) =>
        EnAttente(fiche) && fiche.RevisionConfirmationChat == fiche.Revision;

    public static bool EstAccord(string texte) => Accords.Contains(Normaliser(texte));
    public static bool EstRefus(string texte) => Refus.Contains(Normaliser(texte));

    public static string Preparer(FicheBrouillon fiche, FicheRegles regles, bool dansChat = false)
    {
        if (!regles.EstComplete(fiche) || fiche.DemandeSoumiseId is not null)
            throw new DemandeException("La fiche doit être complète et non transmise avant la confirmation.", 400);
        fiche.RevisionAConfirmer = fiche.Revision;
        fiche.RevisionConfirmationChat = dansChat ? fiche.Revision : null;
        return "Tous les champs obligatoires sont renseignés. Relisez la fiche avec « Afficher la fiche » ; vous pouvez encore corriger une information. " +
            "Confirmez-vous que cette fiche reflète fidèlement votre besoin et souhaitez-vous la transmettre à validation ? " +
            "Répondez « Je confirme » ou utilisez le bouton de relecture et de confirmation.";
    }

    public static void Annuler(FicheBrouillon fiche)
    {
        fiche.RevisionAConfirmer = null;
        fiche.RevisionConfirmationChat = null;
    }

    private static string Normaliser(string texte) => texte.Trim().TrimEnd('.', '!').Trim();
}
