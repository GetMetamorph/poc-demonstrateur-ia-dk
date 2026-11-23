using System.Text.Json;
using Siccrf.Guichet.Features.Conversation;
using Xunit;

namespace Siccrf.Guichet.Tests;

public sealed class ChatPropositionLecteurTests
{
    private readonly ChatPropositionLecteur _lecteur = new();

    [Fact]
    public void UnRecitPeutRenseignerPlusieursChampsEtRetirerUneInformation()
    {
        var proposition = _lecteur.Lire("""
            {"message":"Qui utilise cet outil ?","modifications":[
              {"champ":"application","valeur":"IRIS Géo","source":"IRIS Géo"},
              {"champ":"difficultes","valeur":"Double saisie","source":"double saisie"},
              {"champ":"echeance","valeur":null,"source":"plus de date"}
            ]}
            """, "IRIS Géo : double saisie, et plus de date imposée.");

        Assert.Equal(3, proposition.Modifications.Count);
        Assert.Null(proposition.Modifications[2].Valeur);
    }

    public static TheoryData<string> SortiesInvalides => new()
    {
        "pas du JSON",
        "[]",
        "null",
        "{\"message\":\"Bonjour\",\"modifications\":[],\"statut\":\"Validée\"}",
        "{\"message\":\"Bonjour\",\"message\":\"Autre\",\"modifications\":[]}",
        "{\"message\":null,\"modifications\":[]}",
        "{\"message\":\"\",\"modifications\":[]}",
        "{\"message\":\"Bonjour\"}",
        "{\"message\":\"Bonjour\",\"modifications\":null}",
        "{\"message\":\"Bonjour\",\"modifications\":[null]}",
        "{\"message\":\"Bonjour\",\"modifications\":[{\"champ\":\"statut\",\"valeur\":\"Validée\",\"source\":\"IRIS\"}]}",
        "{\"message\":\"Bonjour\",\"modifications\":[{\"champ\":\"application\",\"valeur\":12,\"source\":\"IRIS\"}]}",
        "{\"message\":\"Bonjour\",\"modifications\":[{\"champ\":\"application\",\"valeur\":\"IRIS\",\"source\":\"inventé\"}]}",
        "{\"message\":\"Bonjour\",\"modifications\":[{\"champ\":\"application\",\"valeur\":\"IRIS\",\"source\":\"IRIS\",\"source\":\"IRIS\"}]}",
        "{\"message\":\"Bonjour\",\"modifications\":[{\"champ\":\"application\",\"valeur\":\"IRIS\",\"source\":\"IRIS\"},{\"champ\":\"application\",\"valeur\":\"Autre\",\"source\":\"IRIS\"}]}",
        "```json\n{\"message\":\"Bonjour\",\"modifications\":[]}\n```"
    };

    [Theory]
    [MemberData(nameof(SortiesInvalides))]
    public void UneSortieInvalideOuNonAutoriseeEstRejetee(string sortie)
    {
        var exception = Assert.Throws<ChatServiceException>(() => _lecteur.Lire(sortie, "IRIS"));
        Assert.Equal("reponse", exception.Code);
    }

    [Fact]
    public void LesLimitesDuCatalogueSontAppliqueesAuJson()
    {
        var json = JsonSerializer.Serialize(new
        {
            message = "D'accord",
            modifications = new[] { new { champ = "intitule", valeur = new string('x', 151), source = "IRIS" } }
        });
        Assert.Throws<ChatServiceException>(() => _lecteur.Lire(json, "IRIS"));
    }
}
