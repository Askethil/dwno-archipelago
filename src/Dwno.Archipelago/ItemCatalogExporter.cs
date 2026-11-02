using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace Dwno.Archipelago;

// One-shot table export on the Unity thread; never creates or grants an item.
public sealed class ItemCatalogExporter : MonoBehaviour
{
    private readonly ManualLogSource log = BepInEx.Logging.Logger.CreateLogSource("ItemCatalog");
    private readonly List<Dictionary<string, object?>> rows = new();
    private object? table;
    private PropertyInfo? indexer;
    private Type? indexType;
    private int count;
    private int cursor;
    private bool finished;
    private int failures;
    private DateTime nextAttempt;
    private string language = "unknown";

    public ItemCatalogExporter(IntPtr pointer) : base(pointer) { }

    public void Update()
    {
        if (finished || DateTime.UtcNow < nextAttempt) return;
        try
        {
            if (table is null)
            {
                nextAttempt = DateTime.UtcNow.AddSeconds(2);
                var assembly = Assembly.Load("Assembly-CSharp");
                var app = assembly.GetType("AppMainScript", true)!;
                var manager = Read(app, null, "parameterManager");
                if (manager is null || Read(manager.GetType(), manager, "isInitialized") is not true) return;
                var csv = Read(manager.GetType(), manager, "itemData");
                var parameters = csv is null ? null : Read(csv.GetType(), csv, "m_params");
                if (parameters is null) return;
                var length = Read(parameters.GetType(), parameters, "Length");
                if (length is null) throw new InvalidOperationException("Item table Length unavailable");
                count = Convert.ToInt32(length, CultureInfo.InvariantCulture);
                if (count == 0) return;
                if (count < 0 || count > 20000) throw new InvalidOperationException($"Unexpected item count {count}");
                var langType = assembly.GetType("Language", true)!;
                language = langType.GetMethod("GetLanguageFileCode", BindingFlags.Public | BindingFlags.Static,
                    null, Type.EmptyTypes, null)?.Invoke(null, null) as string ?? "unknown";
                indexer = parameters.GetType().GetProperties().Single(p => p.Name == "Item" && p.GetIndexParameters().Length == 1);
                indexType = indexer.GetIndexParameters()[0].ParameterType;
                table = parameters;
                log.LogInfo($"[CATALOG] START language={language} tableRows={count} batch=10");
            }
            for (var batch = 0; batch < 10 && cursor < count; batch++, cursor++)
            {
                var item = indexer!.GetValue(table, new[] { Convert.ChangeType(cursor, indexType!, CultureInfo.InvariantCulture) });
                if (item is null)
                {
                    rows.Add(new Dictionary<string, object?> { ["tableIndex"] = cursor, ["error"] = "null-table-entry" });
                    continue;
                }
                var type = item.GetType();
                var row = new Dictionary<string, object?> {
                    ["tableIndex"] = cursor,
                    ["gameId"] = Read(type, item, "m_id"),
                    ["kindValue"] = Read(type, item, "m_kind"),
                    ["classValue"] = Read(type, item, "m_class"),
                    ["sortId"] = Read(type, item, "m_sortID"),
                    ["basePrice"] = Read(type, item, "m_price"),
                    ["initialPossession"] = Read(type, item, "m_initialPossession")
                };
                try
                {
                    row["name"] = type.GetMethod("GetName", Type.EmptyTypes)?.Invoke(item, null) as string;
                    row["description"] = type.GetMethod("GetDescription", Type.EmptyTypes)?.Invoke(item, null) as string;
                }
                catch (Exception ex) { row["localizationError"] = ex.GetBaseException().Message; }
                rows.Add(row);
            }
            if (cursor < count) return;
            var ids = rows.Where(r => r.ContainsKey("gameId") && r["gameId"] is not null)
                .Select(r => Convert.ToUInt32(r["gameId"], CultureInfo.InvariantCulture)).ToArray();
            var missingNames = rows.Count(r => !r.TryGetValue("name", out var n) || string.IsNullOrWhiteSpace(n as string));
            var duplicateIds = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            var export = new {
                schemaVersion = 1, exportedAtUtc = DateTime.UtcNow, languageCode = language,
                source = "AppMainScript.parameterManager.itemData.m_params",
                coverage = "Loaded base item table only; pool eligibility and full coverage require review.",
                tableRowCount = count, uniqueIdCount = ids.Distinct().Count(), missingNameCount = missingNames,
                duplicateIds, items = rows
            };
            var folder = Path.Combine(Paths.BepInExRootPath, "exports");
            Directory.CreateDirectory(folder);
            var safeLanguage = string.Concat(language.Where(c => char.IsLetterOrDigit(c) || c == '-'));
            if (safeLanguage.Length == 0) safeLanguage = "unknown";
            var path = Path.Combine(folder, $"items-{safeLanguage}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));
            finished = true;
            log.LogInfo($"[CATALOG] DONE rows={count} uniqueIds={ids.Distinct().Count()} missingNames={missingNames} duplicates={duplicateIds.Length} path={path}");
        }
        catch (Exception ex)
        {
            failures++;
            nextAttempt = DateTime.UtcNow.AddSeconds(2);
            log.LogError($"[CATALOG] FAILED attempt={failures}: {ex.GetBaseException().Message}");
            if (failures >= 3) { finished = true; log.LogError("[CATALOG] DISABLED; game data unchanged."); }
        }
    }

    private static object? Read(Type type, object? instance, string name) =>
        type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic |
            (instance is null ? BindingFlags.Static : BindingFlags.Instance))?.GetValue(instance);
}
