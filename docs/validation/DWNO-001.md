# DWNO-001 — Validation locale

Date : 2 novembre 2026.

- Installation : `C:\Program Files (x86)\Steam\steamapps\common\Digimon World Next Order`.
- Jeu Windows x64, Unity `2019.4.11f1` selon BepInEx.
- BepInEx `6.0.0-be.791+b782da4ac54e1072d2857f4a2f77c80c3ef08005`, runtime embarqué .NET `6.0.7`.
- SHA-256 `GameAssembly.dll` : `F972297A08F91E8A2AB83CE0B927FF20F540FC69136FD44D2E1513BD12D3BB7F`.
- Compilation Release : réussie, zéro avertissement, zéro erreur.
- DLL déployée dans `BepInEx/plugins/Dwno.Archipelago/Dwno.Archipelago.dll`.
- Interop générés au premier lancement.
- Journal : `BepInEx/LogOutput.log`, copie conservée dans `DWNO-001-first-launch.log`.
- Message observé : `[DWNO-001] DWNO Archipelago 0.1.0 loaded successfully.` puis `Chainloader startup complete`.
- `ErrorLog.log` vide au contrôle.

Le critère de chargement et de journalisation est rempli. L'outil de capture de fenêtre a expiré (`FrameArrived timed out`) : l'affichage du menu n'a pas été vérifié visuellement. Aucune sauvegarde n'a été ouverte par l'agent. Les hooks de gameplay restent à valider.

La génération IL2CPP signale des champs et méthodes impossibles à restaurer ; cela n'a pas empêché le chargement du plugin minimal. Ne pas en déduire que toutes les méthodes du jeu sont disponibles pour les futurs patches.

## Revalidation de l'environnement reproductible

Le 2 novembre 2026 : `Setup`, `Doctor`, `Build`, `Deploy` puis `Verify` exécutés avec succès. SDK effectivement sélectionné : `6.0.428`. Compilation : zéro erreur, zéro avertissement. Ancienne DLL sauvegardée localement avant remplacement. Jeu relancé via Steam ; nouvelle DLL identique au build et message de chargement retrouvé dans le nouveau journal, suivi de `Chainloader startup complete`.

`Doctor` testé depuis Ubuntu WSL avec PowerShell 7 Windows. Windows PowerShell 5 refuse les scripts sur cette machine ; aucune politique d'exécution n'a été modifiée. Le guide utilise `pwsh.exe`.

La capture de la fenêtre du jeu expire encore ; la confirmation visuelle du menu reste à faire par l'utilisateur.
