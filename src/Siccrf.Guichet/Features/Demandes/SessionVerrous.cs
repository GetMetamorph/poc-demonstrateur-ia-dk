using Microsoft.Extensions.Caching.Memory;

namespace Siccrf.Guichet.Features.Demandes;

// ASP.NET Session n'applique pas de verrou : sérialiser les lectures/écritures d'une
// même session pour éviter qu'une réponse IA écrase une correction dans un autre onglet.
public sealed class SessionVerrous(IMemoryCache cache)
{
    private readonly object _creation = new();

    public SemaphoreSlim Pour(string cookie)
    {
        lock (_creation)
            return cache.GetOrCreate("verrou-session:" + cookie, entree =>
            {
                entree.SlidingExpiration = TimeSpan.FromHours(3);
                return new SemaphoreSlim(1, 1);
            })!;
    }
}
