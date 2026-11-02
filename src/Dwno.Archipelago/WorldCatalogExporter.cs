using System;
using UnityEngine;

namespace Dwno.Archipelago;

public sealed class WorldCatalogExporter : MonoBehaviour
{
    private readonly WorldCatalogReader reader = new();
    public WorldCatalogExporter(IntPtr pointer) : base(pointer) { }
    public void Update() => reader.Step();
}
