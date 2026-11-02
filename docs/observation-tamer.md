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
