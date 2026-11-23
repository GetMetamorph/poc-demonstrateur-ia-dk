using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Siccrf.Guichet.Features.ComptesDemo;
using Siccrf.Guichet.Features.Demandes;
using Siccrf.Guichet.Persistence;

namespace Siccrf.Guichet.Features.Administration;

public sealed class ValidationService(GuichetDbContext db, TimeProvider horloge)
{
    public async Task<bool> ValiderAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            var maintenant = horloge.GetUtcNow().UtcDateTime;
            var nombre = await db.Demandes.Where(d => d.Id == id && d.Statut == StatutsDemande.Soumise)
                .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.Statut, StatutsDemande.Validee)
                    .SetProperty(d => d.DateValidationUtc, maintenant)
                    .SetProperty(d => d.ValidePar, CompteDemo.Valideur), cancellationToken);
            return nombre > 0 || await db.Demandes.AnyAsync(d => d.Id == id, cancellationToken);
        }
        catch (SqliteException)
        {
            throw new DemandeException("La validation est momentanément indisponible. Réessayez sans modifier la demande.", 503);
        }
    }
}
