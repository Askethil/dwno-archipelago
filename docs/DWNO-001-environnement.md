# DWNO-001 — Environnement reproductible

## Choix

Sources C# conservées dans Git ; SDK verrouillé à .NET 6.0.428 (dernier patch 6.0 accepté). Le ciblage .NET 6 correspond au runtime embarqué par la build BepInEx retenue ; ne pas changer la cible sans vérifier ce runtime. BepInEx est verrouillé à `6.0.0-be.791+b782da4`, Windows IL2CPP x64. Son archive est contrôlée par SHA-256 avant extraction. Cette empreinte assure la reproductibilité, pas une signature de l'éditeur.

Les références sont locales, sans versions NuGet flottantes. Notre plugin minimal n'a besoin ni de Harmony ni des interop du jeu. Les chemins personnels, sauvegardes de DLL, archives et binaires restent sous `.local/` ou dans le dossier du jeu, hors Git.

## Préparer un checkout

Installer le SDK .NET 6.0.428 si absent. Depuis PowerShell, à la racine du dépôt :

```powershell
./scripts/dev.ps1 Setup -GamePath 'C:/Program Files (x86)/Steam/steamapps/common/Digimon World Next Order'
./scripts/dev.ps1 Doctor
./scripts/dev.ps1 Build
```

Setup télécharge l'archive officielle si nécessaire, vérifie son empreinte, extrait les références et enregistre `.local/environment.json`. Il n'installe pas automatiquement le chargeur dans le jeu et ne remplace pas une installation BepInEx existante.

Pour une nouvelle installation du jeu : extraire le contenu de `.local/BepInEx-791/` à côté de l'exécutable, après avoir vérifié qu'aucun autre chargeur n'est présent. Lancer une fois via Steam pour générer les interop. Sur la machine de développement actuelle, cette installation a déjà été effectuée.

## Boucle de développement

Fermer le jeu normalement, puis :

```powershell
./scripts/dev.ps1 Deploy
# Lancer le jeu depuis Steam et attendre son chargement.
./scripts/dev.ps1 Verify
```

Deploy recompile, sauvegarde l'ancienne DLL dans `.local/backups/`, copie seulement notre DLL puis compare les empreintes. Il refuse de remplacer le plugin pendant que le jeu tourne. Verify compare la DLL compilée à celle installée, refuse un journal antérieur à la DLL, puis recherche le message du plugin et la fin du démarrage BepInEx. Une vérification visuelle du menu complète ce contrôle.

## WSL et VS Code

Le checkout actuel se trouve sur Windows. Dans WSL, y accéder avec :

```bash
cd /mnt/c/Users/Guillaume/Documents/ChatGPT/test
code .
pwsh.exe -NoProfile -File scripts/dev.ps1 Doctor
pwsh.exe -NoProfile -File scripts/dev.ps1 Build
```

Ces commandes utilisent PowerShell 7 (`pwsh.exe`) et invoquent le SDK Windows ; elles évitent de mélanger les sorties de compilations Linux et Windows. Le jeu, BepInEx et la validation s'exécutent sous Windows. Windows PowerShell 5 (`powershell.exe`) peut refuser les scripts selon sa politique d'exécution ; nous ne modifions pas cette politique.

## Critères d'acceptation

- Compilation Release réussie sans erreur.
- DLL déployée identique à la DLL compilée.
- Message `[DWNO-001] DWNO Archipelago 0.1.0 loaded successfully.` dans le journal du lancement testé.
- `Chainloader startup complete` puis menu du jeu visible.
- Aucun hook ni changement de gameplay activé.

La preuve du premier chargement est dans `docs/validation/DWNO-001.md`. Ne pas déclarer la vérification visuelle effectuée tant qu'elle n'a pas été observée.
