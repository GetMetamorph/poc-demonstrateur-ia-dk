using Siccrf.Guichet.Features.Demandes;

namespace Siccrf.Guichet.Features.Conversation;

public sealed class ConversationService(IChatService chat, FicheRegles regles, EntretienGuide entretien)
{
    public async Task EnvoyerAsync(FicheBrouillon fiche, string texte, string modele,
        CancellationToken cancellationToken)
    {
        if (fiche.DemandeSoumiseId is not null)
            throw new DemandeException("Cette demande est déjà transmise. Créez une nouvelle demande pour poursuivre.");
        var contexte = new ChatContexte(fiche.Champs, regles.Manquants(fiche).Select(c => c.Cle).ToArray(),
            fiche.Messages, texte, entretien.QuestionCourante(fiche));
        var proposition = await chat.RepondreAsync(contexte, modele, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var changements = proposition.Modifications.ToDictionary(m => m.Champ, m => m.Valeur);
        if (!regles.Appliquer(fiche, changements, out _))
            throw AlbertApiClient.ReponseInvalide();

        ConfirmationDemande.Annuler(fiche);
        fiche.Revision++;
        // Ne jamais afficher la relance libre du modèle : le guide couvre les champs
        // réellement manquants, y compris si Albert ne propose qu'une synthèse.
        fiche.AjouterEchange(texte, entretien.PreparerReponse(fiche));
    }
}
