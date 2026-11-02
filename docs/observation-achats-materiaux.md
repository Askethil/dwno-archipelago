# Essai : achats et ramassage de matériaux

## Révision active : 5

La configuration actuelle conserve les quatre cibles de la première session et `uItemPickPanel.PickItem`, avec horodatages UTC et signatures. Attendu : `READY revision=5 installed=5 failed=0`. L'injection `__instance` et la lecture de contexte ont été retirées après les crashs ; aucune donnée de point n'est actuellement collectée.

La révision 3 se fermait également. La révision 4, sans injection d'instance, a produit des milliers d'appels à `ItemPickPointBase.PickItem` dès le démarrage, avec des arguments incohérents. Cette cible est retirée en révision 5. Une cible native partagée ou une incompatibilité de patch constitue une piste, pas un diagnostic établi. Ne pas utiliser les traces de révision 4 comme preuve de gameplay.

### Historique des essais de démarrage

La révision 2 a installé 16 patches, puis le processus s'est fermé avec une violation d'accès dans `coreclr.dll` lors de deux lancements. L'installation des patches et `Chainloader startup complete` ne suffisent donc pas à valider la stabilité du jeu.

La révision 3 retire `ListAdd`, `LisAddBase`, `SaveItemData` et `SaveItemDataBase` pour isoler le problème. Leur responsabilité précise n'est pas démontrée. Elle conserve les quatre cibles initiales et les deux nouvelles méthodes `ItemPickPointBase.PickItem` et `uItemPickPanel.PickItem`, avec horodatages et contexte. Attendu : `READY revision=3 installed=6 failed=0`. Vérifier également que le processus reste ouvert et que le menu est accessible avant les essais.

Les descriptions de la révision 2 ci-dessous sont historiques ; ses hooks d'inventaire supplémentaires sont désactivés.

Plugin d'observation uniquement. Quatre cibles : `ItemStorageData.AddItemPlayer`, `ItemStorageData._AddMaterialOrKeyItem`, `uItemPickPanel.PickDefault` et `uItemPickPanel.PickMaterial`. Aucun argument ni retour n'est modifié. Les objets du jeu ne sont pas sérialisés ; seuls les arguments primitifs, noms d'énumérations et types d'objets sont tracés.

La révision 2 ajoute `ItemPickPointBase.PickItem`, `uItemPickPanel.PickItem`, `ItemStorageData.ListAdd`, `LisAddBase`, `SaveItemData` et `SaveItemDataBase`, y compris leurs surcharges. Les noms de types des paramètres distinguent les surcharges ; chaque entrée et retour porte un horodatage UTC. Pour les méthodes de ramassage, les propriétés publiques `id`, `itemId` et `isMaterial` du point ciblé sont lues si disponibles. Aucune collection ni sauvegarde n'est parcourue.

Au lancement, rechercher les lignes `[OBS] PATCHED`, puis `[OBS] READY revision=2 installed=... failed=0`. Une erreur `PATCH_FAILED`, `MISSING` ou `INIT_FAILED` indique que l'instrumentation est incomplète. Le jeu peut continuer avec seulement une partie des traces : ne pas considérer l'absence d'événement comme une preuve d'absence d'appel. Certains appels d'inventaire peuvent provenir de l'initialisation ou d'une sauvegarde ; corréler les heures avec l'action testée.

Effectuer les essais séparément et relever l'objet, la quantité et le nombre détenu avant/après :

1. Acheter un objet ordinaire en boutique.
2. Acheter plusieurs exemplaires du même objet.
3. Ramasser un objet au sol.
4. Récolter un point de matériaux avec un partenaire.

Chaque appel produit `ENTER #n` et, s'il retourne normalement, `RETURN #n`. Des appels imbriqués peuvent montrer une corrélation ; ce n'est pas encore une preuve de crédit d'inventaire, d'origine boutique ou d'identifiant de check stable. Une opération différée peut aussi se poursuivre après `RETURN`. Le champ `point.itemId` est une donnée observée du point, pas nécessairement la récompense finale.

## Première session (2 novembre 2026)

L'utilisateur a signalé un achat de Plugin offensif S, puis 5 Plugins offensifs X : aucune entrée sur les quatre premières cibles. Après une récolte annoncée de 8 Digisèves, le journal comportait des appels à `PickMaterial` et `PickDefault`. Une Noix d'az au sol annoncée ensuite n'a produit aucune nouvelle trace. Ces constats ne permettent pas d'associer chaque appel à une récompense ni de conclure à l'absence de mise à jour d'inventaire. Les journaux bruts sont sauvegardés localement, hors Git.

Les traces sont dans `BepInEx/LogOutput.log`. Préserver ce fichier avant un nouveau lancement. Pour désactiver : fermer le jeu, mettre `Observation.Enabled = false` dans `BepInEx/config/uagboe.dwno.archipelago.cfg`, puis relancer. Cette instrumentation n'envoie rien à Archipelago.
