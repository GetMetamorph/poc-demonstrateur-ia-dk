using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Siccrf.Guichet.Features.Conversation;

public sealed class AlbertApiClient(IHttpClientFactory clients, IOptions<AlbertOptions> options)
{
    private const int TailleMax = 512 * 1024;

    public async Task<JsonDocument> EnvoyerAsync(string chemin, object? contenu, CancellationToken cancellationToken)
    {
        var configuration = options.Value;
        using var request = new HttpRequestMessage(contenu is null ? HttpMethod.Get : HttpMethod.Post,
            configuration.Adresse(chemin));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.ApiKey);
        if (contenu is not null)
            request.Content = JsonContent.Create(contenu);

        try
        {
            using var client = clients.CreateClient("Albert");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            VerifierStatut(response);

            if (response.Content.Headers.ContentLength > TailleMax)
                throw ReponseInvalide();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var bloc = new byte[8192];
            int lus;
            while ((lus = await stream.ReadAsync(bloc, cancellationToken)) > 0)
            {
                if (buffer.Length + lus > TailleMax)
                    throw ReponseInvalide();
                await buffer.WriteAsync(bloc.AsMemory(0, lus), cancellationToken);
            }

            return JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
        }
        catch (HttpRequestException)
        {
            throw new ChatServiceException("indisponible", "Albert est momentanément inaccessible. Votre fiche est conservée.");
        }
        catch (JsonException)
        {
            throw ReponseInvalide();
        }
        catch (IOException)
        {
            throw new ChatServiceException("indisponible", "La réponse Albert a été interrompue. Vous pouvez réessayer.");
        }
    }

    private static void VerifierStatut(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var delai = response.Headers.RetryAfter?.Delta ??
                (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow);
            var secondes = delai is null ? 30 : (int)Math.Clamp(Math.Ceiling(delai.Value.TotalSeconds), 1, 300);
            throw new ChatServiceException("limitation", "La limite Albert est atteinte. Réessayez un peu plus tard.", 429, secondes);
        }

        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                new ChatServiceException("acces", "Albert refuse l'accès. Vérifiez localement la clé et les droits du compte."),
            HttpStatusCode.NotFound or HttpStatusCode.UnprocessableEntity =>
                new ChatServiceException("modele", "Le modèle n'est plus disponible ou compatible. Actualisez la liste des modèles."),
            _ => new ChatServiceException("indisponible", "Albert est temporairement indisponible. Votre fiche est conservée.")
        };
    }

    public static ChatServiceException ReponseInvalide() =>
        new("reponse", "Albert a renvoyé une réponse inexploitable. Aucune modification n'a été appliquée.", 502);
}
