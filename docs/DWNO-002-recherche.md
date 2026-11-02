# DWNO-002 — Étude des mods existants

Analyse du code source le 2 novembre 2026. Les dépôts de référence sont téléchargés dans `research/` et exclus de Git. Aucun hook de gameplay n'est activé dans notre plugin.

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
