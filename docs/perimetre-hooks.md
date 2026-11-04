# Périmètre des prochains hooks

Les symboles ci-dessous sont repérés statiquement dans les interop locaux. Ils constituent des pistes, pas des hooks validés. Conserver les logs d'observation séparés des opérations qui modifieront réellement le jeu.

| Besoin | Symboles / données repérés | Validation nécessaire |
| --- | --- | --- |
| Bits du joueur | `PlayerData.GetMoney`, `SetMoney(money)`, `AddMoney(money)`, `m_Money` | Comparer le solde lu à l'écran, puis un gain et une dépense ; distinguer chargement, montant demandé et montant réellement appliqué |
| Objets et matériaux du joueur | `StorageData.m_ItemStorageData`, `ItemStorageData.m_itemDataListTbl`, `ItemData.m_itemID/m_itemNum` | Identifier les catégories joueur, réserve, matériaux et objets-clés ; capacités et limites |
| Ramassage | `uItemPickPanel.PickItem/PickDefault/PickMaterial`, `m_targetItemPickPoint`, `m_itemIds` | Point stable et crédit réussi ; une source peut donner plusieurs récompenses |
| Achats et ventes | `uShopPanel.Open/Close`, `Setup(open_shop_type, seller_no)`, `uShopPanelItem`, `uShopPanelNumChange` | Identifier la confirmation de transaction, le prix et les cas d'annulation ; corréler avec les deltas d'inventaire |
| Ajouter / supprimer des objets | `ItemStorageData.AddItemPlayer`, `_AddMaterialOrKeyItem`, `CScenarioScript.CmdExeGetItem/CmdExeRemoveItem` | Types exacts, capacité, résultat, objets-clés, persistance et réapplication ; aucun appel actif pour le moment |
| Changements de zone | `MainGameManager.RequestMapChange/RequestMapChangeForce`, `StorageData.m_mapData`, `MapData.m_mapNo/m_areaNo` | Distinguer demande, chargement, arrivée effective et changement de sauvegarde |
| Dialogues | `TalkMain.PlayVoiceText`, `CloseWindow`, `CommonMessageWindow`, `CScenarioScript.CmdExeDispMessageStr/CmdExeDispMessageLang` | Début/fin, identifiant d'événement, choix et résultat ; le texte seul n'est pas un ID stable |
| Cinématiques | `Movie.Play/IsPlaying/_PlayEnd`, `TalkMain.PlayMovie`, `CScenarioScript._RequestStartTalkEvent/TalkEventEndCB` | Distinguer vidéo, scène scriptée, fin normale et passage ; ne pas désactiver une scène liée à la progression |

## Ordre de travail

1. Valider les relevés d'inventaire sur achat, vente, objet au sol et récolte multiple. C'est le socle commun pour constater le résultat d'une action.
2. Identifier le contexte de transaction et les transitions de zone. Observer avant d'appeler des méthodes pour modifier le jeu.
3. Instrumenter les identifiants de dialogue et scénario, puis les débuts/fins de cinématique. Éviter les hooks génériques par référence et les fonctions natives partagées déjà suspectées dans les crashs.

Le moniteur actuel est en lecture seule. Il ne constitue ni une API d'ajout/retrait, ni une détection certifiée d'achat/vente, ni un système de checks Archipelago.
