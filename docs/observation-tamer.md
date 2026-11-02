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
