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
        var story = new StorySkipOptions
        {
            EnableManualOnStart = Config.Bind("Story", "EnableHoldToSkip", false,
                "Active le skip manuel au lancement ; F9 contrôle le skip et F8 le demande manuellement.").Value,
            AutoSkipStoryDialogue = Config.Bind("Story", "AutoSkipStoryDialogue", true,
                "Skip automatique des scènes d'histoire identifiées. F9 coupe le skip ; les dialogues inconnus restent manuels.").Value,
            AutoSkipMiniStoryDialogue = Config.Bind("Story", "AutoSkipMiniStoryDialogue", false,
                "Skip automatique des mini-histoires/recrutements identifiés. Séparé de l'histoire principale ; désactivé par défaut.").Value,
            AutoSelectFirstChoice = Config.Bind("Story", "AutoSelectFirstChoice", true,
                "Pendant le skip actif, choisit la première réponse TalkMain via le traitement natif. F10 rend les choix manuels.").Value,
            AcceleratePresentation = Config.Bind("Story", "AcceleratePresentation", true,
                "Pendant le skip actif, raccourcit les durées positives des pauses, caméras, rotations et fondus. Les valeurs spéciales restent intactes.").Value,
            FastChoiceConfirmation = Config.Bind("Story", "FastChoiceConfirmation", true,
                "Réduit les échéances proches de validation pendant le skip. Les temporisateurs inconnus restent intacts.").Value,
            AccelerateDialogueTransitions = Config.Bind("Story", "AccelerateDialogueTransitions", true,
                "Pendant le skip actif, accélère les Animators du fond et des fenêtres TalkMain, puis restaure leur vitesse.").Value,
            FastDialogueOpening = Config.Bind("Dialogue", "FastOpening", true,
                "Accélère toujours l'ouverture des dialogues (fondu, apparition et cadrage), même sans skip. Ne valide ni texte ni réponse et ne court-circuite pas le chargement.").Value
        };
        StorySkip.Install(Log, story);
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
