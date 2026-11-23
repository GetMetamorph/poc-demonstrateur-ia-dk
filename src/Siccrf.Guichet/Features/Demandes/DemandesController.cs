using Microsoft.AspNetCore.Mvc;
using Siccrf.Guichet.Features.ComptesDemo;
using Siccrf.Guichet.Features.Conversation;

namespace Siccrf.Guichet.Features.Demandes;

public sealed class DemandesController(BrouillonSession brouillons, FicheRegles regles, CompteDemo compte,
    EntretienGuide entretien)
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
        fiche.QuestionEnCours = entretien.QuestionCourante(fiche);
        brouillons.Enregistrer(fiche);
        TempData["Succes"] = "Les corrections ont été appliquées au brouillon.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/demande/soumettre")]
    public IActionResult Soumettre()
    {
        if (compte.EstAdministrateur)
            return StatusCode(StatusCodes.Status403Forbidden);

        var fiche = brouillons.Lire();
        if (!regles.EstComplete(fiche))
        {
            ModelState.AddModelError("", "La fiche est incomplète. Renseignez les champs obligatoires indiqués.");
            Response.StatusCode = StatusCodes.Status400BadRequest;
        }
        else
        {
            // Aucun envoi factice : confirmation et enregistrement seront ajoutés ensemble.
            ModelState.AddModelError("", "La soumission sera disponible à l'étape 3, avec l'enregistrement SQLite.");
            Response.StatusCode = StatusCodes.Status409Conflict;
        }

        return View(Vue, new DemandePage(fiche, regles.Manquants(fiche)));
    }

    [HttpGet("/erreur")]
    public IActionResult Erreur() => Problem("Une erreur est survenue. Rechargez la page pour réessayer.");
}
