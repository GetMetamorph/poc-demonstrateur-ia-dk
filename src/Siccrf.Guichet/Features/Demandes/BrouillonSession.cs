using System.Text.Json;

namespace Siccrf.Guichet.Features.Demandes;

// Brouillon temporaire côté serveur. La persistance SQLite viendra avec la transmission.
public sealed class BrouillonSession(IHttpContextAccessor accessor)
{
    private const string Cle = "fiche-brouillon";
    private ISession Session => accessor.HttpContext!.Session;

    public FicheBrouillon Lire()
    {
        var json = Session.GetString(Cle);
        if (json is not null)
            return JsonSerializer.Deserialize<FicheBrouillon>(json)!;

        var fiche = new FicheBrouillon();
        Enregistrer(fiche);
        return fiche;
    }

    public void Enregistrer(FicheBrouillon fiche) => Session.SetString(Cle, JsonSerializer.Serialize(fiche));
}
