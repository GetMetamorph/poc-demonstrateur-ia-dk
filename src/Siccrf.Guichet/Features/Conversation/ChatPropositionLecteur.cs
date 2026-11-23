using System.Text.Json;
using Siccrf.Guichet.Features.Demandes;

namespace Siccrf.Guichet.Features.Conversation;

public sealed class ChatPropositionLecteur
{
    public ChatProposition Lire(string contenu, string messageUtilisateur)
    {
        try
        {
            using var json = JsonDocument.Parse(contenu, new JsonDocumentOptions { MaxDepth = 8 });
            var racine = json.RootElement;
            ExigerProprietes(racine, "message", "modifications");
            var message = Chaine(racine.GetProperty("message"), 1500);
            var tableau = racine.GetProperty("modifications");
            if (tableau.ValueKind != JsonValueKind.Array || tableau.GetArrayLength() > FicheCatalogue.Champs.Count)
                throw AlbertApiClient.ReponseInvalide();

            var modifications = new List<ChatModification>();
            var cles = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in tableau.EnumerateArray())
            {
                ExigerProprietes(item, "champ", "valeur", "source");
                var cle = Chaine(item.GetProperty("champ"), 50);
                var definition = FicheCatalogue.Champs.FirstOrDefault(c => c.Cle == cle);
                if (definition is null || !cles.Add(cle))
                    throw AlbertApiClient.ReponseInvalide();

                var source = Chaine(item.GetProperty("source"), 400);
                if (!messageUtilisateur.Contains(source, StringComparison.Ordinal))
                    throw AlbertApiClient.ReponseInvalide();

                var valeurJson = item.GetProperty("valeur");
                var valeur = valeurJson.ValueKind == JsonValueKind.Null
                    ? null : Chaine(valeurJson, definition.LongueurMax);
                modifications.Add(new ChatModification(cle, valeur, source));
            }

            return new ChatProposition(message, modifications);
        }
        catch (JsonException)
        {
            throw AlbertApiClient.ReponseInvalide();
        }
    }

    private static void ExigerProprietes(JsonElement element, params string[] attendues)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw AlbertApiClient.ReponseInvalide();
        var proprietes = element.EnumerateObject().Select(p => p.Name).ToArray();
        if (proprietes.Length != attendues.Length ||
            proprietes.Distinct(StringComparer.Ordinal).Count() != attendues.Length ||
            proprietes.Except(attendues, StringComparer.Ordinal).Any())
            throw AlbertApiClient.ReponseInvalide();
    }

    private static string Chaine(JsonElement element, int longueurMax)
    {
        if (element.ValueKind != JsonValueKind.String)
            throw AlbertApiClient.ReponseInvalide();
        var valeur = element.GetString()!;
        if (string.IsNullOrWhiteSpace(valeur) || valeur.Length > longueurMax ||
            valeur.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t')))
            throw AlbertApiClient.ReponseInvalide();
        return valeur;
    }
}
