using Microsoft.AspNetCore.Mvc;
using Siccrf.Guichet.Features.ComptesDemo;
using Siccrf.Guichet.Features.Conversation;
using Microsoft.EntityFrameworkCore;
using Siccrf.Guichet.Persistence;

namespace Siccrf.Guichet.Features.Demandes;

public sealed class DemandesController(BrouillonSession brouillons, FicheRegles regles, CompteDemo compte,
    EntretienGuide entretien, SoumissionService soumissions, GuichetDbContext db)
    : Controller
{
    private const string Vue = "~/Features/Demandes/Views/Index.cshtml";

    [HttpGet("/")]
    public IActionResult Index()
    {
        if (compte.EstAdministrateur)
            return Redirect("/administration");

        var fiche = brouillons.Lire();
        return View(Vue, new DemandePage(fiche, regles.Manquants(fiche)));
    }

    [HttpPost("/demande/corriger")]
    [RequestSizeLimit(300_000)]
    public IActionResult Corriger([FromForm] Dictionary<string, string?> champs, [FromForm] long? revision)
    {
        if (compte.EstAdministrateur)
            return StatusCode(StatusCodes.Status403Forbidden);

        var fiche = brouillons.Lire();
        if (fiche.DemandeSoumiseId is not null)
            return ErreurFiche(fiche, "Cette demande est déjà soumise. Vous pouvez créer une nouvelle demande.", 409);
        if (!ModelState.IsValid || revision != fiche.Revision)
        {
            ModelState.AddModelError("", "La fiche a changé ou le formulaire est invalide. Les valeurs courantes sont affichées ; relisez-les avant de corriger à nouveau.");
            Response.StatusCode = StatusCodes.Status409Conflict;
            return View(Vue, new DemandePage(fiche, regles.Manquants(fiche)));
        }
        if (!regles.Appliquer(fiche, champs, out var erreurs))
        {
            foreach (var erreur in erreurs)
                ModelState.AddModelError("", erreur);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(Vue, new DemandePage(fiche, regles.Manquants(fiche)));
        }

        fiche.Revision++;
        ConfirmationDemande.Annuler(fiche);
        fiche.QuestionEnCours = entretien.QuestionCourante(fiche);
        brouillons.Enregistrer(fiche);
        TempData["Succes"] = "Les corrections ont été appliquées au brouillon.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/demande/preparer")]
    public IActionResult Preparer([FromForm] long? revision)
    {
        if (compte.EstAdministrateur)
            return StatusCode(StatusCodes.Status403Forbidden);
        var fiche = brouillons.Lire();
        if (!ModelState.IsValid || revision != fiche.Revision)
            return ErreurFiche(fiche, "La fiche a changé. Rechargez-la avant de préparer la confirmation.", 409);
        try
        {
            ConfirmationDemande.Preparer(fiche, regles);
            brouillons.Enregistrer(fiche);
            return RedirectToAction(nameof(Relecture));
        }
        catch (DemandeException exception)
        {
            return ErreurFiche(fiche, exception.Message, exception.Statut);
        }
    }

    [HttpGet("/demande/relecture")]
    public IActionResult Relecture()
    {
        if (compte.EstAdministrateur)
            return Redirect("/administration");
        var fiche = brouillons.Lire();
        if (!ConfirmationDemande.EnAttente(fiche) || !regles.EstComplete(fiche))
            return RedirectToAction(nameof(Index));
        return View("~/Features/Demandes/Views/Relecture.cshtml", fiche);
    }

    [HttpPost("/demande/soumettre")]
    public async Task<IActionResult> Soumettre([FromForm] Guid brouillonId, [FromForm] long? revision,
        [FromForm] bool confirme, CancellationToken cancellationToken)
    {
        if (compte.EstAdministrateur)
            return StatusCode(StatusCodes.Status403Forbidden);
        var fiche = brouillons.Lire();
        if (!ModelState.IsValid)
            return ErreurFiche(fiche, "Le formulaire de confirmation est invalide. Rechargez la fiche.", 400);
        try
        {
            await soumissions.SoumettreAsync(fiche, brouillonId, revision, confirme, "Bouton", cancellationToken);
            brouillons.Enregistrer(fiche);
            return RedirectToAction(nameof(Recapitulatif));
        }
        catch (DemandeException exception)
        {
            return ErreurFiche(fiche, exception.Message, exception.Statut);
        }
    }

    [HttpGet("/demande/recapitulatif")]
    public async Task<IActionResult> Recapitulatif(CancellationToken cancellationToken)
    {
        var fiche = brouillons.Lire();
        var demande = await db.Demandes.AsNoTracking().SingleOrDefaultAsync(
            d => d.Id == fiche.DemandeSoumiseId && d.BrouillonId == fiche.Id, cancellationToken);
        if (demande is null)
            return RedirectToAction(nameof(Index));
        return View("~/Features/Demandes/Views/Detail.cshtml", new DemandeDetail(demande, compte.EstAdministrateur));
    }

    [HttpPost("/demande/nouvelle")]
    public IActionResult Nouvelle()
    {
        if (compte.EstAdministrateur)
            return StatusCode(StatusCodes.Status403Forbidden);
        var fiche = brouillons.Lire();
        if (fiche.DemandeSoumiseId is null)
            return BadRequest();
        brouillons.Enregistrer(new FicheBrouillon { ModeleAlbert = fiche.ModeleAlbert });
        return RedirectToAction(nameof(Index));
    }

    private IActionResult ErreurFiche(FicheBrouillon fiche, string message, int statut)
    {
        ModelState.AddModelError("", message);
        Response.StatusCode = statut;
        return View(Vue, new DemandePage(fiche, regles.Manquants(fiche)));
    }

    [HttpGet("/erreur")]
    public IActionResult Erreur() => Problem("Une erreur est survenue. Rechargez la page pour réessayer.");
}
