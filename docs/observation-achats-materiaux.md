# Essai : achats et ramassage de matériaux

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
