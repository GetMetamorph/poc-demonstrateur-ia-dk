using Siccrf.Guichet.Features.Conversation;

namespace Siccrf.Guichet.Features.Demandes;

public sealed class FicheBrouillon
{
    public DateTimeOffset DateCreation { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string> Champs { get; init; } = new(StringComparer.Ordinal);
    public long Revision { get; set; }
    public string? ModeleAlbert { get; set; }
    public List<ChatMessage> Messages { get; init; } = [];
    public ChatQuestion? QuestionEnCours { get; set; }
}
