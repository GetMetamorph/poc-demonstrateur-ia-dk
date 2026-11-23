using System.Net;
using System.Text;
using System.Text.Json;

namespace Siccrf.Guichet.Tests;

// Transport HTTP simulé : aucune clé ni requête Albert réelle dans les tests.
internal sealed class AlbertTestApi : HttpMessageHandler, IHttpClientFactory
{
    private readonly Queue<Func<CancellationToken, Task<HttpResponseMessage>>> _reponses = new();
    public List<(string Path, string Body, string? Authorization)> Requetes { get; } = [];

    public void Ajouter(string json, HttpStatusCode statut = HttpStatusCode.OK, int? retryAfter = null) =>
        _reponses.Enqueue(_ =>
        {
            var response = new HttpResponseMessage(statut)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            if (retryAfter is int secondes)
                response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(secondes));
            return Task.FromResult(response);
        });

    public void Bloquer() => _reponses.Enqueue(async cancellationToken =>
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("Le transport devait être annulé.");
    });

    public void Catalogue() => Ajouter("""
        {"data":[{"id":"modele-test","type":"text-generation"},{"id":"embeddings-test","type":"text-embeddings"}]}
        """);

    public void Completion(string contenu, string raison = "stop") => Ajouter(JsonSerializer.Serialize(new
    {
        choices = new[] { new { finish_reason = raison, message = new { content = contenu } } }
    }));

    public const string Proposition = """
        {"message":"Qui utilise IRIS Géo ?","modifications":[{"champ":"application","valeur":"IRIS Géo","source":"IRIS Géo"},{"champ":"difficultes","valeur":"Double saisie","source":"double saisie"}]}
        """;

    public HttpClient CreateClient(string name) => new(this, disposeHandler: false);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requetes.Add((request.RequestUri!.AbsolutePath,
            request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken),
            request.Headers.Authorization?.ToString()));
        if (_reponses.Count == 0)
            throw new InvalidOperationException("Une requête HTTP inattendue a été émise.");
        return await _reponses.Dequeue()(cancellationToken);
    }
}
