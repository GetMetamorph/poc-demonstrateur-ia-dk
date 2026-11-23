namespace Siccrf.Guichet.Features.Demandes;

public sealed class FicheRegles
{
    private static readonly HashSet<string> ValeursNonRenseignees = new(StringComparer.OrdinalIgnoreCase)
    {
        "à préciser", "a preciser", "à renseigner", "a renseigner", "inconnu", "inconnue",
        "je ne sais pas", "non renseigné", "non renseigne", "sans objet", "n/a", "-", "..."
    };

    public IReadOnlyList<FicheChamp> Manquants(FicheBrouillon fiche) =>
        FicheCatalogue.Champs.Where(champ => champ.Obligatoire &&
            (!fiche.Champs.TryGetValue(champ.Cle, out var valeur) || !EstRenseigne(valeur)))
            .ToArray();

    public bool EstComplete(FicheBrouillon fiche) => Manquants(fiche).Count == 0;

    public bool Appliquer(FicheBrouillon fiche, IReadOnlyDictionary<string, string?> changements,
        out IReadOnlyList<string> erreurs)
    {
        var problemes = new List<string>();
        var valeurs = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (cle, valeur) in changements)
        {
            var champ = FicheCatalogue.Champs.FirstOrDefault(c => c.Cle == cle);
            if (champ is null)
            {
                problemes.Add($"Champ non autorisé : {cle}.");
                continue;
            }

            var texte = valeur?.Trim() ?? "";
            if (texte.Length > champ.LongueurMax)
                problemes.Add($"{champ.Libelle} : {champ.LongueurMax} caractères maximum.");
            else if (texte.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')))
                problemes.Add($"{champ.Libelle} : le texte contient des caractères de contrôle interdits.");
            else
                valeurs[cle] = texte;
        }

        erreurs = problemes;
        if (problemes.Count > 0)
            return false;

        // Validation de l'ensemble avant mutation : aucune mise à jour partielle.
        foreach (var (cle, valeur) in valeurs)
            fiche.Champs[cle] = valeur;

        return true;
    }

    private static bool EstRenseigne(string? valeur) =>
        !string.IsNullOrWhiteSpace(valeur) && !ValeursNonRenseignees.Contains(valeur.Trim());
}
