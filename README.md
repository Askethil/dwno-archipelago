# Digimon World: Next Order — Archipelago

Début d'intégration pour la version Windows IL2CPP. Le plugin actuel écrit seulement un message : aucune modification du gameplay ni connexion Archipelago.

## Compiler

SDK .NET avec ciblage .NET 6. BepInEx sélectionné : `6.0.0-be.791+b782da4`, Unity.IL2CPP-win-x64, téléchargé depuis les builds officielles.

```powershell
dotnet build src/Dwno.Archipelago/Dwno.Archipelago.csproj -c Release
```

Par défaut, les références sont lues dans `.local/BepInEx-791/BepInEx/core`. Pour une installation existante :

```powershell
dotnet build src/Dwno.Archipelago/Dwno.Archipelago.csproj -c Release "-p:BepInExRoot=C:/chemin/du/jeu/BepInEx"
```

La sortie est `src/Dwno.Archipelago/bin/Release/net6.0/Dwno.Archipelago.dll`. Ne copier que cette DLL dans `BepInEx/plugins/Dwno.Archipelago/`.

## Vérifier DWNO-001

Installer BepInEx Windows IL2CPP x64 dans le dossier contenant `Digimon World Next Order.exe`. Lancer le jeu, attendre le menu puis rechercher dans `BepInEx/LogOutput.log` (extension de la build 791 ; les anciens guides indiquent `.txt`) :

```text
[DWNO-001] DWNO Archipelago 0.1.0 loaded successfully.
```

Le premier démarrage génère les interop et peut télécharger des dépendances Unity. La compilation seule ne valide pas le chargement en jeu.

SHA-256 de l'archive téléchargée (empreinte locale de traçabilité, pas signature éditeur) : `BB1C510909B69522733791F3A0817A487379A52DA6468A492F1C9C19B3EC6F9A`.

## Organisation

- `src/` : plugin C#.
- `docs/DWNO-002-recherche.md` : rapport sourcé et premières pistes.
- `.local/` et `research/` : dépendances et références locales ignorées.

Le jeu et sa validation restent côté Windows. Dépôt GitHub : https://github.com/Askethil/dwno-archipelago. Ne pas publier les binaires du jeu, interop générés ou sauvegardes.
