using Siccrf.Guichet.Features.Demandes;

namespace Siccrf.Guichet.Features.Conversation;

public sealed class ConversationService(IChatService chat, FicheRegles regles)
{
    public async Task EnvoyerAsync(FicheBrouillon fiche, string texte, string modele,
        CancellationToken cancellationToken)
    {
        var contexte = new ChatContexte(fiche.Champs, regles.Manquants(fiche).Select(c => c.Cle).ToArray(),
            fiche.Messages, texte);
        var proposition = await chat.RepondreAsync(contexte, modele, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var changements = proposition.Modifications.ToDictionary(m => m.Champ, m => m.Valeur);
        if (!regles.Appliquer(fiche, changements, out _))
            throw AlbertApiClient.ReponseInvalide();

        fiche.Messages.Add(new ChatMessage("user", texte));
        fiche.Messages.Add(new ChatMessage("assistant", proposition.Message));
        if (fiche.Messages.Count > 12)
            fiche.Messages.RemoveRange(0, fiche.Messages.Count - 12);
        fiche.Revision++;
    }
}
