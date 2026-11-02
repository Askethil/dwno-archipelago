# DWNO-002 — Étude des mods existants

Analyse du code source le 2 novembre 2026. Les dépôts de référence sont téléchargés dans `research/` et exclus de Git. Aucun hook de gameplay n'est activé dans notre plugin.

Statut : analyse approfondie prête à relire ; validation des hooks en jeu hors de ce ticket. La comparaison locale ci-dessous complète l'étude des trois dépôts.

## Sources figées

| Dépôt | Commit étudié | Rôle |
| --- | --- | --- |
| [DWNOModdingGuides](https://github.com/Yasha-jin/DWNOModdingGuides/tree/0d6523cf3ae6e4cfdd94a7b8d69db20da806ad83) | `0d6523cf` | Installation |
| [FastPickup](https://github.com/SydMontague/DWNO-Mod-FastPickup/tree/0b973316f1b39eaa043d2d236abf2779e88e77bf) | `0b973316` | Plugin C# et Harmony |
| [DWNOHelper](https://github.com/AthosArantes/DWNOHelper/tree/9e25e5b43d1c82b0e86dfe5ca4b5e8231f563acf) | `9e25e5b4` | Hooks natifs et données partenaires |

## DWNOModdingGuides

Le guide prescrit BepInEx **Unity.IL2CPP Windows x64**, extrait à côté de l'exécutable. Un premier lancement génère les fichiers nécessaires ; les plugins vont dans `BepInEx/plugins`. Le dépôt est archivé depuis le 13 octobre 2026. Le numéro 667 illustré est un exemple historique, pas une version imposée. Le guide ne démontre aucun hook de gameplay.

## FastPickup

`FastPickup.csproj` cible .NET 6, référence `BepInEx.Unity.IL2CPP` 6 bleeding edge et `BepInEx.PluginInfoProps` 2 avec versions flottantes. Il dépend des DLL interop locales `Assembly-CSharp`, `UnityEngine.CoreModule` et `IL2Cppmscorlib`.

`Plugin.Load()` journalise son chargement puis appelle `Harmony.CreateAndPatchAll(typeof(Patches))`.

| Cible | Type | Comportement observé |
| --- | --- | --- |
| `uItemPickPanel.PickDefault` | Postfix | Met `m_itemPickTimer` et `m_waitTime` à zéro |
| `uItemPickPanel.Update` | Postfix | Réinitialise `m_waitTime` ; met `m_targetPartnerCtrl.EmotionIdx` à -1 si présent |
| `PlayerCtrl.StartFeelingAndChangeFace` | Prefix | Retourne false pour `FeelAnimID_Pickup`, supprimant cette animation |

Ces points révèlent le parcours visuel du ramassage, sans prouver où l'inventaire est effectivement crédité. Une animation n'est donc pas encore un check Archipelago fiable. Technique réutilisable : attributs Harmony et accès aux champs via les wrappers interop. Licence MIT ; conserver sa notice en cas de reprise de code.

## DWNOHelper

Architecture : DLL native C++17 x64, outil MSVC v143, `distormx` pour les détours et `inireader` pour la configuration. Le chargeur C# .NET 6 utilise BepInEx IL2CPP puis `NativeLibrary.Load`.

| Élément | Usage observé |
| --- | --- |
| `MainGameManager.Update` | Hook appelant l'original puis lisant la touche configurée |
| `MainGameManager.GetPartnerCtrl(0/1)` | Accès aux deux partenaires |
| `m_data.m_partnerData.m_lifetime` | Lecture de durée de vie, interprétée en minutes |
| `uFieldPanel.StartDigimonMessage` | Affichage temporaire par partenaire |
| `PartnerCtrl.IsPossibleMiracle` | Hook remettant `StorageData.PlayerData.m_isUseExe` à false selon option |

Autres symboles cartographiés : `MainGameManager.GetPlayerCtrl`, `Language.SystemLanguage`, `CommonMessageWindowManager.Get00/Get01`, `uCommonMessageWindow.SetMessage` et conversion UTF-8 vers chaîne IL2CPP.

`GameAssembly.h` contient des RVA constantes ajoutées à l'adresse de base du module dans `GameAssembly.cpp`. Cela gère le déplacement du module, pas une modification du binaire : une mise à jour peut invalider les adresses et les structures. Ne pas recopier ces RVA sans validation. Aucune licence globale explicite repérée : utiliser les découvertes comme pistes, vérifier les droits avant de reprendre du code.

## Choix pour notre intégration

1. **DWNO-001** : simple `BasePlugin.Load()` et message unique ; aucun patch ni dépendance aux assemblies du jeu.
2. Générer les interop de l'installation locale et relever la version Unity, BepInEx et l'empreinte de `GameAssembly.dll`.
3. Explorer `MainGameManager.Update` pour comprendre le cycle et les transitions. Ne pas effectuer de connexion réseau bloquante dans cette méthode ; exécuter les actions Unity sur le thread du jeu.
4. Explorer `uItemPickPanel.PickDefault` avec journalisation contrôlée : identifier l'objet, la zone, la quantité et l'appel réel d'ajout. Vérifier succès, annulation et répétition avant de choisir un check.
5. Rechercher ensuite les méthodes d'inventaire, recrutement, quêtes, sauvegarde et chargement. **Elles ne sont pas identifiées par ces trois dépôts.**

Les hooks sont attestés dans les sources historiques, pas encore validés sur le jeu installé. Les numéros de checks, l'attribution d'objets distants et leur persistance restent à concevoir. Prévoir des identifiants stables, la déduplication et l'isolation par slot/sauvegarde avant toute modification de progression.

## Documentation d'installation

[BepInEx : installation IL2CPP](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html) et [builds officielles](https://builds.bepinex.dev/projects/bepinex_be).

## Preuves dans les sources

| Sujet | Fichiers de référence |
| --- | --- |
| Installation Windows IL2CPP | [Guide](https://github.com/Yasha-jin/DWNOModdingGuides/blob/0d6523cf3ae6e4cfdd94a7b8d69db20da806ad83/Guides/HowToInstallBepInExForDWNO.md) |
| Dépendances C# et point d'entrée | [FastPickup.csproj](https://github.com/SydMontague/DWNO-Mod-FastPickup/blob/0b973316f1b39eaa043d2d236abf2779e88e77bf/FastPickup.csproj), [Plugin.cs](https://github.com/SydMontague/DWNO-Mod-FastPickup/blob/0b973316f1b39eaa043d2d236abf2779e88e77bf/Plugin.cs) |
| Trois patches de ramassage | [Patches.cs](https://github.com/SydMontague/DWNO-Mod-FastPickup/blob/0b973316f1b39eaa043d2d236abf2779e88e77bf/Patches.cs) |
| Détours, original et état des partenaires | [helper.cpp](https://github.com/AthosArantes/DWNOHelper/blob/9e25e5b43d1c82b0e86dfe5ca4b5e8231f563acf/src/helper.cpp) |
| RVA et relocalisation | [GameAssembly.h](https://github.com/AthosArantes/DWNOHelper/blob/9e25e5b43d1c82b0e86dfe5ca4b5e8231f563acf/src/GameAssembly.h), [GameAssembly.cpp](https://github.com/AthosArantes/DWNOHelper/blob/9e25e5b43d1c82b0e86dfe5ca4b5e8231f563acf/src/GameAssembly.cpp) |
| Pont managé / natif | [BepInExLoader/Plugin.cs](https://github.com/AthosArantes/DWNOHelper/blob/9e25e5b43d1c82b0e86dfe5ca4b5e8231f563acf/BepInExLoader/Plugin.cs) |

## Comparaison avec les interop de notre installation

Lecture statique de `BepInEx/interop/Assembly-CSharp.dll`, sans chargement ni exécution de l'assembly. Installation Windows décrite dans [la validation DWNO-001](validation/DWNO-001.md), Unity 2019.4.11f1, BepInEx 791. Les noms et paramètres ci-dessous proviennent des métadonnées locales ; ils ne démontrent ni l'ordre des appels, ni leurs effets, ni la compatibilité Harmony à l'exécution.

| Méthode ou accès | Constat local | Conséquence |
| --- | --- | --- |
| `uItemPickPanel.PickDefault()` et `Update()` | Présents | Les cibles FastPickup existent encore |
| `PlayerCtrl.StartFeelingAndChangeFace` | Paramètres `_animId, _animSpeed, _playTime, _target, _isLoop` | Un prefix recevant seulement `_animId` ne décrit pas toute la signature ; sélectionner explicitement la cible lors du futur patch |
| `MainGameManager.Update()` et `GetPartnerCtrl(_idx)` | Présents | Pistes pour observer le cycle du jeu et les partenaires |
| `PartnerCtrl.IsPossibleMiracle(isLose)` | Présent | Piste historique confirmée comme symbole, aucun déverrouillage ExE prévu |
| `uFieldPanel.StartDigimonMessage(id, message, time)` | Présent | Candidat pour un futur retour visuel |
| `CommonMessageWindowManager.Get00/Get01`, `uCommonMessageWindow.SetMessage(str, window_pos)` | Présents | Informations d'interface ; ne pas copier une signature native incomplète |
| `StorageData.m_playerData` | Accesseurs présents | Ne pas supposer que le nom natif historique `StorageData_get_PlayerData` est le nom C# actuel |
| `PlayerData.m_isUseExe`, `PartnerData.m_lifetime` | Accesseurs présents | Confirme les noms des données ; ne justifie aucune écriture |

## Nouvelles pistes issues de l'installation locale

Ces découvertes complètent les mods, elles ne leur sont pas attribuées. Les paramètres sont des noms, pas des signatures typées complètes. Plusieurs méthodes sont surchargées : inspecter les types exacts avant de configurer Harmony.

| Type | Symboles observés | Question à vérifier en jeu |
| --- | --- | --- |
| `ItemStorageData` | `AddItemPlayer(_itemId, _addNum)` ; `_AddMaterialOrKeyItem(_itemData, _addNum, _type)` | L'ajout est-il effectué ici pour les objets, matériaux et objets-clés ? Quel résultat en cas de sac plein ? |
| `ItemStorageData` | `IsHaveEmpty`, `IsHaveFull`, `GetItemData`, `IsGetItem`, `IsAllGetItemList`, `ListAdd`, `LisAddBase` | Quels appels contrôlent la capacité, et lesquels modifient réellement l'inventaire ? Le nom `LisAddBase` est bien celui observé |
| `ItemPickPointBase` | `id`, `itemId`, `GetItemId(index)`, `PickItem(requestPickCount, rarityRevision)` | L'identifiant est-il stable après changement de zone et rechargement ? |
| `ItemPickPointData` | `m_id`, `m_itemDatas`, `ReadSaveData`, `WriteSaveData` | Quels états de ramassage sont persistés ? |
| `ItemPickPointManager` | `TargetPoint`, `PickingPoint`, `GetItemPickPoint(id)`, `EnableItemPickPoint(id, enable)` | Comment relier zone, point et réussite d'un ramassage ? |
| `uItemPickPanel` | `PickItem`, `PickMaterial`, `m_targetItemPickPoint`, `m_itemIds`, `m_itemPickCallback`, `_PickDefault_b__36_0` | Le callback intervient-il après le crédit d'objet ? Nom généré fragile, à éviter comme contrat permanent |
| `StorageData` | `GetCurrentSaveSlot`, `IsBusySaveLoad`, `AutoLoadGameData`, `_ReadGameData`, `_WriteGameData` | Quand l'état est-il disponible et quand interdire les opérations entrantes ? |

La présence de `ReadSaveData` / `WriteSaveData` ne nous autorise pas à ajouter des octets à une sauvegarde. Commencer par observer ; concevoir ensuite une persistance indépendante indexée par slot et session Archipelago, avec reprise et déduplication.

## Ordre proposé pour le prochain ticket d'instrumentation

1. Observer `ItemStorageData.AddItemPlayer` et `uItemPickPanel.PickDefault/PickMaterial` sans modifier arguments ni résultat. Corréler les appels d'un ramassage connu ; limiter les logs aux événements, sans spam dans `Update`.
2. Relever le point, la carte/zone et l'objet ; comparer avant/après le nombre détenu et le retour de l'opération. Ne pas émettre de check sur une simple entrée dans le panneau.
3. Tester objet ordinaire, matériau, sac plein, annulation, ramassage répété, changement de zone et rechargement. Distinguer un point réapparaissant d'un check unique.
4. Observer les transitions de chargement et le slot. Aucune attribution distante pendant un chargement ; aucune écriture dans les sauvegardes à ce stade.

Choix recommandé : garder BepInEx IL2CPP C# et Harmony pour l'instrumentation. Utiliser les mods comme cartographie initiale, les wrappers locaux comme preuve de présence, puis les traces du jeu comme preuve de comportement. Aucun RVA fixe ni DLL native tierce n'est nécessaire à cette étape.

## Reproduire la lecture statique

L'utilitaire du dépôt lit uniquement les tables de métadonnées PE avec `System.Reflection.Metadata`, sans exécuter de code du jeu. Il affiche les types, noms de méthodes et noms des paramètres ; il ne décode pas leurs types, ne désassemble pas le code natif et ne prouve pas qu'une méthode est appelée.

```powershell
dotnet run --project tools/InspectInterop -- 'C:/chemin/du/jeu/BepInEx/interop/Assembly-CSharp.dll'
dotnet run --project tools/InspectInterop -- 'C:/chemin/du/jeu/BepInEx/interop/Assembly-CSharp.dll' ItemStorageData ItemPickPointBase ItemPickPointData ItemPickPointManager PartnerData
```

Code de sortie 0 si tous les types demandés sont présents, 2 si au moins un est absent. Les noms des types imbriqués ne sont pas qualifiés par l'utilitaire ; demander des noms uniques et contrôler le contexte avant d'en tirer une conclusion. Aucune DLL interop, dump intégral ou donnée de sauvegarde n'est ajouté à Git.
