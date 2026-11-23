# Guichet SICCRF POC étape 2

Application ASP.NET Core MVC .NET 10 construite à partir du modèle officiel fourni. L'interface permet désormais de discuter avec Albert pour remplir et corriger la fiche. L'IA propose des changements ; les champs autorisés, les limites et la complétude restent contrôlés en C#.

## Lancer

Prérequis : SDK .NET 10 stable. Depuis la racine du dépôt contenant `Siccrf.Guichet.slnx` :

```bash
dotnet restore Siccrf.Guichet.slnx
dotnet build Siccrf.Guichet.slnx --no-restore
dotnet test Siccrf.Guichet.slnx --no-build
dotnet run --project src/Siccrf.Guichet
```

Ouvrir http://localhost:5080. Le profil local utilise l'environnement Development et ne requiert pas de certificat HTTPS. Sans clé, l'interface et les corrections manuelles restent utilisables ; le chat indique sa configuration manquante. Bootstrap est inclus localement : aucun CDN n'est appelé par le navigateur.

Le fichier `global.json` accepte les bandes de fonctionnalités stables .NET 10 à partir de 10.0.100. Un SDK .NET 8 ou 9 seul ne suffit pas.

## Déjà utilisable

- Navbar et bascule entre demandeur et administrateur fictifs.
- Page demandeur avec message d'accueil et aperçu des 17 champs métier du Word.
- Identité fictive préremplie, séparation des 10 champs obligatoires et des 7 rubriques non bloquantes.
- Section « Renseigner ou corriger la fiche » : l'aperçu suit la saisie en JavaScript ; « Appliquer les corrections » valide les champs et conserve le brouillon en session serveur.
- Après application, progression et liste des manques calculées par le backend.
- Découverte des modèles via Albert ; seuls les identifiants réellement renvoyés pour la génération de texte sont acceptés.
- Conversation française, extraction de plusieurs champs par réponse, remplacement et effacement de valeurs corrigées.
- Aperçu, formulaire de correction, liste des manques et historique récent actualisés après chaque réponse acceptée.
- JSON strict : champs inconnus, doublons, types incorrects, longueurs excessives et extraits sources absents du message sont rejetés. La présence d'un extrait ne prouve pas à elle seule la fidélité sémantique d'une reformulation : relire la fiche reste nécessaire.
- Une réparation JSON au maximum dans le délai total de l'appel ; aucun changement si elle échoue.
- Gestion des accès refusés, modèles indisponibles, limitations avec Retry-After, erreurs réseau et timeouts sans divulguer les corps d'erreur.
- Révision serveur et sérialisation des requêtes d'une session pour éviter les écrasements entre onglets.
- Page administrateur vide : aucun brouillon présenté comme une demande soumise.
- Protection antiforgery des POST, valeurs encodées, liste de champs autorisés, limite des tailles et mises à jour atomiques.
- Tests ciblés des règles de fiche.

La saisie de corrections accepte une fiche incomplète pour permettre la rédaction progressive. En revanche, le backend refuse une tentative de soumission incomplète, même si le bouton désactivé est contourné. Une fiche complète n'est pas encore enregistrable : l'endpoint retourne un refus explicite jusqu'à l'étape 3.

## Limites explicites

Albert est raccordé, mais aucun appel réel n'a été validé avec une clé utilisateur dans cette livraison. Les tests utilisent un transport HTTP simulé, avec des identifiants de modèles fictifs réservés aux tests. La soumission SQLite, la confirmation d'envoi, la validation administrative et la génération Word restent à développer. Un « oui » dans le chat n'enregistre aucune demande à cette étape.

Les brouillons sont temporaires et propres à la session : deux heures d'inactivité maximum, perte au redémarrage du serveur ou à l'expiration de la session. La bascule de rôle dans la même session conserve le brouillon. Les rôles fictifs ne sont pas une authentification et ne conviennent pas à une publication en production. Enregistrer les modifications avant de changer de page ; les corrections non appliquées restent uniquement dans la page courante.

Seuls les six derniers tours de conversation sont conservés et transmis avec la fiche structurée courante. Les champs restent conservés même après retrait des anciens messages du contexte. Les corrections manuelles non appliquées doivent être enregistrées avant de reprendre le chat. Si la connexion est interrompue après envoi, recharger la page permet de voir si la réponse a été conservée avant de renvoyer le message.

## Configurer Albert localement

Ne pas envoyer la clé dans une conversation ni l'inscrire dans les sources. Le projet possède déjà un `UserSecretsId`. En développement, enregistrer localement la valeur :

```bash
dotnet user-secrets set "Albert:ApiKey" "VOTRE_CLE_LOCALE" --project src/Siccrf.Guichet
```

`VOTRE_CLE_LOCALE` est un marqueur à remplacer localement. Attention à l'historique du terminal sur un poste partagé. Dans Visual Studio, « Gérer les secrets utilisateur » permet aussi d'éditer le fichier hors du projet. User Secrets n'est pas chiffré et doit rester réservé au développement.

Alternativement, configurer `Albert__ApiKey` dans l'environnement du processus, sans fichier versionné. Les autres paramètres sont `Albert:BaseUrl`, `Albert:Model` et `Albert:TimeoutSeconds`. Le modèle reste vide intentionnellement.

Après configuration de la clé et redémarrage, cliquer sur **Actualiser** au-dessus du chat. La découverte utilise `GET /v1/models` côté serveur et garde le catalogue deux minutes en mémoire. Choisir dans le menu un modèle léger/rapide parmi ceux réellement accessibles au compte ; aucun modèle ni classement de vitesse n'est inventé. La sélection dans l'interface reste propre à la session. Si le fournisseur indique la taille des modèles ailleurs que dans le catalogue, s'appuyer sur cette information puis mesurer quelques échanges.

Pour conserver un modèle par défaut entre les sessions, configurer un identifiant réellement découvert :

```bash
dotnet user-secrets set "Albert:Model" "IDENTIFIANT_RENVOYE_PAR_MODELS" --project src/Siccrf.Guichet
```

Les placeholders ne sont ni une clé ni un nom de modèle à utiliser. Aucun appel Albert n'a été validé avec une clé réelle dans cette livraison.

La clé est envoyée uniquement dans l'en-tête Authorization des requêtes serveur, jamais dans le JavaScript, les URL ou les logs applicatifs. Les logs automatiques du client HTTP Albert sont désactivés, les redirections HTTP refusées et la réponse bornée à 512 Kio. La base doit utiliser HTTPS. `Albert:TimeoutSeconds` doit être compris entre 1 et 120 ; la valeur par défaut est 45 secondes pour le catalogue, la réponse et l'éventuelle réparation réunis. Aucun mode JSON Schema propre à un modèle n'est supposé : la sortie est demandée par le prompt puis validée strictement côté serveur.

## Fichiers principaux

| Fichier ou dossier | Rôle |
| --- | --- |
| Siccrf.Guichet.slnx | Solution application et tests. |
| src/Siccrf.Guichet/Program.cs | MVC, session, injection et middleware. |
| src/Siccrf.Guichet/Features/Demandes/FicheCatalogue.cs | Les 17 champs, leurs libellés, obligations et limites. |
| src/Siccrf.Guichet/Features/Demandes/FicheRegles.cs | Complétude et validation des mises à jour. |
| src/Siccrf.Guichet/Features/Demandes/DemandesController.cs | Page, corrections et refus de soumission. |
| src/Siccrf.Guichet/Features/Demandes/BrouillonSession.cs | Brouillon temporaire côté serveur. |
| src/Siccrf.Guichet/Features/Demandes/Views/Index.cshtml | Conversation, choix du modèle, aperçu et corrections. |
| src/Siccrf.Guichet/Features/Conversation | Contrat IChatService, transport Albert, catalogue, prompt, lecture JSON et orchestration des tours. |
| src/Siccrf.Guichet/Features/Demandes/SessionVerrous.cs | Sérialisation des requêtes de la même session sur cette application unique. |
| src/Siccrf.Guichet/Features/ComptesDemo | Identités et changement de rôle fictif. |
| src/Siccrf.Guichet/Features/Administration | Tableau administrateur vide. |
| src/Siccrf.Guichet/Views/Shared/_Layout.cshtml | Navigation et mention de démonstration. |
| src/Siccrf.Guichet/wwwroot | CSS, JavaScript natif et Bootstrap avec licence MIT. |
| tests/Siccrf.Guichet.Tests | Tests des règles critiques déjà implémentées. |
| reference/FEB(1).docx | Modèle fourni, conservé intact. |
| docs/Cadrage.md | Analyse exhaustive, modèle SQLite prévu et étapes d'implémentation. |
| docs/Prompt-systeme.md | Contrat conversationnel et lien vers le prompt exécuté. |
| docs/Verification.md | Vérifications réalisées et limites de validation. |

## Les cinq étapes

1. Interface MVC et règles de fiche — réalisée.
2. Conversation Albert et extraction fiabilisée — implémentée ; essai réel à effectuer avec la clé locale.
3. Confirmation explicite, SQLite, dashboard et validation administrative — à développer.
4. Export Word fondé sur le modèle — à développer.
5. Vérification du parcours complet et des reprises sur erreur — à effectuer.

Les dépendances EF Core SQLite et Open XML SDK seront ajoutées avec leurs fonctionnalités. La compilation et les tests ne nécessitent ni clé ni accès Albert.
