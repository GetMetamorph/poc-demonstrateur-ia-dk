using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Siccrf.Guichet.Features.Conversation;

public sealed class AlbertCatalogue(AlbertApiClient api, IMemoryCache cache)
{
    private const string CleCache = "albert-modeles";

    public async Task<IReadOnlyList<AlbertModele>> DecouvrirAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<IReadOnlyList<AlbertModele>>(CleCache, out var existants))
            return existants!;

        using var json = await api.EnvoyerAsync("models", null, cancellationToken);
        if (json.RootElement.ValueKind != JsonValueKind.Object ||
            !json.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            throw AlbertApiClient.ReponseInvalide();

        var modeles = new List<AlbertModele>();
        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !item.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String ||
                type.GetString() != "text-generation")
                continue;
            if (!item.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(id.GetString()) || id.GetString()!.Length > 200)
                throw AlbertApiClient.ReponseInvalide();
            modeles.Add(new AlbertModele(id.GetString()!));
        }

        var resultat = modeles.DistinctBy(m => m.Id).OrderBy(m => m.Id, StringComparer.Ordinal).ToArray();
        cache.Set<IReadOnlyList<AlbertModele>>(CleCache, resultat, TimeSpan.FromMinutes(2));
        return resultat;
    }

    public async Task VerifierAsync(string modele, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(modele))
            throw new ChatServiceException("modele", "Choisissez un modèle dans la liste Albert.", 422);
        var modeles = await DecouvrirAsync(cancellationToken);
        if (!modeles.Any(m => m.Id == modele))
            throw new ChatServiceException("modele", "Ce modèle ne figure pas dans le catalogue de génération de texte accessible.", 422);
    }
}
