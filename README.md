# Guichet SICCRF POC étape 1

Première interface ASP.NET Core MVC .NET 10, construite à partir des deux pages du modèle officiel fourni. Les fichiers source complets sont inclus dans cette archive ; aucun code n'est à reconstituer depuis la conversation.

## Lancer

Prérequis : SDK .NET 10 stable. Depuis le dossier extrait contenant `Siccrf.Guichet.slnx` :

```bash
dotnet restore Siccrf.Guichet.slnx
dotnet build Siccrf.Guichet.slnx --no-restore
dotnet test Siccrf.Guichet.slnx --no-build
dotnet run --project src/Siccrf.Guichet
```

Ouvrir http://localhost:5080. Le profil local utilise l'environnement Development et ne requiert pas de certificat HTTPS. Il n'y a ni base de données ni clé à configurer pour cette étape. Bootstrap est inclus localement : aucun CDN n'est appelé par le navigateur.

Le fichier `global.json` accepte les bandes de fonctionnalités stables .NET 10 à partir de 10.0.100. Un SDK .NET 8 ou 9 seul ne suffit pas.

## Déjà utilisable

- Navbar et bascule entre demandeur et administrateur fictifs.
- Page demandeur avec message d'accueil et aperçu des 17 champs métier du Word.
- Identité fictive préremplie, séparation des 10 champs obligatoires et des 7 rubriques non bloquantes.
- Section « Renseigner ou corriger la fiche » : l'aperçu suit la saisie en JavaScript ; « Appliquer les corrections » valide les champs et conserve le brouillon en session serveur.
- Après application, progression et liste des manques calculées par le backend.
- Page administrateur vide : aucun brouillon présenté comme une demande soumise.
- Protection antiforgery des POST, valeurs encodées, liste de champs autorisés, limite des tailles et mises à jour atomiques.
- Tests ciblés des règles de fiche.

La saisie de corrections accepte une fiche incomplète pour permettre la rédaction progressive. En revanche, le backend refuse une tentative de soumission incomplète, même si le bouton désactivé est contourné. Une fiche complète n'est pas encore enregistrable : l'endpoint retourne un refus explicite jusqu'à l'étape 3.

## Limites explicites

Le chat affiche l'accueil mais la saisie est désactivée : Albert n'est pas encore raccordé. La configuration fournie prépare l'étape 2 ; renseigner une clé à cette étape ne suffit pas à activer le chat. Aucune extraction IA, soumission SQLite, validation administrative réelle ou génération Word n'est encore implémentée.

Les brouillons sont temporaires et propres à la session : deux heures d'inactivité maximum, perte au redémarrage du serveur ou à l'expiration de la session. La bascule de rôle dans la même session conserve le brouillon. Les rôles fictifs ne sont pas une authentification et ne conviennent pas à une publication en production. Enregistrer les modifications avant de changer de page ; les corrections non appliquées restent uniquement dans la page courante.

## Préparer Albert pour l'étape 2

Ne pas envoyer la clé dans une conversation ni l'inscrire dans les sources. Le projet possède déjà un `UserSecretsId`. En développement, enregistrer localement la valeur :

```bash
dotnet user-secrets set "Albert:ApiKey" "VOTRE_CLE_LOCALE" --project src/Siccrf.Guichet
```

`VOTRE_CLE_LOCALE` est un marqueur à remplacer localement. Attention à l'historique du terminal sur un poste partagé. Dans Visual Studio, « Gérer les secrets utilisateur » permet aussi d'éditer le fichier hors du projet. User Secrets n'est pas chiffré et doit rester réservé au développement.

Alternativement, configurer `Albert__ApiKey` dans l'environnement du processus, sans fichier versionné. Les autres paramètres sont `Albert:BaseUrl`, `Albert:Model` et `Albert:TimeoutSeconds`. Le modèle reste vide intentionnellement.

Après raccordement, la découverte utilisera `GET /v1/models` avec la clé côté serveur. Sélectionner un identifiant réellement renvoyé et de type `text-generation`, puis le configurer :

```bash
dotnet user-secrets set "Albert:Model" "IDENTIFIANT_RENVOYE_PAR_MODELS" --project src/Siccrf.Guichet
```

Les placeholders ne sont ni une clé ni un nom de modèle à utiliser. Aucun appel Albert n'a été validé avec une clé réelle dans cette livraison.

## Fichiers principaux

| Fichier ou dossier | Rôle |
| --- | --- |
| Siccrf.Guichet.slnx | Solution application et tests. |
| src/Siccrf.Guichet/Program.cs | MVC, session, injection et middleware. |
| src/Siccrf.Guichet/Features/Demandes/FicheCatalogue.cs | Les 17 champs, leurs libellés, obligations et limites. |
| src/Siccrf.Guichet/Features/Demandes/FicheRegles.cs | Complétude et validation des mises à jour. |
| src/Siccrf.Guichet/Features/Demandes/DemandesController.cs | Page, corrections et refus de soumission. |
| src/Siccrf.Guichet/Features/Demandes/BrouillonSession.cs | Brouillon temporaire côté serveur. |
| src/Siccrf.Guichet/Features/Demandes/Views/Index.cshtml | Chat d'accueil, aperçu et corrections. |
| src/Siccrf.Guichet/Features/ComptesDemo | Identités et changement de rôle fictif. |
| src/Siccrf.Guichet/Features/Administration | Tableau administrateur vide. |
| src/Siccrf.Guichet/Views/Shared/_Layout.cshtml | Navigation et mention de démonstration. |
| src/Siccrf.Guichet/wwwroot | CSS, JavaScript natif et Bootstrap avec licence MIT. |
| tests/Siccrf.Guichet.Tests | Tests des règles critiques déjà implémentées. |
| reference/FEB(1).docx | Modèle fourni, conservé intact. |
| docs/Cadrage.md | Analyse exhaustive, modèle SQLite prévu et étapes d'implémentation. |
| docs/Prompt-systeme.md | Prompt complet proposé pour le raccordement Albert. |
| docs/Verification.md | Vérifications réalisées et limites de validation. |

Les dépendances EF Core SQLite et Open XML SDK seront ajoutées avec leurs fonctionnalités, sans services vides ni abstractions inutilisées à l'étape 1.
