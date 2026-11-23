using System.Text.Json;

namespace Siccrf.Guichet.Features.Conversation;

public sealed class AlbertChatService(
    AlbertApiClient api, AlbertCatalogue catalogue, ChatPropositionLecteur lecteur) : IChatService
{
    public async Task<ChatProposition> RepondreAsync(ChatContexte contexte, string modele,
        CancellationToken cancellationToken)
    {
        await catalogue.VerifierAsync(modele, cancellationToken);
        for (var tentative = 0; tentative < 2; tentative++)
        {
            using var reponse = await api.EnvoyerAsync("chat/completions", new
            {
                model = modele,
                stream = false,
                messages = ChatPrompt.Construire(contexte, tentative > 0)
                    .Select(m => new { role = m.Role, content = m.Content })
            }, cancellationToken);

            try
            {
                var contenu = ExtraireContenu(reponse.RootElement);
                return lecteur.Lire(contenu, contexte.Message);
            }
            catch (ChatServiceException exception) when (exception.Code == "reponse" && tentative == 0)
            {
                // Une seule réparation, dans le budget de délai commun aux deux appels.
            }
        }

        throw AlbertApiClient.ReponseInvalide();
    }

    private static string ExtraireContenu(JsonElement racine)
    {
        if (racine.ValueKind != JsonValueKind.Object ||
            !racine.TryGetProperty("choices", out var choix) || choix.ValueKind != JsonValueKind.Array ||
            choix.GetArrayLength() == 0)
            throw AlbertApiClient.ReponseInvalide();
        var premier = choix[0];
        if (premier.ValueKind != JsonValueKind.Object ||
            !premier.TryGetProperty("finish_reason", out var raison) || raison.ValueKind != JsonValueKind.String ||
            raison.GetString() != "stop" ||
            !premier.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object ||
            !message.TryGetProperty("content", out var contenu) || contenu.ValueKind != JsonValueKind.String)
            throw AlbertApiClient.ReponseInvalide();
        return contenu.GetString()!;
    }
}
