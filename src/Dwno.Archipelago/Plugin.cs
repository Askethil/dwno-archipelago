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
        if (Config.Bind("Catalog", "ExportItems", true, "Exporte une fois la table des objets et leurs noms localisés, sans modifier le jeu.").Value)
            AddComponent<ItemCatalogExporter>();
        if (Config.Bind("Catalog", "ExportWorld", true, "Exporte les tables ville, carte et flags en lecture seule.").Value)
            AddComponent<WorldCatalogExporter>();
        if (Config.Bind("Observation", "Enabled", true, "Trace les appels ciblés sans modifier le gameplay.").Value)
        {
            Observation.Install(Log);
            SnapshotMonitor.Initialize(Log);
            AddComponent<SnapshotMonitor>();
        }
    }
}
