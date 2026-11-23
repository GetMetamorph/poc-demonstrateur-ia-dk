namespace Siccrf.Guichet.Features.Demandes;

public sealed record DemandePage(FicheBrouillon Fiche, IReadOnlyList<FicheChamp> Manquants)
{
    public int NombreObligatoires => FicheCatalogue.Champs.Count(c => c.Obligatoire);
    public int NombreRenseignes => NombreObligatoires - Manquants.Count;
    public int Progression => NombreRenseignes * 100 / NombreObligatoires;
}
