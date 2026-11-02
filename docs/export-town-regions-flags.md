# Export des définitions de ville, carte et flags

Extraction en lecture seule depuis les tables chargées par `AppMainScript.parameterManager`. Le composant Unity traite 25 entrées par frame ; aucun appel à une méthode de déblocage, mutation ou sauvegarde.

## Tables demandées

- Ville : `townBlockData`, `townGradeUpData`, `townGradeUpMaterialsData`, `townAdditionDp`, `townAreaChgData`, `townJumpData`, `townJumpDestinationData`.
- Carte : `digiviceMapData`, `digiviceAreaDataArray`, `mapNameData`, `areaNameDataArray`.
- Conditions : `flagSet`.

Les champs scalaires `m_*` sont conservés avec leurs noms bruts. Les noms proviennent de `GetName()` quand disponible, ou de `Language.GetString(UInt32)` avec `m_LanguageCode`. Les tables qui n'ont pas de nom ne reçoivent pas de traduction inventée. Les tableaux de groupes conservent leur index de source : cet index n'est pas automatiquement un numéro de région.

## Exécution

Sauvegarder et fermer le jeu ; compiler/déployer avec `scripts/dev.ps1 Deploy`, puis relancer en anglais. Le réglage `Catalog.ExportWorld=true` active un export unique par lancement. Le résultat est écrit dans `BepInEx/exports/world-en-<date UTC>.json`. Rechercher `[WORLD] DONE` dans le journal et contrôler les compteurs et statuts par table.

## Interprétation pour les tests

Les références des bâtiments vers améliorations, des améliorations vers matériaux, et les noms/IDs des flag sets donnent des pistes pour instrumenter la progression. Ne pas interpréter une valeur négative ou zéro comme une absence de condition sans analyser les conventions de la table.

Les données du Digivice décrivent notamment la carte et ses conditions d'affichage/sélection. Elles ne prouvent pas qu'une transition physique est franchissable. De même, un flag de zone visitée ne vaut pas un flag de déblocage. Les recrutements de PNJ et les prérequis narratifs doivent être reliés à des événements de gameplay observés avant de parler d'un unlock validé.

Les tables absentes, nulles ou vides sont signalées explicitement. Si l'export au menu est incomplet, charger une sauvegarde puis refaire une extraction lors d'un prochain lancement. Aucun résultat n'est annoncé comme complet avant vérification. Les exports bruts restent locaux jusqu'à leur revue ; aucune ressource binaire du jeu n'est versionnée.

## Résultat validé le 2 novembre 2026

L'export `_en` contient 42 tables, 2 416 lignes et zéro erreur de lecture ; toutes les tables demandées sont présentes. Les noms de 21 définitions d'amélioration et de 16 régions sont résolus. Sur 161 entrées de noms de zones, une ressource manque (groupe source 0, ligne 0, hash 2414990469) : le message japonais de ressource introuvable est remplacé par `name=null` et un statut explicite dans les données publiées. Les 160 autres noms sont résolus.

Les 1 738 flag sets, 56 recettes de matériaux et 141 définitions de points de développement sont conservés. Le document [world-catalog.en.md](world-catalog.en.md) donne la base de tests, les noms de régions et les noms des zones. Les [liens d'amélioration](../data/town-links.en.json) joignent les IDs d'amélioration aux définitions de flag sets et recettes existantes ; les matériaux sont reliés au catalogue d'objets anglais. Les références non résolues restent présentes, sans interprétation arbitraire comme sentinelles.

L'exporteur ne lit pas l'état actuel des flags de scénario, ne déclenche aucun événement et ne valide pas encore les recrutements ou déblocages. Le jeu reste répondant après l'extraction.
