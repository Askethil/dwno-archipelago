using System.Text;
using System.Text.Json;

namespace Dwno.FlagAnalysis;

internal sealed record SourceRow(string Table, int? Group, int Index, JsonElement Data);
internal sealed record FlagReference(string SourceTable, int? SourceGroupIndex, int SourceRowIndex,
    long? SourceRecordId, string SourceField, string ContextLabel, string? NameEn,
    string NameEvidence, string ReferenceEvidence);
internal sealed record FlagDefinition(uint FlagSetId, string? NameFromSource, string DisplayLabel,
    string LabelStatus, string? GameplayMeaning, string? ConditionLogic, int[] RawConditionSlots,
    string[] ContextLabels, FlagReference[] References);
internal sealed record MissingReference(long ReferenceValue, FlagReference[] References);
internal sealed record ConditionValue(int RawValue, int Occurrences, uint[] FlagSetIds,
    string? DecodedMeaning);
internal sealed record FlagSummary(int FlagSets, int FlagSetsWithSourceNames, int FlagSetsWithContextLabels,
    int UnreferencedFlagSets, int ResolvedReferences, int UnresolvedReferences,
    int UnresolvedReferenceValues, int DistinctRawConditionValues);
internal sealed record FlagAnalysis(int SchemaVersion, string SourceFile, string SourceSha256,
    string Interpretation, FlagSummary Summary, FlagDefinition[] FlagSets,
    MissingReference[] UnresolvedReferences, ConditionValue[] RawConditionValues);

internal static class FlagAnalyzer
{
    // Only documented flag-related fields are joined. Equal numbers in other fields are not evidence.
    private static readonly Dictionary<string, string[]> ReferenceFields = new(StringComparer.Ordinal)
    {
        ["townBlockData"] = new[] { "m_flag_name" },
        ["townGradeUpData"] = Enumerable.Range(0, 8).Select(i => $"m_flag_name_{i}").ToArray(),
        ["townJumpData"] = Enumerable.Range(0, 8).Select(i => $"m_flag_name_{i}").ToArray(),
        ["digiviceMapData"] = new[] { "m_enableFlag" },
        ["digiviceAreaDataArray"] = new[] { "m_enableFlag", "m_root2EnableFlag", "m_scenarioFlag1",
            "m_scenarioFlag2", "m_selectOnFlag" }
    };

    internal static FlagAnalysis Analyze(JsonElement world, string sourceFile, string sha256)
    {
        if (world.GetProperty("schemaVersion").GetInt32() != 1)
            throw new InvalidDataException("Unsupported world catalogue schema.");
        if (world.GetProperty("languageCode").GetString() != "_en")
            throw new InvalidDataException("An English (_en) source is required for English context labels.");
        if (world.TryGetProperty("errorRows", out var errors) && errors.GetInt32() != 0)
            throw new InvalidDataException("Source catalogue contains read errors.");
        var rows = new List<SourceRow>();
        foreach (var table in world.GetProperty("tables").EnumerateArray())
        {
            var source = table.GetProperty("Source").GetString()!;
            if (table.GetProperty("Status").GetString() != "exported")
                throw new InvalidDataException($"Source table {source} was not fully exported.");
            var group = table.TryGetProperty("GroupIndex", out var index) && index.ValueKind == JsonValueKind.Number
                ? index.GetInt32() : (int?)null;
            var tableRows = table.GetProperty("Rows").EnumerateArray().ToArray();
            if (table.GetProperty("RowCount").GetInt32() != tableRows.Length)
                throw new InvalidDataException($"Row count differs for {source}.");
            foreach (var row in tableRows)
            {
                if (row.EnumerateObject().Any(p => p.Name == "error" || p.Name.EndsWith("_error", StringComparison.Ordinal)))
                    throw new InvalidDataException($"Error in source row {source}.");
                rows.Add(new SourceRow(source, group, row.GetProperty("rowIndex").GetInt32(), row));
            }
        }

        var flagRows = rows.Where(r => r.Table == "flagSet").ToArray();
        if (flagRows.Length == 0) throw new InvalidDataException("No flagSet definitions found.");
        var definitions = new Dictionary<uint, SourceRow>();
        foreach (var row in flagRows)
            if (!definitions.TryAdd(row.Data.GetProperty("m_Id").GetUInt32(), row))
                throw new InvalidDataException("Duplicate flagSet ID.");

        var references = definitions.Keys.ToDictionary(id => id, _ => new List<FlagReference>());
        var unresolved = new SortedDictionary<long, List<FlagReference>>();
        foreach (var row in rows)
        {
            if (!ReferenceFields.TryGetValue(row.Table, out var fields)) continue;
            var (label, name, evidence) = Describe(row, rows);
            foreach (var field in fields)
            {
                if (!row.Data.TryGetProperty(field, out var value))
                    throw new InvalidDataException($"Required field {row.Table}.{field} missing.");
                var referenceValue = value.GetInt64();
                var reference = new FlagReference(row.Table, row.Group, row.Index,
                    Number(row.Data, "m_id") ?? Number(row.Data, "m_Id"), field, label, name, evidence,
                    "Exact numeric ID match in an explicitly selected flag-related source field; gameplay effect unverified");
                if (referenceValue >= uint.MinValue && referenceValue <= uint.MaxValue &&
                    references.TryGetValue((uint)referenceValue, out var found)) found.Add(reference);
                else
                {
                    if (!unresolved.TryGetValue(referenceValue, out var missing))
                        unresolved[referenceValue] = missing = new List<FlagReference>();
                    missing.Add(reference with { ReferenceEvidence = "Flag-related source field; referenced definition absent" });
                }
            }
        }

        var flags = definitions.OrderBy(pair => pair.Key).Select(pair => {
            var id = pair.Key;
            var sourceName = Name(pair.Value.Data);
            var consumers = references[id].ToArray();
            var labels = consumers.Where(r => r.NameEn is not null).Select(r => r.ContextLabel)
                .Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
            var display = sourceName ?? (labels.Length > 0
                ? "Referenced by " + string.Join("; ", labels.Take(3)) + (labels.Length > 3 ? $" (+{labels.Length - 3} more)" : "")
                : $"Flag set {id} (unidentified)");
            return new FlagDefinition(id, sourceName, display,
                sourceName is not null ? "name-present-in-source" : labels.Length > 0 ? "context-derived-label" : "unidentified",
                null, null, Enumerable.Range(0, 8).Select(i => pair.Value.Data.GetProperty($"m_Flag{i:D2}").GetInt32()).ToArray(),
                labels, consumers);
        }).ToArray();

        var conditions = flags.SelectMany(f => f.RawConditionSlots.Select(value => (Value: value, Id: f.FlagSetId)))
            .GroupBy(entry => entry.Value).OrderBy(g => g.Key)
            .Select(g => new ConditionValue(g.Key, g.Count(), g.Select(p => p.Id).Distinct().OrderBy(id => id).ToArray(), null)).ToArray();
        var missingReferences = unresolved.Select(pair => new MissingReference(pair.Key, pair.Value.ToArray())).ToArray();
        var summary = new FlagSummary(flags.Length, flags.Count(f => f.NameFromSource is not null),
            flags.Count(f => f.ContextLabels.Length > 0), flags.Count(f => f.References.Length == 0),
            flags.Sum(f => f.References.Length), missingReferences.Sum(r => r.References.Length),
            missingReferences.Length, conditions.Length);
        return new FlagAnalysis(1, sourceFile, sha256,
            "Static cross-references only. Context-derived labels are descriptive, not official flag names or confirmed unlocks. " +
            "Raw slots, repeated values and unresolved references are preserved. No bit/sign/sentinel or AND/OR decoding is assumed. " +
            "No runtime state, event-to-flag causality, recruitment identity or region-unlock effect is established.",
            summary, flags, missingReferences, conditions);
    }

    private static (string Label, string? Name, string Evidence) Describe(SourceRow row, List<SourceRow> all)
    {
        var location = row.Table + (row.Group is not null ? $"[sourceGroup={row.Group}]" : "");
        var direct = Name(row.Data);
        if (direct is not null) return ($"{location} / {direct}", direct, "name present in exported source row");
        if (row.Table == "digiviceMapData")
        {
            var map = Number(row.Data, "m_mapNo");
            var matches = all.Where(r => r.Table == "mapNameData" && map is not null &&
                Number(r.Data, "m_MapNo") == map).Select(r => Name(r.Data)).Where(n => n is not null)
                .Distinct(StringComparer.Ordinal).ToArray();
            if (matches.Length == 1)
                return ($"{location} / {matches[0]} (map {map})", matches[0], "mapNameData joined by exact map number");
        }
        if (row.Table == "digiviceAreaDataArray")
        {
            var area = Number(row.Data, "m_areaNo");
            var matches = all.Where(r => r.Table == "areaNameDataArray" && row.Group is not null &&
                r.Group == row.Group && area is not null && Number(r.Data, "m_AreaNo") == area)
                .Select(r => Name(r.Data)).Where(n => n is not null).Distinct(StringComparer.Ordinal).ToArray();
            if (matches.Length == 1)
                return ($"{location} / {matches[0]} (area {area})", matches[0],
                    "areaNameDataArray joined by same source-group index and area number; map ownership unverified");
        }
        return ($"{location} / row {row.Index}", null, "No unambiguous exported name; source location only");
    }

    private static string? Name(JsonElement row)
    {
        if (row.TryGetProperty("nameStatus", out var status) && status.GetString() == "missing-localization-resource") return null;
        if (!row.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String) return null;
        var text = name.GetString();
        if (string.IsNullOrWhiteSpace(text) || text.Contains("ランゲージ", StringComparison.Ordinal) ||
            text.Contains("見つかり", StringComparison.Ordinal) || text.Contains("language not found", StringComparison.OrdinalIgnoreCase)) return null;
        return text;
    }
    private static long? Number(JsonElement row, string name) => row.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.Number ? value.GetInt64() : null;

    internal static string Report(FlagAnalysis result)
    {
        var text = new StringBuilder();
        text.AppendLine("# Flag conditions — static reference analysis").AppendLine();
        text.AppendLine("Generated by `tools/AnalyzeFlags`. " + result.Interpretation).AppendLine();
        text.AppendLine($"Source: `{result.SourceFile}`; SHA-256 `{result.SourceSha256}`.").AppendLine();
        text.AppendLine("## Coverage").AppendLine();
        text.AppendLine("| Measure | Count |").AppendLine("| --- | ---: |");
        foreach (var (name, count) in new[] {
            ("Flag-set definitions", result.Summary.FlagSets),
            ("Names present on the flag-set source rows", result.Summary.FlagSetsWithSourceNames),
            ("Flag sets with descriptive context labels", result.Summary.FlagSetsWithContextLabels),
            ("Flag sets not referenced by the analyzed fields", result.Summary.UnreferencedFlagSets),
            ("Resolved reference slots", result.Summary.ResolvedReferences),
            ("Unresolved reference slots", result.Summary.UnresolvedReferences),
            ("Distinct unresolved reference values", result.Summary.UnresolvedReferenceValues),
            ("Distinct raw condition values", result.Summary.DistinctRawConditionValues) })
            text.AppendLine($"| {name} | {count} |");
        text.AppendLine().AppendLine("## How to read this catalogue").AppendLine();
        text.AppendLine("- `flagSetId` identifies a definition. Its eight `rawConditionSlots` are a separate numeric domain; no bit-index encoding is assumed.");
        text.AppendLine("- A label such as `townGradeUpData / Item Shop` means that record references the set. It does not mean setting one value recruits an NPC, grants a building, or satisfies all other prerequisites.");
        text.AppendLine("- `gameplayMeaning` and `conditionLogic` stay null until validated. No all-4096 set is called always-on, and no missing reference is silently treated as empty.");
        text.AppendLine("- Area labels use the same source-group index and area number. Group indices are not mapped to region numbers; navigation targets are not treated as owning regions.");
        text.AppendLine("- `unidentified` means no descriptive name was found in the analyzed references, not that a definition is unused by the game.");
        text.AppendLine("- Original reference slots and their table/group/row/field locations are in the JSON. Repeated raw values have a reverse index there.");
        text.AppendLine().AppendLine("## Context-associated definitions").AppendLine();
        text.AppendLine("| Flag-set ID | Eight raw condition slots | Referencing contexts |")
            .AppendLine("| ---: | --- | --- |");
        foreach (var flag in result.FlagSets.Where(f => f.ContextLabels.Length > 0))
        {
            var contexts = string.Join("; ", flag.ContextLabels.Take(6));
            if (flag.ContextLabels.Length > 6) contexts += $" (+{flag.ContextLabels.Length - 6} more; see JSON)";
            text.AppendLine($"| {flag.FlagSetId} | {string.Join(", ", flag.RawConditionSlots)} | {Escape(contexts)} |");
        }
        text.AppendLine().AppendLine("## Unresolved reference values").AppendLine();
        text.AppendLine("| Raw reference value | Reference slots |")
            .AppendLine("| ---: | ---: |");
        foreach (var unresolved in result.UnresolvedReferences)
            text.AppendLine($"| {unresolved.ReferenceValue} | {unresolved.References.Length} |");
        text.AppendLine().AppendLine("## Largest raw-value occurrence counts").AppendLine();
        text.AppendLine("Frequency is an observation, not a decoded sentinel or flag meaning.").AppendLine();
        text.AppendLine("| Raw value | Slot occurrences | Definitions containing it |")
            .AppendLine("| ---: | ---: | ---: |");
        foreach (var value in result.RawConditionValues.OrderByDescending(v => v.Occurrences).ThenBy(v => v.RawValue).Take(10))
            text.AppendLine($"| {value.RawValue} | {value.Occurrences} | {value.FlagSetIds.Length} |");
        text.AppendLine().AppendLine("Data: [flag-conditions.en.json](../data/flag-conditions.en.json). " +
            "Run instructions and limits: [analyze-flags.md](analyze-flags.md).");
        return text.ToString().Replace("\r\n", "\n");
    }

    private static string Escape(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
}
