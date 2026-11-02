using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using BepInEx;
using BepInEx.Logging;

namespace Dwno.Archipelago;

// Only reads parameter tables. No scenario/region/unlock method is called.
internal sealed class WorldCatalogReader
{
    private sealed class Table
    {
        public string Source { get; init; } = "";
        public int? GroupIndex { get; init; }
        public string Status { get; set; } = "pending";
        public int RowCount { get; set; }
        public int NamedRows { get; set; }
        public List<Dictionary<string, object?>> Rows { get; } = new();
        public object? Parameters;
        public PropertyInfo? Indexer;
        public Type? IndexType;
        public int Cursor;
    }

    private readonly ManualLogSource log = Logger.CreateLogSource("WorldCatalog");
    private readonly List<Table> tables = new();
    private int tableIndex;
    private bool initialized;
    private bool done;
    private int failures;
    private DateTime nextAttempt;
    private string language = "unknown";
    private MethodInfo? languageLookup;

    internal void Step()
    {
        if (done || DateTime.UtcNow < nextAttempt) return;
        try
        {
            if (!initialized)
            {
                nextAttempt = DateTime.UtcNow.AddSeconds(2);
                var assembly = Assembly.Load("Assembly-CSharp");
                var manager = Read(assembly.GetType("AppMainScript", true)!, null, "parameterManager");
                if (manager is null || Read(manager.GetType(), manager, "isInitialized") is not true) return;
                var lang = assembly.GetType("Language", true)!;
                language = lang.GetMethod("GetLanguageFileCode", BindingFlags.Public | BindingFlags.Static,
                    null, Type.EmptyTypes, null)?.Invoke(null, null) as string ?? "unknown";
                languageLookup = lang.GetMethod("GetString", BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(uint) }, null);
                foreach (var source in new[] { "townBlockData", "townGradeUpData", "townGradeUpMaterialsData",
                    "townAdditionDp", "townAreaChgData", "townJumpData", "townJumpDestinationData",
                    "flagSet", "digiviceMapData", "mapNameData", "digiviceAreaDataArray", "areaNameDataArray" })
                {
                    var property = manager.GetType().GetProperty(source);
                    if (property is null) { tables.Add(new Table { Source = source, Status = "missing-property" }); continue; }
                    var csv = property.GetValue(manager);
                    if (source.EndsWith("Array", StringComparison.Ordinal) && csv is not null)
                    {
                        var length = Count(csv, 256);
                        for (var i = 0; i < length; i++) AddTable(source, i, At(csv, i));
                        if (length == 0) tables.Add(new Table { Source = source, Status = "empty-group-array" });
                    }
                    else AddTable(source, null, csv);
                }
                initialized = true;
                log.LogInfo($"[WORLD] START language={language} tables={tables.Count} batch=25");
            }

            var budget = 25;
            while (tableIndex < tables.Count && budget > 0)
            {
                var table = tables[tableIndex];
                if (table.Parameters is null || table.Cursor >= table.RowCount)
                {
                    if (table.Status == "reading") table.Status = "exported";
                    log.LogInfo($"[WORLD] TABLE {table.Source}[{table.GroupIndex}] status={table.Status} rows={table.Rows.Count} named={table.NamedRows}");
                    tableIndex++;
                    continue;
                }
                var index = table.Cursor++;
                budget--;
                var row = new Dictionary<string, object?> { ["rowIndex"] = index };
                try
                {
                    var value = table.Indexer!.GetValue(table.Parameters, new[] { Convert.ChangeType(index, table.IndexType!, CultureInfo.InvariantCulture) });
                    if (value is null) row["error"] = "null-entry";
                    else
                    {
                        var type = value.GetType();
                        row["recordType"] = type.Name;
                        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            if (!property.Name.StartsWith("m_", StringComparison.Ordinal) || property.GetIndexParameters().Length != 0) continue;
                            var ptype = property.PropertyType;
                            if (!ptype.IsPrimitive && !ptype.IsEnum && ptype != typeof(string)) continue;
                            try { row[property.Name] = property.GetValue(value); }
                            catch (Exception ex) { row[property.Name + "_error"] = ex.GetBaseException().Message; }
                        }
                        var getName = type.GetMethod("GetName", Type.EmptyTypes);
                        if (getName is not null)
                        {
                            try { row["name"] = getName.Invoke(value, null) as string; }
                            catch (Exception ex) { row["nameError"] = ex.GetBaseException().Message; }
                        }
                        else if (row.TryGetValue("m_LanguageCode", out var code) && code is uint hash && languageLookup is not null)
                        {
                            try { row["name"] = languageLookup.Invoke(null, new object[] { hash }) as string; }
                            catch (Exception ex) { row["nameError"] = ex.GetBaseException().Message; }
                        }
                        if (row.TryGetValue("name", out var name) && !string.IsNullOrWhiteSpace(name as string)) table.NamedRows++;
                    }
                }
                catch (Exception ex) { row["error"] = ex.GetBaseException().Message; }
                table.Rows.Add(row);
            }
            if (tableIndex < tables.Count) return;

            var export = new {
                schemaVersion = 1, exportedAtUtc = DateTime.UtcNow, languageCode = language,
                source = "AppMainScript.parameterManager loaded tables",
                coverage = "Static definitions only. Flag semantics, group-index/map correspondence and gameplay unlock effects require validation.",
                tableCount = tables.Count, rowCount = tables.Sum(t => t.Rows.Count),
                errorRows = tables.Sum(t => t.Rows.Count(r => r.Keys.Any(k => k == "error" || k.EndsWith("Error") || k.EndsWith("_error")))),
                tables
            };
            var folder = Path.Combine(Paths.BepInExRootPath, "exports");
            Directory.CreateDirectory(folder);
            var safeLang = string.Concat(language.Where(char.IsLetterOrDigit));
            var path = Path.Combine(folder, $"world-{safeLang}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));
            done = true;
            log.LogInfo($"[WORLD] DONE tables={export.tableCount} rows={export.rowCount} errors={export.errorRows} path={path}");
        }
        catch (Exception ex)
        {
            nextAttempt = DateTime.UtcNow.AddSeconds(2);
            log.LogError($"[WORLD] FAILED attempt={++failures}: {ex.GetBaseException().Message}");
            if (!initialized) tables.Clear();
            if (failures >= 3) { done = true; log.LogError("[WORLD] DISABLED; progression unchanged."); }
        }
    }

    private void AddTable(string source, int? groupIndex, object? csv)
    {
        var table = new Table { Source = source, GroupIndex = groupIndex };
        tables.Add(table);
        if (csv is null) { table.Status = "null-table"; return; }
        var parameters = Read(csv.GetType(), csv, "m_params");
        if (parameters is null) { table.Status = "null-parameters"; return; }
        table.RowCount = Count(parameters, 100000);
        table.Parameters = parameters;
        table.Indexer = parameters.GetType().GetProperties().Single(p => p.Name == "Item" && p.GetIndexParameters().Length == 1);
        table.IndexType = table.Indexer.GetIndexParameters()[0].ParameterType;
        table.Status = "reading";
    }

    private static object? Read(Type type, object? instance, string name) => type.GetProperty(name,
        BindingFlags.Public | BindingFlags.NonPublic | (instance is null ? BindingFlags.Static : BindingFlags.Instance))?.GetValue(instance);
    private static int Count(object collection, int max)
    {
        var length = Read(collection.GetType(), collection, "Length") ?? Read(collection.GetType(), collection, "Count")
            ?? throw new InvalidOperationException("Collection size unavailable");
        var count = Convert.ToInt32(length, CultureInfo.InvariantCulture);
        if (count < 0 || count > max) throw new InvalidOperationException($"Unexpected collection size {count}/{max}");
        return count;
    }
    private static object? At(object collection, int index)
    {
        var property = collection.GetType().GetProperties().Single(p => p.Name == "Item" && p.GetIndexParameters().Length == 1);
        return property.GetValue(collection, new[] { Convert.ChangeType(index, property.GetIndexParameters()[0].ParameterType, CultureInfo.InvariantCulture) });
    }
}
