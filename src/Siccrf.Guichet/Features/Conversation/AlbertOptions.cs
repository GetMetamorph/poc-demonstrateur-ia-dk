namespace Siccrf.Guichet.Features.Conversation;

public sealed class AlbertOptions
{
    public string BaseUrl { get; set; } = "https://albert.api.etalab.gouv.fr/v1/";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 45;

    public bool ClePresente => !string.IsNullOrWhiteSpace(ApiKey);

    public Uri Adresse(string chemin)
    {
        if (!ClePresente)
            throw new ChatServiceException("configuration", "La clé Albert doit être configurée localement côté serveur.");

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(baseUri.UserInfo) ||
            !string.IsNullOrEmpty(baseUri.Query) || !string.IsNullOrEmpty(baseUri.Fragment) ||
            !BaseUrl.EndsWith('/') || TimeoutSeconds is < 1 or > 120 ||
            ApiKey.Any(char.IsControl))
            throw new ChatServiceException("configuration", "La configuration du service Albert est invalide.");

        return new Uri(baseUri, chemin);
    }
}
