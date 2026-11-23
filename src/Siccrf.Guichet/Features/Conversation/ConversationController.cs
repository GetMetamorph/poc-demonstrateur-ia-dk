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
    AlbertCatalogue catalogue, IOptions<AlbertOptions> options, SoumissionService transmissions) : ControllerBase
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
        if (fiche.DemandeSoumiseId is not null)
            return Conflit(fiche);
        if (commande.Revision != fiche.Revision)
            return Conflit(fiche);
        await catalogue.VerifierAsync(commande.Modele, cancellationToken);
        fiche.ModeleAlbert = commande.Modele;
        ConfirmationDemande.Annuler(fiche);
        fiche.Revision++;
        brouillons.Enregistrer(fiche);
        return Ok(Etat(fiche));
    });

    [HttpPost("messages")]
    [RequestSizeLimit(32_000)]
    public Task<IActionResult> Envoyer(EnvoyerMessage commande) => ExecuterAsync(async cancellationToken =>
    {
        var fiche = brouillons.Lire();
        if (fiche.DemandeSoumiseId is not null)
            return Conflit(fiche);
        if (commande.Revision != fiche.Revision)
            return Conflit(fiche);
        if (string.IsNullOrWhiteSpace(commande.Texte) ||
            commande.Texte.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')))
            return BadRequest(new { erreur = "Saisissez un message contenant du texte." });

        var texte = commande.Texte.Trim();
        if (ConfirmationDemande.EnAttenteDansChat(fiche) && ConfirmationDemande.EstAccord(texte))
        {
            var demande = await transmissions.SoumettreAsync(fiche, fiche.Id, commande.Revision, true, "Chat", cancellationToken);
            fiche.AjouterEchange(texte, $"Votre demande {demande.Reference} est enregistrée avec le statut « Transmise ». Elle est transmise au valideur fictif dans le portail.");
        }
        else if (ConfirmationDemande.EnAttenteDansChat(fiche) && ConfirmationDemande.EstRefus(texte))
        {
            ConfirmationDemande.Annuler(fiche);
            fiche.Revision++;
            fiche.AjouterEchange(texte, "La demande n'a pas été transmise. Vous pouvez corriger la fiche ou utiliser « Relire et transmettre » lorsque vous serez prêt.");
        }
        else
            await conversation.EnvoyerAsync(fiche, texte, Selection(fiche), cancellationToken);
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
        modele = Selection(fiche),
        demande = fiche.DemandeSoumiseId is null ? null : new { id = fiche.DemandeSoumiseId, reference = fiche.ReferenceSoumise },
        confirmationEnAttente = ConfirmationDemande.EnAttente(fiche)
    };

    private string Selection(FicheBrouillon fiche) => fiche.ModeleAlbert ?? options.Value.Model;

    private ConflictObjectResult Conflit(FicheBrouillon fiche) => Conflict(new
    {
        erreur = fiche.DemandeSoumiseId is not null ? "Cette demande est déjà transmise. Vous pouvez consulter son récapitulatif ou créer une nouvelle demande." :
            "La fiche a changé dans un autre onglet. Son état courant a été rechargé ; relisez-le avant de réessayer.",
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
        catch (DemandeException exception)
        {
            return StatusCode(exception.Statut, new { erreur = exception.Message, code = "transmission" });
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
