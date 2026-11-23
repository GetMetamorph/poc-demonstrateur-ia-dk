# Guichet SICCRF POC étape 3

Application ASP.NET Core MVC .NET 10 construite à partir du modèle officiel fourni. Un agent fictif remplit sa fiche avec Albert ou par corrections manuelles, la relit et confirme sa soumission. La demande est enregistrée dans SQLite ; l'administrateur fictif la consulte et la valide. L'IA propose les données ; les règles, les confirmations et les statuts sont contrôlés en C#.

## Lancer

Prérequis : SDK .NET 10 stable. Depuis la racine du dépôt contenant `Siccrf.Guichet.slnx` :

```bash
dotnet restore Siccrf.Guichet.slnx
dotnet build Siccrf.Guichet.slnx --no-restore
dotnet test Siccrf.Guichet.slnx --no-build
dotnet run --project src/Siccrf.Guichet
```

Ouvrir http://localhost:5080. Le profil local utilise l'environnement Development et ne requiert pas de certificat HTTPS. Sans clé, l'interface et les corrections manuelles restent utilisables ; le chat indique sa configuration manquante. Bootstrap est inclus localement : aucun CDN n'est appelé par le navigateur.

La migration SQLite est appliquée automatiquement au démarrage. Le fichier par défaut est `src/Siccrf.Guichet/App_Data/guichet.db`, hors contrôle de version. Aucun serveur SQL, conteneur ou outil EF à installer pour lancer cette version. Le chemin peut être changé avec `Database:Path` ou la variable d'environnement `Database__Path` ; un chemin relatif est résolu depuis le projet web.

Le fichier `global.json` accepte les bandes de fonctionnalités stables .NET 10 à partir de 10.0.100. Un SDK .NET 8 ou 9 seul ne suffit pas.

## Déjà utilisable

- Navbar et bascule entre demandeur et administrateur fictifs.
- Page demandeur avec message d'accueil et aperçu des 17 champs métier du Word.
- Identité fictive préremplie, séparation des 10 champs obligatoires et des 7 rubriques non bloquantes.
- Section « Renseigner ou corriger la fiche » : l'aperçu suit la saisie en JavaScript ; « Appliquer les corrections » valide les champs et conserve le brouillon en session serveur.
- Après application, progression et liste des manques calculées par le backend.
- Découverte des modèles via Albert ; seuls les identifiants réellement renvoyés pour la génération de texte sont acceptés.
- Conversation française, extraction de plusieurs champs par réponse, remplacement et effacement de valeurs corrigées.
- Guide d'entretien déterministe en .NET : après chaque mise à jour validée, une question vise un champ obligatoire encore manquant. Les questions libres d'Albert ne sont pas affichées. Le guide couvre d'abord les informations métier, puis le titre et la synthèse s'ils manquent encore ; les champs déjà renseignés sont sautés.
- La question exacte et sa rubrique sont conservées en session et transmises à Albert pour interpréter une réponse courte. Une réponse insuffisante entraîne une demande de précision, jamais un remplissage arbitraire. L'entretien continue jusqu'à complétude des dix champs obligatoires, puis invite à relire sans demander les rubriques facultatives.
- Aperçu, formulaire de correction, liste des manques et historique récent actualisés après chaque réponse acceptée.
- Interface recentrée sur le chat, sans bannière supérieure ni bloc de présentation. La fiche est cachée au chargement ; le bouton « Afficher la fiche » ouvre l'aperçu et affiche toujours la complétude.
- Entrée envoie le message, Maj+Entrée conserve un retour à la ligne ; la composition de texte et les doubles envois sont pris en compte.
- Indicateur de préparation dans la conversation, état de l'assistant et bouton d'envoi animés pendant le traitement. Les animations respectent la préférence de réduction des mouvements du navigateur.
- Carillon discret uniquement à réception d'une réponse ; le bouton « Son activé / Son coupé » conserve la préférence localement. Si l'audio est indisponible, les retours visuels restent opérationnels.
- Les valeurs proposées par Albert sont rédigées de manière concise et neutre dans chaque rubrique ; les extraits sources restent verbatim pour valider leur provenance. Noms d'outils, chiffres, incertitudes et négations doivent être conservés. La relecture reste nécessaire.
- JSON strict : champs inconnus, doublons, types incorrects, longueurs excessives et extraits sources absents du message sont rejetés. La présence d'un extrait ne prouve pas à elle seule la fidélité sémantique d'une reformulation : relire la fiche reste nécessaire.
- Une réparation JSON au maximum dans le délai total de l'appel ; aucun changement si elle échoue.
- Gestion des accès refusés, modèles indisponibles, limitations avec Retry-After, erreurs réseau et timeouts sans divulguer les corps d'erreur.
- Révision serveur et sérialisation des requêtes d'une session pour éviter les écrasements entre onglets.
- Relecture de la fiche complète puis case de confirmation et bouton « Confirmer et soumettre cette fiche ».
- Confirmation dans le chat uniquement après une question explicite du serveur sur la version courante. « Oui, mais… » ne vaut pas accord ; une correction annule l'attente précédente. Une relecture par bouton ne suffit pas à autoriser un accord dans le chat sans sa propre question.
- Enregistrement SQLite avec référence unique, dates serveur et statut « Soumise » ; déduplication des doubles envois et des tentatives répétées après une interruption.
- Fiche soumise figée et récapitulatif consultable. Le bouton « Nouvelle demande » démarre un autre brouillon.
- Dashboard des demandes enregistrées, tri par date décroissante, détail des rubriques et validation en « Validée », avec auteur et date conservés même si le bouton est utilisé deux fois.
- Protection antiforgery des POST, valeurs encodées, liste de champs autorisés, limite des tailles et mises à jour atomiques.
- Tests ciblés des règles de fiche.

La saisie de corrections accepte une fiche incomplète pour permettre la rédaction progressive. Le serveur refuse tout envoi si la fiche est incomplète, si l'accord manque ou si la version relue a changé. Les rubriques facultatives vides ne bloquent pas. Un échec d'écriture conserve le brouillon ; aucun message de succès n'est affiché avant le commit SQLite.

## Démonstration sans clé Albert

1. Ouvrir « Afficher la fiche », puis « Renseigner ou corriger la fiche ».
2. Renseigner les dix champs obligatoires et appliquer les corrections.
3. Cliquer « Relire et soumettre », relire les valeurs, cocher la confirmation et soumettre.
4. Basculer vers Administrateur dans la navbar, ouvrir la demande et cliquer « Valider la demande ».
5. Redémarrer l'application : les demandes enregistrées restent visibles en administration.

Avec Albert configuré, la conversation remplit les mêmes champs et demande la confirmation explicite lorsqu'ils sont complets. Une réponse autonome telle que « Je confirme » soumet alors la demande sans nouvel appel au modèle.

## Limites explicites

Albert est raccordé, mais aucun appel réel n'a été validé avec une clé utilisateur dans cette livraison. Les tests utilisent un transport HTTP Albert simulé et SQLite réel. L'export Word reste prévu à l'étape 4 ; le détail l'indique explicitement et n'offre pas de faux téléchargement.

Les brouillons sont temporaires et propres à la session : deux heures d'inactivité maximum, perte au redémarrage du serveur ou à l'expiration de la session. La bascule de rôle dans la même session conserve le brouillon. Les rôles fictifs ne sont pas une authentification et ne conviennent pas à une publication en production. Enregistrer les modifications avant de changer de page ; les corrections non appliquées restent uniquement dans la page courante.

Les demandes soumises survivent au redémarrage, indépendamment de la session. La table `Demandes` contient les identités, dates, statut et mode de confirmation ainsi qu'un instantané des champs validés dans `ChampsJson`. L'historique de conversation n'est pas enregistré dans SQLite. La référence publique utilise l'année de soumission en Europe/Paris et l'identifiant global de la table, sans remise à zéro annuelle. Les dates de soumission et validation sont présentées en heure de Paris.

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
| src/Siccrf.Guichet/Features/Demandes/DemandesController.cs | Page, corrections, relecture, confirmation par bouton et récapitulatif. |
| src/Siccrf.Guichet/Features/Demandes/ConfirmationDemande.cs | Accords explicites, version relue et distinction bouton/chat. |
| src/Siccrf.Guichet/Features/Demandes/SoumissionService.cs | Validation finale, instantané, écriture et déduplication. |
| src/Siccrf.Guichet/Persistence | DbContext EF Core, contraintes SQLite et migration initiale. |
| src/Siccrf.Guichet/Features/Demandes/BrouillonSession.cs | Brouillon temporaire côté serveur. |
| src/Siccrf.Guichet/Features/Demandes/Views/Index.cshtml | Conversation, choix du modèle, aperçu et corrections. |
| src/Siccrf.Guichet/Features/Conversation | Contrat IChatService, transport Albert, catalogue, prompt, lecture JSON et orchestration des tours. |
| src/Siccrf.Guichet/Features/Demandes/SessionVerrous.cs | Sérialisation des requêtes de la même session sur cette application unique. |
| src/Siccrf.Guichet/Features/ComptesDemo | Identités et changement de rôle fictif. |
| src/Siccrf.Guichet/Features/Administration | Liste, détail, validation atomique et traçabilité. |
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
3. Confirmation explicite, SQLite, dashboard et validation administrative — implémentés et testés.
4. Export Word fondé sur le modèle — à développer.
5. Vérification du parcours complet et des reprises sur erreur — à effectuer.

EF Core SQLite est inclus. Open XML SDK sera ajouté avec l'étape 4. La compilation et les tests ne nécessitent ni clé ni accès Albert. Les tests de soumission utilisent des fichiers SQLite temporaires et exécutent la migration réelle.
