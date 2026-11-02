# Export du catalogue d'objets

L'exporteur BepInEx lit `AppMainScript.parameterManager.itemData.m_params`, puis appelle `GetName()` et `GetDescription()` sur chaque entrée. Le nom est celui de la langue chargée par le jeu ; il n'est ni traduit ni dérivé de l'ID. Les entrées vides ou en erreur sont conservées et comptabilisées.

Après préparation des dépendances locales, compiler et déployer avec `scripts/dev.ps1 Deploy`, jeu fermé. Le réglage `Catalog.ExportItems=true` active une extraction unique par lancement. Relancer le jeu avec la langue anglaise configurée ; l'export commence lorsque le gestionnaire de paramètres est initialisé, par lots de 10 entrées sur le thread Unity. Aucun objet n'est ajouté, retiré ou modifié.

Résultat : `BepInEx/exports/items-<langue>-<date UTC>.json`. Rechercher `[CATALOG] DONE` dans le journal. Le fichier contient les IDs UInt32, noms, descriptions, catégories numériques brutes, ordre et prix de base. Le prix de base ne doit pas être pris pour le prix effectif d'achat ou de vente sans validation.

Le rapport inclut la langue, le nombre de lignes, les IDs uniques, les doublons et les noms manquants. La couverture annoncée est celle de la table générale chargée, pas une garantie que tous les objets de tous les modes/versions sont présents. Aucun export n'est annoncé comme complet avant examen de ces compteurs et de quelques correspondances déjà observées.

## Préparer le pool Archipelago

1. Conserver cet export comme catalogue brut local, séparé du pool de récompenses.
2. Contrôler les noms et catégories, puis sélectionner les objets ordinaires, matériaux et autres récompenses autorisées.
3. Examiner individuellement les objets-clés, entrées internes, noms vides et effets spéciaux.
4. Valider l'attribution par le mod, les quantités, les limites de stockage et la persistance.
5. Définir des IDs Archipelago stables distincts des IDs du jeu.

Les ressources binaires et exports bruts restent locaux. Le code d'extraction est versionné. Cette branche dépend de l'instrumentation existante ; aucune fusion vers `main` n'est automatique.

## Langues

La documentation et les noms de récompenses Archipelago utilisent l'anglais comme référence. L'ID du jeu et l'ID Archipelago restent stables et distincts. Côté joueur, le mod résout le nom localisé avec `ParameterItemData.GetName()` dans la langue active. Il ne faut pas renommer les clés de récompenses en fonction de la langue du joueur, ni utiliser un libellé localisé comme identifiant de sauvegarde.

Un export dans une autre langue peut enrichir la documentation des correspondances, mais ne remplace pas le catalogue anglais de référence.

## Extraction validée le 2 novembre 2026

Le jeu a fourni le code de langue `_en`. Export terminé : **740 lignes, 740 IDs uniques, aucun nom manquant et aucun ID dupliqué**. Les noms des objets déjà observés concordent avec les IDs : Attack Plugin, Attack Plugin S, Medium Recovery Disk, Rolly Fruit et DigiWhitewood. Il existe 41 groupes de noms partagés par plusieurs IDs : conserver toutes les entrées par ID.

La liste lisible est dans [game-items.en.md](game-items.en.md), les données sélectionnées dans [game-items.en.json](../data/game-items.en.json). Les catégories proviennent des constantes locales de `ParameterItemData.KindIndex`. Les descriptions et le dump brut restent locaux. Le mod n'a attribué aucun objet ; la liste n'est pas encore un pool approuvé.
