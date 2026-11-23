using Siccrf.Guichet.Features.Demandes;
using Xunit;

namespace Siccrf.Guichet.Tests;

public sealed class ConfirmationDemandeTests
{
    [Theory]
    [InlineData("oui")]
    [InlineData(" Je confirme ! ")]
    [InlineData("OUI, JE CONFIRME")]
    public void UnAccordAutonomePeutEtreReconnu(string texte) => Assert.True(ConfirmationDemande.EstAccord(texte));

    [Theory]
    [InlineData("oui mais il faut modifier le titre")]
    [InlineData("je ne confirme pas")]
    [InlineData("je confirme ?")]
    [InlineData("peut-être")]
    [InlineData("on verra")]
    public void UnePhraseAmbigueOuNegativeNestPasUnAccord(string texte) => Assert.False(ConfirmationDemande.EstAccord(texte));

    [Fact]
    public void UneRelectureParBoutonNePermetPasDInterpreterUnOuiCommeAccordDansLeChat()
    {
        var fiche = SoumissionSqliteTests.Complete();
        ConfirmationDemande.Preparer(fiche, new FicheRegles());
        Assert.True(ConfirmationDemande.EnAttente(fiche));
        Assert.False(ConfirmationDemande.EnAttenteDansChat(fiche));
        ConfirmationDemande.Preparer(fiche, new FicheRegles(), dansChat: true);
        Assert.True(ConfirmationDemande.EnAttenteDansChat(fiche));
    }

    [Fact]
    public void UneReponsePositiveNeSuffitPasSansQuestionPrealable()
    {
        var fiche = SoumissionSqliteTests.Complete();
        Assert.True(ConfirmationDemande.EstAccord("oui"));
        Assert.False(ConfirmationDemande.EnAttente(fiche));
        ConfirmationDemande.Preparer(fiche, new FicheRegles());
        Assert.True(ConfirmationDemande.EnAttente(fiche));
        fiche.Revision++;
        Assert.False(ConfirmationDemande.EnAttente(fiche));
    }
}
