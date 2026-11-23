using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Siccrf.Guichet.Features.Administration;
using Siccrf.Guichet.Features.ComptesDemo;
using Siccrf.Guichet.Features.Demandes;
using Siccrf.Guichet.Persistence;
using Xunit;

namespace Siccrf.Guichet.Tests;

public sealed class SoumissionSqliteTests
{
    [Fact]
    public async Task UneFicheIncompleteEstRefuseeMemeAvecUneConfirmationForgeable()
    {
        using var stockage = new StockageSqliteTest();
        await using var db = await stockage.OuvrirAsync();
        var fiche = new FicheBrouillon { RevisionAConfirmer = 0 };
        var exception = await Assert.ThrowsAsync<DemandeException>(() => Service(db).SoumettreAsync(fiche, fiche.Id, 0, true, "Bouton", default));
        Assert.Equal(400, exception.Statut);
        Assert.Equal(0, await db.Demandes.CountAsync());
        Assert.Null(fiche.DemandeSoumiseId);
    }

    [Theory]
    [InlineData(false, true, 0)]
    [InlineData(true, false, 0)]
    [InlineData(true, true, 1)]
    public async Task SansQuestionSansAccordOuAvecUneAncienneVersionLaSoumissionEstRefusee(bool question, bool accord, long revision)
    {
        using var stockage = new StockageSqliteTest();
        await using var db = await stockage.OuvrirAsync();
        var fiche = Complete();
        if (question) ConfirmationDemande.Preparer(fiche, new FicheRegles());
        await Assert.ThrowsAsync<DemandeException>(() => Service(db).SoumettreAsync(fiche, fiche.Id, revision, accord, "Bouton", default));
        Assert.Equal(0, await db.Demandes.CountAsync());
    }

    [Fact]
    public async Task UneConfirmationEnregistreUnInstantaneEtDesDatesServeur()
    {
        using var stockage = new StockageSqliteTest();
        var fiche = Complete();
        ConfirmationDemande.Preparer(fiche, new FicheRegles());
        long id;
        await using (var db = await stockage.OuvrirAsync())
        {
            var demande = await Service(db).SoumettreAsync(fiche, fiche.Id, fiche.Revision, true, "Bouton", default);
            id = demande.Id;
            Assert.Equal("EB-2026-0001", demande.Reference);
            Assert.Equal(StatutsDemande.Soumise, demande.Statut);
            Assert.Equal("Bouton", demande.ModeConfirmation);
            Assert.Equal(Instant, demande.DateConfirmationUtc);
            Assert.Equal(Instant, demande.DateSoumissionUtc);
            Assert.Equal(CompteDemo.Demandeur, demande.Demandeur);
            Assert.Null(demande.DateValidationUtc);
            Assert.False(ConfirmationDemande.EnAttente(fiche));
        }

        fiche.Champs["intitule"] = "Modification en mémoire après soumission";
        // Réouverture du fichier avec un autre contexte : l'enregistrement survit et reste figé.
        await using var relu = await stockage.OuvrirAsync();
        var persistante = await relu.Demandes.AsNoTracking().SingleAsync();
        Assert.Equal(id, persistante.Id);
        Assert.Equal("Centraliser le suivi des dossiers", persistante.Intitule);
        Assert.Equal(10, persistante.Champs.Count);
    }

    [Fact]
    public async Task UneNouvelleTentativeApresPerteDeSessionRetrouveLaMemeDemande()
    {
        using var stockage = new StockageSqliteTest();
        await using var db = await stockage.OuvrirAsync();
        var fiche = Complete();
        ConfirmationDemande.Preparer(fiche, new FicheRegles(), dansChat: true);
        var premiere = await Service(db).SoumettreAsync(fiche, fiche.Id, 0, true, "Chat", default);
        var ancienneSession = Complete(fiche.Id);
        ConfirmationDemande.Preparer(ancienneSession, new FicheRegles(), dansChat: true);
        var seconde = await Service(db).SoumettreAsync(ancienneSession, ancienneSession.Id, 0, true, "Chat", default);
        Assert.Equal(premiere.Id, seconde.Id);
        Assert.Equal(1, await db.Demandes.CountAsync());
        Assert.Equal(premiere.Id, ancienneSession.DemandeSoumiseId);
    }

    [Fact]
    public async Task DeuxContextesConcurrentsNeCreentQuUneSeuleDemande()
    {
        using var stockage = new StockageSqliteTest();
        await using (var initial = await stockage.OuvrirAsync()) { }
        var id = Guid.NewGuid();
        async Task<long> Soumettre()
        {
            await using var db = await stockage.OuvrirAsync();
            var fiche = Complete(id);
            ConfirmationDemande.Preparer(fiche, new FicheRegles());
            return (await Service(db).SoumettreAsync(fiche, fiche.Id, 0, true, "Bouton", default)).Id;
        }
        var resultats = await Task.WhenAll(Task.Run(Soumettre), Task.Run(Soumettre));
        Assert.Equal(resultats[0], resultats[1]);
        await using var verification = await stockage.OuvrirAsync();
        Assert.Equal(1, await verification.Demandes.CountAsync());
    }

    [Fact]
    public async Task UneErreurDEcritureConserveLeBrouillonEtPermetDeReessayer()
    {
        using var stockage = new StockageSqliteTest();
        await using (var initial = await stockage.OuvrirAsync()) { }
        var fiche = Complete();
        ConfirmationDemande.Preparer(fiche, new FicheRegles());
        await using (var lectureSeule = stockage.LectureSeule())
        {
            var erreur = await Assert.ThrowsAsync<DemandeException>(() => Service(lectureSeule).SoumettreAsync(fiche, fiche.Id, 0, true, "Bouton", default));
            Assert.Equal(503, erreur.Statut);
            Assert.Null(fiche.DemandeSoumiseId);
            Assert.True(ConfirmationDemande.EnAttente(fiche));
            Assert.Equal(10, fiche.Champs.Count);
        }
        await using var db = await stockage.OuvrirAsync();
        await Service(db).SoumettreAsync(fiche, fiche.Id, 0, true, "Bouton", default);
        Assert.Equal(1, await db.Demandes.CountAsync());
    }

    [Fact]
    public async Task LaValidationEstDateeEtUneRepetitionNeRemplacePasLaDecisionInitiale()
    {
        using var stockage = new StockageSqliteTest();
        await using var db = await stockage.OuvrirAsync();
        var fiche = Complete();
        ConfirmationDemande.Preparer(fiche, new FicheRegles());
        var demande = await Service(db).SoumettreAsync(fiche, fiche.Id, 0, true, "Bouton", default);
        var horloge = new HorlogeTest(Instant.AddHours(1));
        var validation = new ValidationService(db, horloge);
        Assert.True(await validation.ValiderAsync(demande.Id, default));
        horloge.Instant = Instant.AddHours(2);
        Assert.True(await validation.ValiderAsync(demande.Id, default));
        var relue = await db.Demandes.AsNoTracking().SingleAsync();
        Assert.Equal(StatutsDemande.Validee, relue.Statut);
        Assert.Equal(Instant.AddHours(1), relue.DateValidationUtc);
        Assert.Equal(CompteDemo.Valideur, relue.ValidePar);
        Assert.False(await validation.ValiderAsync(9999, default));
    }

    [Fact]
    public void LaReferenceUtiliseLAnneeDeSoumissionEnFrance()
    {
        var demande = new Demande { Id = 42, DateSoumissionUtc = new DateTime(2026, 12, 31, 23, 30, 0, DateTimeKind.Utc) };
        Assert.Equal("EB-2027-0042", demande.Reference);
    }

    internal static FicheBrouillon Complete(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Champs = FicheCatalogue.Champs.Where(c => c.Obligatoire).ToDictionary(c => c.Cle,
            c => c.Cle == "intitule" ? "Centraliser le suivi des dossiers" : $"Information métier pour {c.Libelle}")
    };

    private static readonly DateTime Instant = new(2026, 11, 23, 12, 30, 0, DateTimeKind.Utc);
    private static SoumissionService Service(GuichetDbContext db) => new(db, new FicheRegles(), new HorlogeTest(Instant));
}

internal sealed class HorlogeTest(DateTime instant) : TimeProvider
{
    public DateTime Instant { get; set; } = instant;
    public override DateTimeOffset GetUtcNow() => new(Instant);
}

internal sealed class StockageSqliteTest : IDisposable
{
    public string Chemin { get; } = Path.Combine(Path.GetTempPath(), $"siccrf-stockage-{Guid.NewGuid():N}.db");
    public async Task<GuichetDbContext> OuvrirAsync()
    {
        var db = Creer(SqliteOpenMode.ReadWriteCreate);
        await db.Database.MigrateAsync();
        return db;
    }
    public GuichetDbContext LectureSeule() => Creer(SqliteOpenMode.ReadOnly);
    private GuichetDbContext Creer(SqliteOpenMode mode) => new(new DbContextOptionsBuilder<GuichetDbContext>()
        .UseSqlite(new SqliteConnectionStringBuilder { DataSource = Chemin, Pooling = false, Mode = mode, DefaultTimeout = 5 }.ToString()).Options);
    public void Dispose()
    {
        File.Delete(Chemin);
        File.Delete(Chemin + "-wal");
        File.Delete(Chemin + "-shm");
    }
}
