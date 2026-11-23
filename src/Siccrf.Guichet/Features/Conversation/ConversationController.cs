using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Siccrf.Guichet.Features.ComptesDemo;
using Siccrf.Guichet.Features.Demandes;

namespace Siccrf.Guichet.Features.Conversation;

[ApiController]
[Route("conversation")]
public sealed class ConversationController(
    BrouillonSession brouillons, CompteDemo compte, FicheRegles regles, ConversationService conversation,
    AlbertCatalogue catalogue, IOptions<AlbertOptions> options) : ControllerBase
{
    [HttpGet("modeles")]
    public Task<IActionResult> Modeles() => ExecuterAsync(async cancellationToken =>
    {
        var modeles = await catalogue.DecouvrirAsync(cancellationToken);
        var fiche = brouillons.Lire();
        return Ok(new { modeles, selection = Selection(fiche), revision = fiche.Revision });
    });

    [HttpPost("modele")]
    public Task<IActionResult> Choisir(ChoisirModele commande) => ExecuterAsync(async cancellationToken =>
    {
        var fiche = brouillons.Lire();
        if (commande.Revision != fiche.Revision)
            return Conflit(fiche);
        await catalogue.VerifierAsync(commande.Modele, cancellationToken);
        fiche.ModeleAlbert = commande.Modele;
        fiche.Revision++;
        brouillons.Enregistrer(fiche);
        return Ok(Etat(fiche));
    });

    [HttpPost("messages")]
    [RequestSizeLimit(32_000)]
    public Task<IActionResult> Envoyer(EnvoyerMessage commande) => ExecuterAsync(async cancellationToken =>
    {
        var fiche = brouillons.Lire();
        if (commande.Revision != fiche.Revision)
            return Conflit(fiche);
        if (string.IsNullOrWhiteSpace(commande.Texte) ||
            commande.Texte.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')))
            return BadRequest(new { erreur = "Saisissez un message contenant du texte." });

        await conversation.EnvoyerAsync(fiche, commande.Texte.Trim(), Selection(fiche), cancellationToken);
        brouillons.Enregistrer(fiche);
        return Ok(Etat(fiche));
    });

    private object Etat(FicheBrouillon fiche) => new
    {
        revision = fiche.Revision,
        champs = fiche.Champs,
        messages = fiche.Messages,
        question = fiche.QuestionEnCours,
        manquants = regles.Manquants(fiche).Select(c => new { cle = c.Cle, libelle = c.Libelle }),
        nombreObligatoires = FicheCatalogue.Champs.Count(c => c.Obligatoire),
        modele = Selection(fiche)
    };

    private string Selection(FicheBrouillon fiche) => fiche.ModeleAlbert ?? options.Value.Model;

    private ConflictObjectResult Conflit(FicheBrouillon fiche) => Conflict(new
    {
        erreur = "La fiche a changé dans un autre onglet. Son état courant a été rechargé ; relisez-le avant de réessayer.",
        etat = Etat(fiche)
    });

    private async Task<IActionResult> ExecuterAsync(Func<CancellationToken, Task<IActionResult>> action)
    {
        if (compte.EstAdministrateur)
            return StatusCode(403, new { erreur = "Revenez au compte demandeur pour discuter avec l'assistant." });
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
        budget.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.Value.TimeoutSeconds, 1, 120)));
        Response.Headers.CacheControl = "no-store";
        try
        {
            return await action(budget.Token);
        }
        catch (ChatServiceException exception)
        {
            if (exception.RetryAfter is int secondes)
                Response.Headers.RetryAfter = secondes.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return StatusCode(exception.Statut, new { erreur = exception.Message, code = exception.Code });
        }
        catch (OperationCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested)
        {
            return StatusCode(504, new { erreur = "Albert n'a pas répondu dans le délai prévu. Votre fiche est conservée ; vous pouvez réessayer.", code = "timeout" });
        }
    }
}

public sealed record EnvoyerMessage(
    [Required, StringLength(4000, MinimumLength = 1)] string Texte,
    [Required, Range(0, long.MaxValue)] long? Revision);

public sealed record ChoisirModele(
    [Required, StringLength(200, MinimumLength = 1)] string Modele,
    [Required, Range(0, long.MaxValue)] long? Revision);
