namespace Siccrf.Guichet.Features.Conversation;

public sealed record ChatMessage(string Role, string Content);
public sealed record ChatModification(string Champ, string? Valeur, string Source);
public sealed record ChatProposition(string Message, IReadOnlyList<ChatModification> Modifications);
public sealed record ChatContexte(
    IReadOnlyDictionary<string, string> Champs,
    IReadOnlyList<string> Manquants,
    IReadOnlyList<ChatMessage> Historique,
    string Message);
public sealed record AlbertModele(string Id);

public interface IChatService
{
    Task<ChatProposition> RepondreAsync(ChatContexte contexte, string modele, CancellationToken cancellationToken);
}

public sealed class ChatServiceException(string code, string message, int statut = 503, int? retryAfter = null)
    : Exception(message)
{
    public string Code { get; } = code;
    public int Statut { get; } = statut;
    public int? RetryAfter { get; } = retryAfter;
}
