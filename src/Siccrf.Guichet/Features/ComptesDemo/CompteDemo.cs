namespace Siccrf.Guichet.Features.ComptesDemo;

// Ce rôle de démonstration ne constitue pas une autorisation de sécurité.
public sealed class CompteDemo(IHttpContextAccessor accessor)
{
    public const string Demandeur = "Camille Martin";
    public const string Bureau = "Bureau Démonstration — SICCRF";
    public const string Valideur = "Alex Dupont";

    private ISession Session => accessor.HttpContext!.Session;

    public bool EstAdministrateur => Session.GetString("demo-role") == "administrateur";

    public void Changer(string role) => Session.SetString("demo-role", role);
}
