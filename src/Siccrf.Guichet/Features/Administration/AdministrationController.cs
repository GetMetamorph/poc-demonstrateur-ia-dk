using Microsoft.AspNetCore.Mvc;
using Siccrf.Guichet.Features.ComptesDemo;
using Microsoft.EntityFrameworkCore;
using Siccrf.Guichet.Features.Demandes;
using Siccrf.Guichet.Persistence;

namespace Siccrf.Guichet.Features.Administration;

public sealed class AdministrationController(CompteDemo compte, GuichetDbContext db, ValidationService validations) : Controller
{
    [HttpGet("/administration")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!compte.EstAdministrateur)
            return Redirect("/");
        var demandes = await db.Demandes.AsNoTracking().OrderByDescending(d => d.DateSoumissionUtc)
            .ThenByDescending(d => d.Id).ToListAsync(cancellationToken);
        return View("~/Features/Administration/Views/Index.cshtml", demandes);
    }

    [HttpGet("/administration/demandes/{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken cancellationToken)
    {
        if (!compte.EstAdministrateur)
            return StatusCode(403);
        var demande = await db.Demandes.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        return demande is null ? NotFound() : View("~/Features/Demandes/Views/Detail.cshtml", new DemandeDetail(demande, true));
    }

    [HttpPost("/administration/demandes/{id:long}/valider")]
    public async Task<IActionResult> Valider(long id, CancellationToken cancellationToken)
    {
        if (!compte.EstAdministrateur)
            return StatusCode(403);
        try
        {
            if (!await validations.ValiderAsync(id, cancellationToken))
                return NotFound();
            TempData["Succes"] = "La demande est validée. La date et le valideur fictif ont été enregistrés.";
        }
        catch (DemandeException exception)
        {
            TempData["Erreur"] = exception.Message;
        }
        return RedirectToAction(nameof(Detail), new { id });
    }
}
