using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace Dwno.Archipelago;

[BepInPlugin(Id, Name, Version)]
[BepInProcess("Digimon World Next Order.exe")]
public sealed class Plugin : BasePlugin
{
    public const string Id = "uagboe.dwno.archipelago";
    public const string Name = "DWNO Archipelago";
    public const string Version = "0.1.0";

    public override void Load()
    {
        Log.LogInfo($"[DWNO-001] {Name} {Version} loaded successfully.");
        StorySkip.Install(Log, Config.Bind("Story", "EnableHoldToSkip", false,
            "Prototype expérimental : F9 active/désactive pour la session ; maintenir F8 demande le passage natif. Désactivé par défaut.").Value,
            Config.Bind("Story", "AutoSelectFirstChoice", true,
                "Pendant le skip actif, choisit la première réponse des dialogues TalkMain via leur traitement natif. F10 désactive ce choix automatique. Expérimental.").Value,
            Config.Bind("Story", "AutoSkipStoryDialogue", true,
                "Active automatiquement le skip du texte dans les contextes histoire explicitement identifiés. F9 coupe tout le skip. Les scènes inconnues restent manuelles.").Value,
            Config.Bind("Story", "AcceleratePresentation", true,
                "Pendant le skip actif, raccourcit les durées positives de Wait, MoveGameCamera et RotationGameModel (facteur 0.1, minimum 0.05 seconde). Les valeurs spéciales restent intactes. Expérimental.").Value);
        AddComponent<StorySkipKeys>();
        if (Config.Bind("Catalog", "ExportItems", true, "Exporte une fois la table des objets et leurs noms localisés, sans modifier le jeu.").Value)
            AddComponent<ItemCatalogExporter>();
        if (Config.Bind("Catalog", "ExportWorld", true, "Exporte les tables ville, carte et flags en lecture seule.").Value)
            AddComponent<WorldCatalogExporter>();
        if (Config.Bind("Observation", "Enabled", true, "Trace les appels ciblés sans modifier le gameplay.").Value)
        {
            Observation.Install(Log);
            TutorialObservation.Install(Log);
            StoryPresentationObservation.Install(Log);
            SnapshotMonitor.Initialize(Log);
            AddComponent<SnapshotMonitor>();
        }
    }
}
