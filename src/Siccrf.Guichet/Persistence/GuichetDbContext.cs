using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Siccrf.Guichet.Features.Demandes;

namespace Siccrf.Guichet.Persistence;

public sealed class GuichetDbContext(DbContextOptions<GuichetDbContext> options) : DbContext(options)
{
    public DbSet<Demande> Demandes => Set<Demande>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var demande = modelBuilder.Entity<Demande>();
        demande.ToTable("Demandes", table =>
        {
            table.HasCheckConstraint("CK_Demandes_Statut", "Statut IN ('Soumise', 'Validée')");
            table.HasCheckConstraint("CK_Demandes_Validation",
                "(Statut = 'Soumise' AND DateValidationUtc IS NULL AND ValidePar IS NULL) OR " +
                "(Statut = 'Validée' AND DateValidationUtc IS NOT NULL AND ValidePar IS NOT NULL)");
            table.HasCheckConstraint("CK_Demandes_Confirmation", "ModeConfirmation IN ('Bouton', 'Chat')");
        });
        demande.HasKey(d => d.Id);
        demande.HasIndex(d => d.BrouillonId).IsUnique();
        demande.HasIndex(d => d.DateSoumissionUtc);
        demande.Property(d => d.Champs).HasColumnName("ChampsJson").IsRequired()
            .HasConversion(v => Serialiser(v), v => Deserialiser(v))
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, string>>(
                (a, b) => Serialiser(a!) == Serialiser(b!),
                v => Serialiser(v).GetHashCode(),
                v => Deserialiser(Serialiser(v))));
        demande.Property(d => d.Demandeur).IsRequired().HasMaxLength(200);
        demande.Property(d => d.Bureau).IsRequired().HasMaxLength(200);
        demande.Property(d => d.ValideurPrevu).IsRequired().HasMaxLength(200);
        demande.Property(d => d.Statut).IsRequired().HasMaxLength(20);
        demande.Property(d => d.ModeConfirmation).IsRequired().HasMaxLength(10);
        demande.Property(d => d.ValidePar).HasMaxLength(200);
    }

    private static string Serialiser(Dictionary<string, string> champs) => JsonSerializer.Serialize(champs);
    private static Dictionary<string, string> Deserialiser(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
}
