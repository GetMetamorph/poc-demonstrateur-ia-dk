# Vérifications du POC

## Étape 2 du 23 novembre 2026

La commande `dotnet test Siccrf.Guichet.slnx --no-restore -m:1 --nologo --verbosity minimal` a recompilé les projets puis exécuté **56 tests réussis**, zéro échec et zéro test ignoré, avec le SDK .NET 10.0.404. La compilation ne produit aucun avertissement. `node --check` valide la syntaxe de `conversation.js` ; `git diff --check` ne signale aucune erreur d'espacement.

Les tests de l'étape 1 sont conservés. Les ajouts couvrent le contrat JSON strict et les doublons, les champs autorisés et leurs longueurs, les extraits sources, la découverte filtrée et mise en cache des modèles, le refus d'un modèle inventé, les mises à jour multiples, les corrections et effacements, les réponses tronquées, une seule réparation, l'absence de mutation en cas d'échec, les erreurs 401/403/404/422/429/503, Retry-After, l'annulation et la non-divulgation des secrets ou corps d'erreur.

Dix tests d'intégration utilisent le véritable pipeline MVC avec WebApplicationFactory, sessions, antiforgery et transport HTTP Albert simulé. Ils vérifient la mise à jour de la fiche, la prise en compte d'une correction au tour suivant, le choix de modèle, le rejet d'une révision périmée, deux requêtes concurrentes, l'état après JSON invalide, le timeout 504, les sessions isolées, le refus du rôle administrateur et l'absence de soumission même sur une fiche complète suivie d'un « oui ».

Aucune clé utilisateur n'a été fournie : **aucun appel Albert réel ni modèle disponible au compte n'a été validé**. Les identifiants des tests sont factices et ne figurent pas dans la configuration applicative. La documentation et la spécification OpenAPI 0.9.3 ont été consultées. Les nouveaux composants JavaScript ne sont pas encore vérifiés dans un navigateur interactif ; les vues sont compilées et testées en HTTP. Les étapes SQLite, confirmation, validation administrative et export Word restent à développer.

## Historique de vérification de l'étape 1

Exécutées le 23 novembre 2026 sur Linux avec SDK .NET 10.0.404 et ASP.NET Core 10.0.15. Les versions utilisées sont celles du SDK téléchargé depuis la distribution officielle Microsoft. Le projet cible net10.0 et accepte un SDK stable .NET 10 avec ses bandes de fonctionnalités ultérieures.

## Compilation et tests

Commandes effectuées avec le SDK installé dans cet environnement :

```bash
dotnet build Siccrf.Guichet.slnx --nologo -m:1
dotnet test Siccrf.Guichet.slnx --no-build --nologo -m:1
node --check src/Siccrf.Guichet/wwwroot/js/fiche.js
```

Résultats : compilation réussie, zéro avertissement, zéro erreur ; **10 cas de tests xUnit réussis**, zéro échec et zéro test ignoré ; syntaxe JavaScript valide. La compilation séquentielle `-m:1` a été utilisée parce que cet environnement limite les sockets de communication des workers MSBuild. Le lancement standard sur un poste local n'a pas cette contrainte.

Les tests couvrent : dix champs obligatoires initialement manquants ; complétude avec rubriques facultatives vides ; cinq variantes de valeurs inconnues ou vides ; rejet atomique d'un champ interdit ; rejet atomique d'une valeur trop longue ; correction et effacement qui recalculent les manques.

## Vérifications HTTP sur l'application lancée

L'application a été lancée avec Kestrel et interrogée via de vraies requêtes HTTP avec cookies de session et jetons antiforgery. **13 contrôles réussis** :

1. Accueil MVC en HTTP 200, 17 champs affichés et progression initiale 0/10.
2. Routes de formulaire correctes et jetons antiforgery générés.
3. CSS, Bootstrap et JavaScript accessibles depuis l'application, sans CDN.
4. POST sans jeton antiforgery refusé en HTTP 400.
5. Champ `statut` interdit : rejet de la requête sans appliquer les autres champs.
6. Correction appliquée et conservée au rechargement ; progression serveur à 1/10 ; texte ressemblant à un script correctement encodé dans le HTML.
7. Nouvelle session isolée avec un brouillon vide.
8. Bascule vers l'administration et absence des brouillons dans la liste.
9. Correction refusée en HTTP 403 depuis le rôle administrateur.
10. Retour au demandeur avec conservation du brouillon de la session.
11. Soumission incomplète refusée en HTTP 400 côté backend.
12. Fiche complète à 10/10 avec les sept rubriques facultatives vides.
13. Soumission complète refusée en HTTP 409 tant que SQLite et la confirmation ne sont pas implémentées ; aucun faux succès.

## Document de référence

Les deux pages de `reference/FEB(1).docx` ont été lues intégralement, les parties principales, en-têtes, pieds de page, notes et tableaux ont été inspectés dans le package OOXML, puis les deux pages ont été rendues et inspectées visuellement. Aucun contrôle de contenu n'a été trouvé dans le modèle. Le fichier de référence n'a pas été modifié.

## Limites de vérification

Le lancement de Chromium automatisé est bloqué dans cet environnement par une restriction sur les sockets Unix. Le rendu visuel des pages web, l'exécution interactive de la prévisualisation JavaScript, les formats mobile et l'accessibilité complète n'ont donc pas été vérifiés dans un navigateur. Les vues Razor ont été compilées et servies ; les routes, formulaires et protections ont été vérifiés en HTTP.

Aucun test réel Albert n'a été effectué faute de clé fournie. La documentation et la spécification OpenAPI ont été consultées ; aucun catalogue authentifié de modèles n'a été obtenu. SQLite, la confirmation de soumission, la transition Validée et l'export Word restent des étapes à développer et ne sont pas présentés comme testés.
