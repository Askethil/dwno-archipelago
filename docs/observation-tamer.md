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

## Basique → Vie : groupe acheté

L'utilisateur confirme la fin de Vie avant de passer à Porteur. Trois apprentissages capturés à 18:32:49–52 (Paris), chacun par activation du flag puis dépense via `AddSkillPoint` :

| Nom interne | Coût PC | PC avant → après | Bit activé |
| --- | --- | --- | --- |
| `LONGEVITY_EVOLUTION` | 3 | 245 → 242 | 5 |
| `LIFE_UP` | 3 | 242 → 239 | 6 |
| `LIFE_LARGE_UP` | 15 | 239 → 224 | 7 |

Total Vie : 21 PC. Premier mot final `00000000000000FE`, second mot nul. Noms français individuels à relever dans l'interface.

## Basique → Porteur : groupe acheté

L'utilisateur confirme la fin de Porteur avant de passer à Inverse. Quatre apprentissages capturés à 18:33:30–34 (Paris), chacun par activation du flag puis dépense via `AddSkillPoint` :

| Nom interne | Coût PC | PC avant → après | Bit activé |
| --- | --- | --- | --- |
| `HIGH_CARRY` | 5 | 224 → 219 | 8 |
| `MEGA_CARRY` | 8 | 219 → 211 | 9 |
| `GIGA_CARRY` | 12 | 211 → 199 | 10 |
| `MAX_CARRY` | 15 | 199 → 184 | 11 |

Total Porteur : 40 PC. Premier mot final `0000000000000FFE`, second mot nul. Noms français individuels à relever dans l'interface.

## Basique → Inverse : groupe acheté

L'utilisateur confirme la fin d'Inverse avant de passer à Entraîneur → Professeur. Quatre apprentissages capturés à 18:34:20–24 (Paris), chacun par activation du flag puis dépense via `AddSkillPoint` :

| Nom interne | Coût PC | PC avant → après | Bit activé |
| --- | --- | --- | --- |
| `DEJITAMA_SHINE` | 3 | 184 → 181 | 12 |
| `PASSION_INJECTION` | 7 | 181 → 174 | 13 |
| `LOVE_INJECTION` | 10 | 174 → 164 | 14 |
| `BLESSING_HEAVEN` | 12 | 164 → 152 | 15 |

Total Inverse : 32 PC. Premier mot final `000000000000FFFE`, second mot nul. Noms français individuels à relever dans l'interface. Les quatre groupes Basique observés totalisent 117 PC dépensés, de 269 à 152 PC.

## Entraîneur → Professeur : groupe acheté

L'utilisateur confirme la fin de Professeur avant de passer à Soins vitaux. Six apprentissages capturés à 18:35:32–37 (Paris), chacun par activation du flag puis dépense via `AddSkillPoint` :

| Nom interne | Coût PC | PC avant → après | Bit activé |
| --- | --- | --- | --- |
| `POWER_TRAINER` | 3 | 152 → 149 | 16 |
| `INTELLIGENT_TRAINER` | 3 | 149 → 146 | 17 |
| `TOUGHNESS_TRAINER` | 3 | 146 → 143 | 18 |
| `CHANCE_UP` | 8 | 143 → 135 | 20 |
| `FATIGUE_CARE` | 8 | 135 → 127 | 21 |
| `MASTER_TRAINER` | 15 | 127 → 112 | 19 |

Total Professeur : 40 PC. Premier mot final `00000000003FFFFE`, second mot nul. L'ordre d'achat ne correspond pas strictement à l'ordre des bits. Noms français individuels à relever dans l'interface.

## Entraîneur → Soins vitaux : groupe acheté

L'utilisateur confirme la fin de Soins vitaux avant de passer à Évolution. Cinq apprentissages capturés à 18:36:25–30 (Paris), chacun par activation du flag puis dépense via `AddSkillPoint` :

| Nom interne | Coût PC | PC avant → après | Bit activé |
| --- | --- | --- | --- |
| `EAT_CARE` | 3 | 112 → 109 | 22 |
| `TOILET_CARE` | 3 | 109 → 106 | 23 |
| `SLEEP_CARE` | 3 | 106 → 103 | 24 |
| `AFFECTION` | 3 | 103 → 100 | 25 |
| `LIFE_MASTER` | 12 | 100 → 88 | 26 |

Total Soins vitaux : 24 PC. Premier mot final `0000000007FFFFFE`, second mot nul. Noms français individuels à relever dans l'interface.

## Entraîneur → Évolution : groupe acheté

L'utilisateur confirme la fin d'Évolution. Quatre apprentissages capturés à 18:37:26–30 (Paris), chacun par activation du flag puis dépense via `AddSkillPoint` :

| Nom interne | Coût PC | PC avant → après | Bit activé |
| --- | --- | --- | --- |
| `GOOD_EVOLUTION` | 5 | 88 → 83 | 27 |
| `EVOLUTION_PROMOTION` | 5 | 83 → 78 | 28 |
| `EVOLUTION_VETERAN` | 8 | 78 → 70 | 29 |
| `GREAT_EVOLUTION` | 15 | 70 → 55 | 30 |

Total Évolution : 33 PC. Premier mot final `000000007FFFFFFE`, second mot nul. Noms français individuels à relever dans l'interface. Depuis le reset, 30 apprentissages sont documentés, pour 214 PC dépensés (269 → 55).

## Survivant → Trouver : groupe acheté

L'utilisateur confirme la fin de Trouver puis annonce un nouveau reset pour explorer les autres compétences. Cinq apprentissages capturés à 18:38:12–17 (Paris), chacun par activation du flag puis dépense via `AddSkillPoint` :

| Nom interne | Coût PC | PC avant → après | Bit activé |
| --- | --- | --- | --- |
| `WILD_FINDER` | 3 | 55 → 52 | 31 |
| `FISHERMAN` | 3 | 52 → 49 | 32 |
| `GURANDA` | 8 | 49 → 41 | 34 |
| `WILD_SCOUTER` | 8 | 41 → 33 | 33 |
| `INGREDIENTS_HUNTER` | 12 | 33 → 21 | 35 |

Total Trouver : 34 PC. Premier mot final `0000000FFFFFFFFE`, second mot nul. Noms français individuels à relever dans l'interface. Cette première série après reset documente 35 compétences, pour 248 PC dépensés (269 → 21). Journal sauvegardé localement avant le reset suivant ; remboursement et nouvel état à vérifier sur les prochaines traces.

## Deuxième reset observé

À 18:38:57 (Paris), le nouvel appel à `AllResetLearningSkill` est suivi du remboursement : `21 → 269 PC` (+248), flags `[0000000FFFFFFFFE,0] → [0,0]`. Cette fois, le relevé confirme également la dépense annoncée : `435615 → 425615 Bits`, delta `-10000`. Le coût est confirmé pour ce deuxième reset, pas rétrospectivement pour le premier.

## Survivant → Extracteur : groupe acheté

Précision sur le deuxième reset : juste après le retour de `AllResetLearningSkill`, un appel à `PlayerData.AddMoney(money=-10000)` est capturé, puis le solde diminue de 10 000 Bits. Le chemin `AddMoney` est ainsi observé pour cette dépense, distinct des appels à `SetMoney` des essais précédents.

L'utilisateur confirme la fin d'Extracteur avant de passer à Campeur. Cinq apprentissages capturés à 18:39:33–38 (Paris), chacun par activation du flag puis dépense via `AddSkillPoint` :

| Nom interne | Coût PC | PC avant → après | Bit activé |
| --- | --- | --- | --- |
| `METAL_MAN` | 5 | 269 → 264 | 36 |
| `STONE_MAN` | 5 | 264 → 259 | 37 |
| `LIQUID_MAN` | 5 | 259 → 254 | 38 |
| `WOOD_MAN` | 5 | 254 → 249 | 39 |
| `MATERIAL_MASTER` | 20 | 249 → 229 | 40 |

Total Extracteur : 40 PC. Premier mot final `000001F000000000`, second mot nul. Noms français individuels à relever dans l'interface. Le catalogue compte désormais 40 compétences distinctes observées, réparties sur deux séries après reset.

## Survivant → Campeur : trois compétences achetées

L'utilisateur annonce trois compétences de Campeur avant de passer à Marcheur. Trois apprentissages capturés à 18:40:29–32 (Paris), chacun par activation du flag puis dépense via `AddSkillPoint` :

| Nom interne | Coût PC | PC avant → après | Bit activé |
| --- | --- | --- | --- |
| `COOKING_BOY` | 5 | 229 → 224 | 41 |
| `MR_CUISINE` | 8 | 224 → 216 | 42 |
| `ULTIMATE_COOK` | 12 | 216 → 204 | 43 |

Total observé Campeur : 25 PC. Premier mot final `00000FF000000000`, second mot nul. Noms français individuels à relever dans l'interface. Le catalogue compte 43 compétences distinctes observées ; ne pas déduire d'achats réussis que toutes les compétences possibles du jeu sont couvertes.

## Survivant → Marcheur : groupe acheté

L'utilisateur confirme la fin de Marcheur avant de passer à Commandant → Ordre. Quatre apprentissages capturés à 18:41:18–23 (Paris), chacun par activation du flag puis dépense via `AddSkillPoint` :

| Nom interne | Coût PC | PC avant → après | Bit activé |
| --- | --- | --- | --- |
| `HEALTH_RUNNER` | 3 | 204 → 201 | 44 |
| `MENTAL_TRAINING` | 3 | 201 → 198 | 45 |
| `REFRESHING_RUNNER` | 7 | 198 → 191 | 46 |
| `HALE` | 10 | 191 → 181 | 47 |

Total Marcheur : 23 PC. Premier mot final `0000FFF000000000`, second mot nul. Noms français individuels à relever dans l'interface. Le catalogue compte 47 compétences distinctes observées.
