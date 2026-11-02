using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace Dwno.Archipelago;

// Unity calls Update on the main thread. No background access to game objects.
public sealed class SnapshotMonitor : MonoBehaviour
{
    private static ManualLogSource? log;
    private static Type? storageType;
    private static Type? managerType;
    private Dictionary<string, long>? previous;
    private string? previousPanel;
    private string? slot;
    private DateTime nextSample;
    private DateTime nextHeartbeat;
    private int failures;

    public SnapshotMonitor(IntPtr pointer) : base(pointer) { }

    internal static void Initialize(ManualLogSource logger)
    {
        log = logger;
        var assembly = Assembly.Load("Assembly-CSharp");
        storageType = assembly.GetType("StorageData", true);
        managerType = assembly.GetType("MainGameManager", true);
        log.LogInfo("[SNAP] READY interval=500ms inventory=deltas panel=changes thread=Unity-main");
    }

    public void Update()
    {
        var now = DateTime.UtcNow;
        if (now < nextSample || failures >= 3) return;
        nextSample = now.AddMilliseconds(500);
        try
        {
            var inventory = Get(storageType!, null, "m_ItemStorageData");
            var player = Get(storageType!, null, "m_playerData");
            if (inventory is null || player is null)
            {
                previous = null;
                previousPanel = null;
                return;
            }
            var currentSlot = Scalar(Get(storageType!, null, "m_saveSlot"));
            if (slot != currentSlot) { previous = null; previousPanel = null; slot = currentSlot; }
            var current = Inventory(inventory);
            if (previous is null)
                log?.LogInfo($"[SNAP] {now:O} BASELINE slot={slot} entries={current.Count}");
            else
                foreach (var key in previous.Keys.Union(current.Keys))
                {
                    previous.TryGetValue(key, out var oldCount);
                    current.TryGetValue(key, out var newCount);
                    if (newCount != oldCount)
                        log?.LogInfo($"[SNAP] {now:O} INVENTORY slot={slot} {key} before={oldCount} after={newCount} delta={newCount - oldCount}");
                }
            previous = current;

            var manager = Get(managerType!, null, "m_instance");
            if (manager is not null)
            {
                var panel = Get(manager.GetType(), manager, "itemPickUI");
                if (panel is not null)
                {
                    var point = Get(panel.GetType(), panel, "m_targetItemPickPoint");
                    var pointInfo = point is null ? "none" : string.Join(" ", new[] { "id", "itemId", "isMaterial" }
                        .Select(n => $"{n}={Scalar(Get(point.GetType(), point, n))}"));
                    var ids = Get(panel.GetType(), panel, "m_itemIds");
                    var panelInfo = $"state={Scalar(Get(panel.GetType(), panel, "m_state"))} point=({pointInfo}) itemIds=[{string.Join(",", Elements(ids, 64).Select(Scalar))}]";
                    if (panelInfo != previousPanel)
                    {
                        log?.LogInfo($"[SNAP] {now:O} PANEL {panelInfo}");
                        previousPanel = panelInfo;
                    }
                }
            }
            if (now >= nextHeartbeat)
            {
                log?.LogInfo($"[SNAP] {now:O} HEARTBEAT slot={slot} entries={current.Count}");
                nextHeartbeat = now.AddSeconds(30);
            }
            failures = 0;
        }
        catch (Exception ex)
        {
            failures++;
            log?.LogWarning($"[SNAP] READ_FAILED count={failures} {ex.GetBaseException().Message}");
            if (failures == 3) log?.LogError("[SNAP] DISABLED after 3 read failures; event hooks remain active.");
        }
    }

    private static Dictionary<string, long> Inventory(object inventory)
    {
        var result = new Dictionary<string, long>();
        var table = Get(inventory.GetType(), inventory, "m_itemDataListTbl")
            ?? throw new InvalidOperationException("m_itemDataListTbl unavailable");
        var category = 0;
        foreach (var list in Elements(table, 32))
        {
            foreach (var item in Elements(list, 4096))
            {
                if (item is null) continue;
                var id = Get(item.GetType(), item, "m_itemID");
                var amount = Get(item.GetType(), item, "m_itemNum");
                if (id is null || amount is null) throw new InvalidOperationException("ItemData ID/count unavailable");
                var key = $"storage={category} itemId={Scalar(id)}";
                result.TryGetValue(key, out var count);
                result[key] = count + Convert.ToInt64(amount, CultureInfo.InvariantCulture);
            }
            category++;
        }
        return result;
    }

    private static object? Get(Type type, object? instance, string name) =>
        type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic |
            (instance is null ? BindingFlags.Static : BindingFlags.Instance))?.GetValue(instance);

    private static IEnumerable<object?> Elements(object? collection, int limit)
    {
        if (collection is null) yield break;
        var type = collection.GetType();
        var size = Get(type, collection, "Count") ?? Get(type, collection, "Length")
            ?? throw new InvalidOperationException($"Collection size unavailable: {type.Name}");
        var count = Convert.ToInt32(size, CultureInfo.InvariantCulture);
        if (count < 0 || count > limit) throw new InvalidOperationException($"Collection limit exceeded: {count}/{limit}");
        var indexer = type.GetProperties().Single(p => p.GetIndexParameters().Length == 1 && p.Name == "Item");
        var indexType = indexer.GetIndexParameters()[0].ParameterType;
        for (var i = 0; i < count; i++)
            yield return indexer.GetValue(collection, new[] { Convert.ChangeType(i, indexType, CultureInfo.InvariantCulture) });
    }

    private static string Scalar(object? value) => value is null ? "null" :
        value.GetType().IsPrimitive || value.GetType().IsEnum ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null" : $"<{value.GetType().Name}>";
}
