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
    }
}
