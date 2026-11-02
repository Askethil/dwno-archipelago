# Observation des compétences du dresseur

Objectif : cartographier les événements et leurs effets, sans modifier les compétences, points ou Bits depuis le mod.

## Candidats repérés dans les interop locaux

| Cible | Observation prévue |
| --- | --- |
| `ParameterTamerSkill.AllResetLearningSkill()` | Entrée et retour du reset des compétences |
| `ParameterTamerSkill.SetLearningSkillFlag(skill, flag)` | Compétence passée en argument et état demandé |
| `PlayerData.SetSkillPoint(_value)` | Nouveau total demandé |
| `PlayerData.AddSkillPoint(addPoint)` | Variation demandée |
| `PlayerData.SetMoney/AddMoney` | Corrélation avec le paiement du PNJ |

Ces signatures sont présentes statiquement. Leur déclenchement et leur rôle exact restent à valider en jeu.

Le moniteur lit `m_level`, `m_skillPoint` et `m_TamerSkillFlag.m_BitFlagWork` toutes les 500 ms, sur le thread Unity. `TAMER_BASELINE` est l'état initial, `TAMER` l'état après changement et `TAMER_BEFORE` l'état précédent. Les mots de flags UInt64 sont affichés en hexadécimal ; leur correspondance avec les compétences n'est pas encore interprétée. Trois erreurs de lecture désactivent uniquement ce sous-moniteur.

## Essai préparé

L'utilisateur se trouve devant le PNJ et annonce un coût de reset de **10 000 Bits**. Il sauvegarde et ferme le jeu avant le déploiement. Après chargement :

1. Attendre la capture de l'état initial ; noter les points disponibles et le solde.
2. Valider un seul reset au PNJ ; relever les points après remboursement.
3. Apprendre une seule compétence ; annoncer son nom et son coût en points.

Résultat attendu à vérifier : événement de reset corrélé à une dépense de 10 000 Bits et à un changement de flags/points, puis événement d'apprentissage corrélé à la compétence choisie et aux points consommés. Le coût seul n'identifie pas un reset. Les états issus d'un chargement ne sont pas des apprentissages. Aucun appel de mutation n'est déclenché par l'agent ou le moniteur.

Cette branche est basée sur `codex/observation-achats-materiaux` et sa PR doit cibler cette branche pour isoler le nouveau travail tant que la PR d'observation initiale n'est pas fusionnée.

## Reset observé le 2 novembre 2026

À 18:27:58 (Paris), `ParameterTamerSkill.AllResetLearningSkill()` est appelé. Pendant cet appel, 42 appels à `PlayerData.AddSkillPoint` remboursent au total 265 points. L'instantané confirme : niveau 41 inchangé, points `4 → 269`, flags `[002FF0F07A17F77E,00000000000000FE] → [0,0]`. Les anciens mots contenaient 42 bits actifs ; ne pas attribuer de nom à ces bits sans cartographie. L'utilisateur confirme 269 PC à l'écran.

Le reset et le remboursement sont donc observés sur cet essai. Aucun appel à `SetLearningSkillFlag` ni `SetSkillPoint` n'est nécessairement impliqué : les traces du reset montrent surtout les remboursements via `AddSkillPoint` et l'effacement final des flags. L'apprentissage reste à tester.

Le coût annoncé de 10 000 Bits n'apparaît pas dans le journal capturé à cette étape : le solde chargé était 435 615 Bits, sans nouvelle variation `MONEY` ni appel d'argent pendant le reset. Ne pas déclarer le paiement confirmé.

## Apprentissage observé : Hausse d'humeur

À 18:29:49 (Paris), l'utilisateur apprend **Basique → Friandises → Hausse d'humeur**, coût déclaré 3 PC. Les traces montrent successivement `SetLearningSkillFlag(skill=MOOD_UP, flag=True)` puis `AddSkillPoint(addPoint=-3)`. Le relevé confirme `269 → 266 PC`, niveau 41 inchangé, flags `[0,0] → [0000000000000002,0]` (bit 1 du premier mot activé).

La correspondance `Hausse d'humeur ↔ MOOD_UP` et la dépense de 3 PC sont corrélées à cet essai. L'apprentissage observé comporte deux opérations distinctes : activer le flag puis dépenser les points. Le mod ne déclenche aucune de ces opérations. Une future attribution Archipelago devra définir explicitement si elle consomme des PC et comment elle gère prérequis, effets et doublons.

## Basique → Friandises : groupe acheté

L'utilisateur confirme avoir tout acheté dans ce sous-groupe. Quatre apprentissages sont observés depuis le reset, chacun par `SetLearningSkillFlag(..., True)` suivi d'un `AddSkillPoint` négatif :

| Ordre | Nom interne | Nom français confirmé | PC dépensés | PC avant → après | Bit activé |
| --- | --- | --- | --- | --- | --- |
| 1 | `MOOD_UP` | Hausse d'humeur | 3 | 269 → 266 | 1 |
| 2 | `BOUNDS_UP` | À relever dans l'interface | 3 | 266 → 263 | 2 |
| 3 | `CONFIDENCE_UP` | À relever dans l'interface | 3 | 263 → 260 | 3 |
| 4 | `MIND_MASTER` | À relever dans l'interface | 15 | 260 → 245 | 4 |

Total : 24 PC, état final du premier mot de flags `000000000000001E`, second mot nul. Les bits sont déduits des instantanés successifs. Les noms internes ne doivent pas être traduits arbitrairement comme s'il s'agissait des libellés français ou anglais officiels.
