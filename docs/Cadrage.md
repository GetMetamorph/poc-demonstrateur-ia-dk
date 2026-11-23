# POC SICCRF Guichet intelligent d'expression de besoin

Ce cadrage s'appuie sur la lecture intégrale de `reference/FEB(1).docx`, de ses parties OOXML et de son rendu sur deux pages. Les étapes 1 à 3 sont implémentées : interface, conversation Albert, confirmation explicite, SQLite et validation administrative. L'essai Albert avec une clé réelle et l'export Word restent à effectuer. Le MVP reste une seule application MVC .NET 10 ; le serveur conserve toutes les décisions métier.

## 1 Analyse exhaustive du modèle

Le document contient cinq informations d'identification, dix-sept champs métier, une confirmation avec date, puis un circuit informatif de validation. Il ne comporte ni astérisques ni déclaration générale de champs obligatoires. « À renseigner » est une consigne ; les obligations ci-dessous sont donc des **décisions pragmatiques du POC**, sauf mentions explicites de caractère facultatif ou conditionnel.

### Identification et informations automatiques

| Libellé du Word | Source retenue | Règle du POC |
| --- | --- | --- |
| N° de demande | Serveur, après confirmation et à l'enregistrement | Unique, absent du brouillon. Exemple de forme : EB-2026-0001. L'année 2027 du Word est illustrative. |
| Demande exprimée par | Compte demandeur fictif Camille Martin | Prérempli et non modifiable par l'IA. |
| Date de la demande | Serveur | Date de transmission ; la date de création du brouillon est conservée séparément. |
| Bureau | Compte fictif Bureau Démonstration — SICCRF | Prérempli et non modifiable par l'IA. |
| Valideur | Compte fictif Alex Dupont | Valideur prévu en démonstration ; auteur effectif et date renseignés seulement au passage à Validée. |

Ces valeurs d'identité sont des données de démonstration choisies pour le POC, pas extraites du Word ou attribuées à un agent réel. Elles ne sont jamais à demander au chatbot. Le numéro et les dates ne sont jamais proposés par le modèle.

### Champs métier issus de la conversation ou des corrections

| Rubrique | Libellé exact | Clé | Mention du modèle | Règle POC |
| --- | --- | --- | --- | --- |
| Identification | Application ou service numérique concerné | application | À renseigner ou « nouveau service » | Obligatoire. « Nouveau service » accepté. |
| Identification | Intitulé court de la demande | intitule | À renseigner | Obligatoire. Proposition reformulée possible à partir des propos de l'agent. |
| 1 Synthèse | Résumé en quelques lignes | synthese | Problème, objectif et bénéfice principal | Obligatoire. Reformulation fidèle, sans faits ajoutés. |
| 2 Contexte | Situation actuelle | situationActuelle | Comment le travail est réalisé aujourd'hui | Obligatoire. |
| 2 Contexte | Difficultés rencontrées | difficultes | Limites, tâches manuelles, conséquences concrètes | Obligatoire. |
| 2 Contexte | Exemple représentatif | exemple | Explicitement facultatif, sans données sensibles inutiles | Facultatif. |
| 3 Objectifs | Résultat recherché | resultat | Ce qui doit devenir possible ou plus simple | Obligatoire. |
| 3 Objectifs | Critères de réussite | criteresReussite | Comment constater que le besoin est satisfait | Obligatoire. Un constat qualitatif suffit ; aucune métrique inventée. |
| 3 Objectifs | Solution envisagée par le demandeur | solution | Explicitement facultatif ; ne constitue pas une prescription technique | Facultatif et distinct du besoin. |
| 4 Périmètre | Utilisateurs ou services concernés | beneficiaires | Population et volumétrie estimée si connue | Population obligatoire ; volumétrie facultative. |
| 4 Périmètre | Processus métier concerné | processus | À renseigner | Obligatoire. Une activité formulée simplement suffit. |
| 4 Périmètre | Liens avec d'autres outils ou demandes | liens | Si connus | Facultatif, conditionnel. |
| 5 Bénéfices | Bénéfices attendus | benefices | Temps, qualité, fiabilité, conformité, service rendu | Obligatoire, sans imposer de chiffrage. |
| 5 Bénéfices | Échéance souhaitée et justification | echeance | Si contrainte réelle | Facultatif. Si une date impérative est annoncée, demander la raison sans bloquer la transmission pour une rubrique conditionnelle. |
| 5 Bénéfices | Contraintes identifiées | contraintes | Réglementaires, sécurité, données, organisation, accessibilité, si connues | Facultatif. Ne pas présenter une absence d'information comme une absence de risque. |
| 6 Compléments | Documents joints ou références utiles | references | Explicitement facultatif | Facultatif. Références textuelles ; pas de gestion de fichiers dans le MVP. |
| 6 Compléments | Points restant à clarifier | pointsAClarifier | Identifiés par le demandeur ou l'assistant IA | Facultatif, non bloquant à lui seul. Une ambiguïté sur un champ obligatoire est clarifiée avant d'en déclarer la valeur connue. |

Total : **10 champs métier obligatoires et 7 rubriques non bloquantes**. Les dix valeurs doivent être renseignées ; les marqueurs vides ou « À préciser » ne comptent pas. La complétude technique ne garantit pas la qualité métier : la relecture de l'agent reste nécessaire. Le POC n'impose pas une analyse sémantique de chaque phrase pour permettre l'envoi.

### Confirmation et validation

| Élément du Word | Traitement MVP |
| --- | --- |
| Case de confirmation du demandeur | Obligatoire avant enregistrement. Confirmation attachée à la version relue ; l'interface emploie désormais « transmettre à validation ». |
| Date de confirmation | Horodatage serveur, jamais valeur inventée par l'IA. |
| Identification du chef de service par référentiel ou sélection habilitée | Remplacée par le cadre fictif explicitement signalé. Pas d'annuaire réel. |
| Décision, date, auteur et commentaires | Statut, date et auteur tracés. Les commentaires restent vides dans le MVP ; saisie éventuelle hors périmètre indispensable. |
| Tableau Étape / Décision ou statut / Observations | Préservé comme information de circuit ; ce tableau ne dicte pas le workflow du POC. |
| Chef de service : À valider / Validée / À compléter / Refusée | Dans le portail MVP : seulement Transmise puis Validée. Les autres décisions ne sont pas implémentées. |
| Direction métier : Selon circuit applicable | Informatif ; aucune approbation simulée. |
| SICCRF : À instruire / En étude / Arbitrée | Informatif ; aucune instruction simulée. |
| Note indiquant que le portail fait référence pour les validations et leur historique | Conservée. Pour le MVP à transition unique, la ligne SQLite contient l'état, la date et l'auteur ; pas de moteur d'historisation général. |

L'entête ministériel, les titres, l'adresse postale, les coordonnées génériques de pied de page, la pagination et les explications sont des éléments de présentation, pas des champs à extraire de la conversation. Le modèle ne contient **aucun contrôle de contenu Word**. Le nombre de pages pourra augmenter avec la longueur des réponses ; la fidélité aux rubriques prime sur un plafond artificiel de deux pages.

## 2 Périmètre du MVP

**Indispensable** : page demandeur à deux panneaux ; accueil en français ; dialogue libre Albert ; extraction de plusieurs champs par message ; corrections ; aperçu ; complétude serveur ; relecture et confirmation explicite ; numéro unique et SQLite ; statuts Transmise et Validée ; bascule de compte fictif ; liste triée par date décroissante des seules demandes transmises ou validées ; détail ; téléchargement Word ; date et auteur de validation ; gestion des erreurs ; protection de la clé ; tests des règles critiques.

**Facultatif et reporté** : priorité explicable, synthèses du dashboard, PDF, streaming des réponses, sauvegarde durable des brouillons. Aucun de ces éléments ne doit retarder la démonstration.

**Exclu** : SSO et annuaire, authentification réelle, permissions avancées, mails, workflow hiérarchique réel, instruction RSSI/XA/SICCRF, CODIR/COPIL, Grist, Azure DevOps, pièces jointes binaires, analytics, microservices, Docker, broker, modèle local.

## 3 Architecture minimale

Un projet `Microsoft.NET.Sdk.Web` avec ASP.NET Core MVC `net10.0`, Razor, Bootstrap distribué localement, JavaScript natif. Un projet xUnit de tests. Les contrôleurs retournent les vues rangées avec leurs fonctionnalités ; aucun projet Core/Application/Infrastructure, repository générique, CQRS ou MediatR.

| Dossier prévu sous src/Siccrf.Guichet | Composants et responsabilité |
| --- | --- |
| Features/Demandes | FicheCatalogue, FicheRegles, FicheBrouillon, BrouillonSession, DemandesController et vues. Suivi du brouillon, mises à jour atomiques, manques et transmission. |
| Features/Conversation | IChatService, AlbertChatService, AlbertOptions, DTO JSON, prompt système et ConversationController. Appels HTTP et propositions ; aucune écriture en base par le fournisseur. |
| Features/Administration | Liste, détail, commande de validation et vues. |
| Features/ComptesDemo | Identités fixes, rôle en session, contrôleur de bascule. Ce rôle est une commodité de démo, pas une authentification. |
| Features/ExportWord | FicheWordExporter et modèle balisé dérivé du Word d'origine. |
| Persistence | GuichetDbContext, mapping EF Core et migrations SQLite, une seule table métier. |
| Views/Shared et wwwroot | Layout commun, Bootstrap, CSS et JavaScript. |

Ces dossiers sont créés au fur et à mesure de leur implémentation. À l'étape 3, Demandes, Conversation, Administration, ComptesDemo et Persistence sont présents ; ExportWord reste prévu. Les classes concrètes sont injectées directement ; seule l'intégration IA justifie une interface fournisseur.

### Modèle et SQLite à l'étape 3

`FicheBrouillon` contient les valeurs de la fiche, un identifiant technique, la date de création, la révision, un historique borné et une éventuelle confirmation en attente, distincte pour le chat. Il reste en session serveur pendant la rédaction, avec expiration après deux heures d'inactivité ; un redémarrage efface ces brouillons. L'identité provient du compte fictif.

`Demande` est un instantané figé des informations transmises. Les valeurs métier correspondent aux clés du catalogue et sont conservées ensemble dans une colonne JSON validée, plutôt que dupliquer leur mapping dans dix-sept propriétés EF. Les identités, dates, statut et identifiant de déduplication ont leurs colonnes SQL. Un `DbContext` direct suffit ; aucun repository supplémentaire.

| Colonnes de Demandes | Type SQLite et règle |
| --- | --- |
| Id | INTEGER PRIMARY KEY AUTOINCREMENT ; identifiant interne attribué à l'enregistrement. |
| BrouillonId | TEXT NOT NULL UNIQUE ; jeton technique de déduplication de la transmission, créé côté serveur, sans numéro public de brouillon. |
| DateCreationUtc | TEXT NOT NULL ; création du brouillon. |
| DateSoumissionUtc | TEXT NOT NULL ; horodatage serveur. Index pour le tri. |
| DateConfirmationUtc | TEXT NOT NULL ; confirmation effective. |
| ModeConfirmation | TEXT NOT NULL ; Bouton ou Chat. |
| Demandeur, Bureau, ValideurPrevu | TEXT NOT NULL ; instantanés des identités fictives. |
| ChampsJson | TEXT NOT NULL ; dictionnaire des rubriques validées par le catalogue. Dix champs obligatoires, sept rubriques non bloquantes ; le titre est lu dans l'instantané. |
| Statut | TEXT NOT NULL ; codes historiques `Soumise` et `Validée`. Le premier est affiché « Transmise » ; les données existantes restent compatibles. |
| DateValidationUtc, ValidePar | TEXT NULL ; tous deux renseignés lors de la transition Transmise vers Validée. |

Utiliser des `DateTime` UTC pour les horodatages persistés et le tri EF/SQLite. Présenter les dates dans le fuseau Europe/Paris. Le format public est calculé depuis l'Id et l'année de transmission dans ce fuseau : `EB-{annee}-{id:0000}`. La séquence est globale, sans remise à zéro annuelle ; elle reste unique. Le mot « Référence » peut être une propriété calculée, sans colonne dupliquée.

Transmission avec le commit EF Core : revalider la fiche, vérifier la confirmation de la bonne révision, insérer l'instantané, puis marquer le brouillon transmis après commit. L'unicité de BrouillonId rend les doubles clics et nouvelles tentatives idempotents. Le brouillon transmis est figé jusqu'au démarrage explicite d'une nouvelle demande. Aucun faux message de succès avant le commit. Validation admin atomique uniquement depuis Transmise ; une seconde validation ne remplace pas la date ni l'auteur initiaux.

### Export Word à l'étape 4

Conserver le fichier de référence intact. Préparer une copie avec des contrôles de contenu balisés, en reprenant les libellés et valeurs des paragraphes repérés dans le modèle ; éviter le remplacement global du XML brut, fragile quand Word répartit le texte sur plusieurs runs.

Le service Open XML SDK copie le modèle en mémoire, renseigne uniquement les balises autorisées, conserve en-têtes, logo, styles, sections, pied de page et tableau. Les textes sont écrits par les API OOXML, avec échappement XML, paragraphes et retours à la ligne corrects. Une rubrique facultative vide peut être affichée « Non renseigné ». La confirmation et sa date proviennent de SQLite. Le suivi du circuit indique clairement les étapes non implémentées ; aucune validation métier fictive supplémentaire n'est créée.

Le DOCX est généré à la demande depuis l'instantané enregistré, sans copie du fichier dans la base. Tester présence de toutes les rubriques, caractères spéciaux et textes longs, puis ouvrir/rendre un export avant de livrer cette étape.

## 4 Contrat conversationnel et Albert

Documentation consultée le 23 novembre 2026 : https://albert.api.etalab.gouv.fr/reference et https://albert.api.etalab.gouv.fr/openapi.json. La spécification téléchargée porte la version 0.9.3. Le rendu indexé de la documentation peut être plus ancien ; la spécification effective est utilisée pour le contrat.

- `GET https://albert.api.etalab.gouv.fr/v1/models` : authentification HTTP Bearer ; résultat `data[]`, notamment `id` et `type`.
- `POST https://albert.api.etalab.gouv.fr/v1/chat/completions` : Bearer, `model`, `messages`, `stream: false`.
- Filtrer les modèles sur `type == "text-generation"`. Le choix configuré doit figurer dans la réponse accessible au compte.
- Sans clé, impossible de vérifier les modèles disponibles. Aucun nom de modèle n'est présumé. Après découverte, choisir explicitement un modèle léger décrit comme tel par le catalogue ou le fournisseur ; la liste seule ne garantit pas la latence. Mesurer quelques dialogues de démonstration et garder l'identifiant configurable.
- La spécification autorise des propriétés supplémentaires mais ne garantit pas un support uniforme de `response_format: json_schema`. Le mode de base demandera un JSON strict dans les consignes et le validera en C#. Activer une contrainte de sortie du fournisseur seulement après vérification sur le modèle choisi.

`Albert:BaseUrl`, `Albert:Model`, `Albert:TimeoutSeconds` sont configurables. La clé utilise `Albert:ApiKey` dans User Secrets en développement ou `Albert__ApiKey` dans l'environnement. Aucun secret dans appsettings, le navigateur, les URL, les exceptions présentées ou les logs. User Secrets est extérieur au projet, sans chiffrement : il protège de la mise en source, pas d'un poste compromis.

### Fonctionnement d'un tour

1. Valider et borner le message utilisateur côté serveur. Le navigateur envoie le texte et une révision attendue, jamais un état réputé fiable de la fiche.
2. Envoyer les consignes système, le catalogue autorisé, les valeurs courantes, les champs manquants calculés en C#, et un historique court avec le nouveau message. La fiche structurée prévaut sur les anciennes formulations corrigées.
3. `IChatService` retourne un DTO de propositions. Le fournisseur ne reçoit pas le DbContext.
4. Désérialiser strictement : objet et types attendus, pas de propriétés supplémentaires, clés connues, pas de doublons, limites de longueur. Valider les sources citées comme extraits du dernier message. Cela améliore la traçabilité sans prouver sémantiquement chaque reformulation.
5. Appliquer l'ensemble des changements de façon atomique, recalculer les champs manquants et incrémenter la révision. Aucun succès si le JSON est invalide.
6. Afficher la réponse comme texte encodé ; le navigateur actualise l'aperçu. Le serveur contrôle séparément l'éventuelle proposition de relecture et la confirmation.

Le prompt complet proposé est dans `docs/Prompt-systeme.md`. Exemple de sortie, avec les clés du catalogue :

```json
{
  "message": "Je retiens la double saisie dans IRIS Géo. Qui effectue ces saisies aujourd'hui ?",
  "modifications": [
    {
      "champ": "application",
      "valeur": "IRIS Géo",
      "source": "Dans IRIS Géo"
    },
    {
      "champ": "difficultes",
      "valeur": "Les agents doivent saisir deux fois les mêmes informations.",
      "source": "on saisit deux fois les mêmes informations"
    }
  ]
}
```

Pas de clé `statut`, `complete`, `transmettre` ou d'identité dans ce contrat. `valeur: null` signifie effacer une information retirée par l'utilisateur. Une correction remplace la valeur complète du champ ; aucune concaténation systématique. Les champs omis ne changent pas. Les faits conservés doivent rester compatibles avec la correction et la réponse ne doit pas redemander une information déjà connue.

Le chatbot pose une question utile à la fois, deux questions liées au maximum. Il peut extraire plusieurs champs d'un seul récit, proposer un intitulé et une synthèse fidèles, distinguer résultat et solution, et passer les rubriques facultatives inconnues. La liste technique des manques est affichée dans l'interface ; elle ne devient pas un interrogatoire dans le chat.

### Confirmation contrôlée par le serveur

Quand les champs sont complets, le serveur propose la relecture et émet la question explicite de confirmation. Il garde `RevisionEnAttente` côté serveur. Le bouton « Confirmer et transmettre cette fiche » et le chat appellent la même commande applicative.

Une réponse comme « oui » ne vaut confirmation que si elle répond à cette question, sur la même révision, sans correction concomitante. Le serveur reconnaît une liste conservatrice d'accords autonomes comme « oui », « je confirme », « je confirme la transmission ». Une phrase ambiguë ou « oui, mais… » entraîne une clarification, pas un envoi. Toute correction invalide la confirmation ; une nouvelle relecture est proposée. L'IA n'arbitre pas l'accord et ne réalise jamais l'enregistrement.

### Erreurs

| Situation | Comportement prévu |
| --- | --- |
| Clé ou modèle absents | État de configuration explicite ; fiche et corrections utilisables. Aucun faux assistant de secours. |
| 401 / 403 | Message de configuration ou accès indisponible ; pas de corps d'erreur ni secret affiché. |
| 404 / 422 | Modèle absent ou type incorrect ; conserver le brouillon, redécouvrir le catalogue. |
| 429 | Indiquer la limitation, respecter Retry-After si présent, permettre une nouvelle tentative sans doublon. |
| 503 / 5xx / réseau | Indisponibilité temporaire, aucune mutation du brouillon, nouvelle tentative manuelle. |
| Timeout | Délai configurable de 45 secondes et annulation ; aucune mutation de fiche. |
| JSON invalide ou propositions interdites | Une tentative de réparation structurée au maximum dans le budget de délai total ; sinon aucune application et message clair. |
| SQLite indisponible à la transmission | Pas de succès ; brouillon conservé, nouvelle tentative idempotente possible. |

Les logs restent techniques : code HTTP, durée et identifiant de corrélation ; pas de corps de conversation, de fiche ou d'en-tête Authorization.

## 5 Développement par résultats utilisables

| Étape | Objectif et composants | Dépendances | Critère de validation |
| --- | --- | --- | --- |
| 1 Interface et règles, livrée | Projet MVC, layout Bootstrap, comptes fictifs, page chat d'accueil, catalogue, aperçu, corrections serveur et règles de complétude, page admin vide, tests ciblés | SDK .NET 10 ; aucune clé et aucune base | Application lancée, 17 rubriques présentes, correction reflétée et conservée après rechargement, champ facultatif vide non bloquant, POST incomplet refusé, bascule de compte et isolation des sessions. Chat et transmission signalés indisponibles. |
| 2 Conversation réelle | IChatService, HttpClient Albert, découverte du modèle, prompt et validation JSON, route de conversation, mises à jour JS | Étape 1 ; clé API et modèle réellement accessible | Un récit remplit plusieurs champs, une correction les remplace, les questions portent sur les manques, aucune invention visible dans un scénario relu ; erreurs et JSON invalide préservent la fiche. Tests HTTP avec fournisseur simulé et essai réel identifié. |
| 3 Parcours persistant | EF Core SQLite, migration initiale, commande de transmission, relecture et confirmation, liste/detail admin, validation datée | Étapes 1 et 2 pour la démo conversationnelle ; tests métier possibles sans API | Sans confirmation ou incomplet : refus. Après confirmation : une seule ligne Transmise, référence unique, visible après redémarrage, validation puis auteur/date ; pas de doublon après double clic et pas de brouillon en admin. Tests SQLite réels avec base temporaire. |
| 4 Word | Copie balisée du modèle, Open XML SDK, export depuis détail et téléchargement | Étape 3 et modèle original | DOCX ouvert et inspecté, logo/rubriques conservés, valeurs et confirmation exactes, bon statut informatif, texte long sans troncature. |
| 5 Vérification de démonstration | Tests des transitions et reprises, contrôle du scénario complet, instructions de lancement mises à jour | Étapes 1 à 4 | Demande libre fictive → corrections → relecture → confirmation → Transmise → détail → Word → Validée ; essais timeout, JSON invalide, manque obligatoire et double transmission. |

Les étapes 1 à 3 sont implémentées. Le parcours sans export peut déjà être démontré, avec remplissage manuel sans clé ou conversation Albert configurée. Le téléchargement Word reste à l'étape 4 ; les fonctionnalités facultatives attendent le parcours complet.

## 6 Vérifications et limites de la livraison

Voir `README.md` pour les commandes et `docs/Verification.md` pour les vérifications effectivement exécutées. La validation applicative vérifie des champs connus, longueurs et valeurs manquantes ; elle n'est pas une évaluation automatique de la pertinence du besoin. Le mode démo est explicitement impropre à la production et doit rester local.
