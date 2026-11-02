# Essai : achats et ramassage de matériaux

Plugin d'observation uniquement. Quatre cibles : `ItemStorageData.AddItemPlayer`, `ItemStorageData._AddMaterialOrKeyItem`, `uItemPickPanel.PickDefault` et `uItemPickPanel.PickMaterial`. Aucun argument ni retour n'est modifié. Les objets du jeu ne sont pas sérialisés ; seuls les arguments primitifs, noms d'énumérations et types d'objets sont tracés.

Au lancement, rechercher quatre lignes `[OBS] PATCHED`, puis `[OBS] READY installed=4`. Une erreur `PATCH_FAILED`, `MISSING` ou `INIT_FAILED` indique que l'instrumentation est incomplète. Le jeu peut continuer avec seulement une partie des traces : ne pas considérer l'absence d'événement comme une preuve d'absence d'appel.

Effectuer les essais séparément et relever l'objet, la quantité et le nombre détenu avant/après :

1. Acheter un objet ordinaire en boutique.
2. Acheter plusieurs exemplaires du même objet.
3. Ramasser un objet au sol.
4. Récolter un point de matériaux avec un partenaire.

Chaque appel produit `ENTER #n` et, s'il retourne normalement, `RETURN #n`. Des appels imbriqués peuvent montrer une corrélation ; ce n'est pas encore une preuve de crédit d'inventaire, d'origine boutique ou d'identifiant de check stable. Une opération différée peut aussi se poursuivre après `RETURN`.

Les traces sont dans `BepInEx/LogOutput.log`. Préserver ce fichier avant un nouveau lancement. Pour désactiver : fermer le jeu, mettre `Observation.Enabled = false` dans `BepInEx/config/uagboe.dwno.archipelago.cfg`, puis relancer. Cette instrumentation n'envoie rien à Archipelago.
