using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Siccrf.Guichet.Features.ComptesDemo;
using Siccrf.Guichet.Persistence;

namespace Siccrf.Guichet.Features.Demandes;

public sealed class SoumissionService(GuichetDbContext db, FicheRegles regles, TimeProvider horloge)
{
    public async Task<Demande> SoumettreAsync(FicheBrouillon fiche, Guid brouillonId, long? revision,
        bool confirme, string mode, CancellationToken cancellationToken)
    {
        if (brouillonId != fiche.Id)
            throw new DemandeException("Ce formulaire correspond à un autre brouillon. Rechargez la page.");

        try
        {
            // Une tentative répétée retrouve l'enregistrement, même si le commit de session a échoué.
            var existante = await db.Demandes.AsNoTracking().SingleOrDefaultAsync(d => d.BrouillonId == fiche.Id, cancellationToken);
            if (existante is not null)
            {
                MarquerSoumise(fiche, existante);
                return existante;
            }

            var instantane = new FicheBrouillon();
            if (!regles.Appliquer(instantane, fiche.Champs.ToDictionary(p => p.Key, p => (string?)p.Value), out _) ||
                !regles.EstComplete(instantane))
                throw new DemandeException("La fiche est incomplète ou invalide. Complétez les champs obligatoires avant de soumettre.", 400);
            if (!confirme || revision != fiche.Revision || !ConfirmationDemande.EnAttente(fiche))
                throw new DemandeException("Relisez la version courante de la fiche et confirmez explicitement sa soumission.");
            if (mode == "Chat" && !ConfirmationDemande.EnAttenteDansChat(fiche))
                throw new DemandeException("La confirmation doit répondre à la question explicite posée dans le chat.");
            if (mode is not ("Bouton" or "Chat"))
                throw new DemandeException("Le mode de confirmation est invalide.", 400);

            var maintenant = horloge.GetUtcNow().UtcDateTime;
            var demande = new Demande
            {
                BrouillonId = fiche.Id,
                Champs = new Dictionary<string, string>(instantane.Champs, StringComparer.Ordinal),
                Demandeur = CompteDemo.Demandeur,
                Bureau = CompteDemo.Bureau,
                ValideurPrevu = CompteDemo.Valideur,
                DateCreationUtc = fiche.DateCreation.UtcDateTime,
                DateConfirmationUtc = maintenant,
                DateSoumissionUtc = maintenant,
                ModeConfirmation = mode
            };
            db.Demandes.Add(demande);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
            {
                db.Entry(demande).State = EntityState.Detached;
                demande = await db.Demandes.AsNoTracking().SingleOrDefaultAsync(d => d.BrouillonId == fiche.Id, cancellationToken)
                    ?? throw new DemandeException("L'enregistrement n'a pas abouti. Votre brouillon est conservé.", 503);
            }

            MarquerSoumise(fiche, demande);
            return demande;
        }
        catch (Exception exception) when (exception is DbUpdateException or SqliteException)
        {
            throw new DemandeException("L'enregistrement est momentanément indisponible. Votre brouillon est conservé ; vous pouvez réessayer.", 503);
        }
    }

    private static void MarquerSoumise(FicheBrouillon fiche, Demande demande)
    {
        if (fiche.DemandeSoumiseId is null)
            fiche.Revision++;
        fiche.DemandeSoumiseId = demande.Id;
        fiche.ReferenceSoumise = demande.Reference;
        fiche.QuestionEnCours = null;
        ConfirmationDemande.Annuler(fiche);
    }
}
