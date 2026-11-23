using Microsoft.AspNetCore.Mvc;
using Siccrf.Guichet.Features.ComptesDemo;

namespace Siccrf.Guichet.Features.Administration;

public sealed class AdministrationController(CompteDemo compte) : Controller
{
    [HttpGet("/administration")]
    public IActionResult Index() => compte.EstAdministrateur
        ? View("~/Features/Administration/Views/Index.cshtml")
        : Redirect("/");
}
