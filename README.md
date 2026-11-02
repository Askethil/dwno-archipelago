# Digimon World: Next Order — Archipelago

Début d'intégration pour la version Windows IL2CPP. Le plugin actuel écrit seulement un message : aucune modification du gameplay ni connexion Archipelago.

## Compiler

Pour préparer et utiliser l'environnement reproductible (SDK verrouillé, configuration locale, déploiement avec sauvegarde et contrôle des logs), suivre [DWNO-001 — Environnement](docs/DWNO-001-environnement.md).

La branche d'observation nécessite aussi les interop Unity locaux : après le premier lancement BepInEx, relancer `./scripts/dev.ps1 Setup` pour les copier dans `.local/interop`. Le plugin d'observation relève les variations d'inventaire et les changements du panneau sur le thread Unity. Voir [les essais](docs/observation-achats-materiaux.md) et [le périmètre des hooks](docs/perimetre-hooks.md).

```powershell
./scripts/dev.ps1 Doctor
./scripts/dev.ps1 Build
```

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

- [Story skip prototype](docs/story-skip-prototype.md) : traces de scènes et prototype F9/F8, désactivé par défaut et à valider sur une sauvegarde de test.

- [Town, region and flag test reference](docs/world-catalog.en.md) : définitions anglaises de ville, carte et conditions ; [données](data/world-catalog.en.json), [liens d'améliorations](data/town-links.en.json).

- [English game item catalogue](docs/game-items.en.md) : 740 entrées de la table générale, avec leurs IDs et noms lus dans le jeu ; [JSON](data/game-items.en.json).
- [Exporteur du catalogue](docs/export-item-catalog.md) : procédure, compteurs de couverture et séparation avec le futur pool Archipelago.

- [Catalogue du dresseur](docs/tamer-skills-catalogue.md) : 71 compétences observées, noms internes, coûts, flags et correspondances FR/EN des arbres.
- [Données structurées](data/tamer-skills.json) et [journal des essais](docs/observation-tamer.md).

- `src/` : plugin C#.
- `docs/DWNO-002-recherche.md` : rapport sourcé et premières pistes.
- `.local/` et `research/` : dépendances et références locales ignorées.

Le jeu et sa validation restent côté Windows. Dépôt GitHub : https://github.com/Askethil/dwno-archipelago. Ne pas publier les binaires du jeu, interop générés ou sauvegardes.
