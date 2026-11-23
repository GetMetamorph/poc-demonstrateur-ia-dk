using Microsoft.AspNetCore.Mvc;

namespace Siccrf.Guichet.Features.ComptesDemo;

[Route("compte-demo")]
public sealed class ComptesDemoController(CompteDemo compte) : Controller
{
    [HttpPost("changer")]
    public IActionResult Changer(string role)
    {
        if (role is not ("demandeur" or "administrateur"))
            return BadRequest();

        compte.Changer(role);
        return Redirect(role == "administrateur" ? "/administration" : "/");
    }
}
