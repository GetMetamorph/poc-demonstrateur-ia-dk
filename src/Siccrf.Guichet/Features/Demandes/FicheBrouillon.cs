namespace Siccrf.Guichet.Features.Demandes;

public sealed class FicheBrouillon
{
    public DateTimeOffset DateCreation { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string> Champs { get; init; } = new(StringComparer.Ordinal);
}
