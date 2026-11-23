using Siccrf.Guichet.Features.Conversation;

namespace Siccrf.Guichet.Features.Demandes;

public sealed class FicheBrouillon
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset DateCreation { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string> Champs { get; init; } = new(StringComparer.Ordinal);
    public long Revision { get; set; }
    public string? ModeleAlbert { get; set; }
    public List<ChatMessage> Messages { get; init; } = [];
    public ChatQuestion? QuestionEnCours { get; set; }
    public long? RevisionAConfirmer { get; set; }
    public long? RevisionConfirmationChat { get; set; }
    public long? DemandeSoumiseId { get; set; }
    public string? ReferenceSoumise { get; set; }

    public void AjouterEchange(string utilisateur, string assistant)
    {
        Messages.Add(new ChatMessage("user", utilisateur));
        Messages.Add(new ChatMessage("assistant", assistant));
        if (Messages.Count > 12)
            Messages.RemoveRange(0, Messages.Count - 12);
    }
}
