using System.ComponentModel.DataAnnotations.Schema;

namespace Siccrf.Guichet.Features.Demandes;

public static class StatutsDemande
{
    public const string Soumise = "Soumise";
    public const string Validee = "Validée";
}

// Instantané de la fiche à la soumission ; le dialogue n'est pas persisté.
public sealed class Demande
{
    public long Id { get; set; }
    public Guid BrouillonId { get; set; }
    public Dictionary<string, string> Champs { get; set; } = new(StringComparer.Ordinal);
    public string Demandeur { get; set; } = "";
    public string Bureau { get; set; } = "";
    public string ValideurPrevu { get; set; } = "";
    public DateTime DateCreationUtc { get; set; }
    public DateTime DateConfirmationUtc { get; set; }
    public DateTime DateSoumissionUtc { get; set; }
    public string ModeConfirmation { get; set; } = "";
    public string Statut { get; set; } = StatutsDemande.Soumise;
    public DateTime? DateValidationUtc { get; set; }
    public string? ValidePar { get; set; }

    [NotMapped] public string Intitule => Champs.GetValueOrDefault("intitule", "");
    [NotMapped] public string Reference => $"EB-{DatesFrance.Convertir(DateSoumissionUtc).Year}-{Id:0000}";
}

public static class DatesFrance
{
    private static readonly TimeZoneInfo Fuseau = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
    public static DateTime Convertir(DateTime dateUtc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dateUtc, DateTimeKind.Utc), Fuseau);
    public static string Afficher(DateTime dateUtc) => Convertir(dateUtc).ToString("dd/MM/yyyy à HH:mm");
}

public sealed class DemandeException(string message, int statut = 409) : Exception(message)
{
    public int Statut { get; } = statut;
}
