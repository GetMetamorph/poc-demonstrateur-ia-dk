using Siccrf.Guichet.Features.Demandes;
using Xunit;

namespace Siccrf.Guichet.Tests;

public sealed class FicheReglesTests
{
    private readonly FicheRegles _regles = new();

    [Fact]
    public void UnBrouillonVidePresenteLesDixChampsObligatoires()
    {
        Assert.Equal(10, _regles.Manquants(new FicheBrouillon()).Count);
    }

    [Fact]
    public void LesRubriquesFacultativesVidesNeBloquentPasLaCompletude()
    {
        var fiche = FicheComplete();
        Assert.True(_regles.EstComplete(fiche));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" À préciser ")]
    [InlineData("À renseigner")]
    [InlineData("Je ne sais pas")]
    public void UneValeurNonRenseigneeNeCompletePasUnChampObligatoire(string valeur)
    {
        var fiche = FicheComplete();
        fiche.Champs["application"] = valeur;
        Assert.Equal("application", Assert.Single(_regles.Manquants(fiche)).Cle);
    }

    [Fact]
    public void UnChampNonAutoriseRejetteTouteLaMiseAJour()
    {
        var fiche = new FicheBrouillon();
        var succes = _regles.Appliquer(fiche, new Dictionary<string, string?>
        {
            ["application"] = "IRIS Géo",
            ["statut"] = "Validée"
        }, out var erreurs);
        Assert.False(succes);
        Assert.NotEmpty(erreurs);
        Assert.Empty(fiche.Champs);
    }

    [Fact]
    public void UneValeurTropLongueRejetteTouteLaMiseAJour()
    {
        var fiche = new FicheBrouillon();
        Assert.False(_regles.Appliquer(fiche, new Dictionary<string, string?>
        {
            ["application"] = "IRIS Géo", ["intitule"] = new('x', 151)
        }, out _));
        Assert.Empty(fiche.Champs);
    }

    [Fact]
    public void UneCorrectionRemplaceLaValeurEtUnEffacementRecalculeLesManques()
    {
        var fiche = FicheComplete();
        Assert.True(_regles.Appliquer(fiche, new Dictionary<string, string?>
        {
            ["application"] = "  Nouveau service  ", ["resultat"] = null
        }, out _));
        Assert.Equal("Nouveau service", fiche.Champs["application"]);
        Assert.Equal("resultat", Assert.Single(_regles.Manquants(fiche)).Cle);
    }

    private static FicheBrouillon FicheComplete() => new()
    {
        Champs = FicheCatalogue.Champs.Where(c => c.Obligatoire)
            .ToDictionary(c => c.Cle, c => $"Information pour {c.Libelle}")
    };
}
