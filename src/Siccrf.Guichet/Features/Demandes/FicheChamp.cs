namespace Siccrf.Guichet.Features.Demandes;

public sealed record FicheChamp(
    string Cle, string Rubrique, string Libelle, string Aide, bool Obligatoire, int LongueurMax = 2000);
