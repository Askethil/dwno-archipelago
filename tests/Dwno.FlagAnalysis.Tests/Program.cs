using System.Text.Json;
using Dwno.FlagAnalysis;

var checks = 0;
void Check(string name, bool value)
{
    checks++;
    if (!value) throw new InvalidOperationException("Failed: " + name);
}
Dictionary<string, object?> Row(params (string Key, object? Value)[] values) => values.ToDictionary(p => p.Key, p => p.Value);
Dictionary<string, object?> Flag(uint id, params int[] slots)
{
    var row = Row(("rowIndex", (int)id), ("m_Id", id));
    for (var i = 0; i < 8; i++) row[$"m_Flag{i:D2}"] = i < slots.Length ? slots[i] : 4096;
    return row;
}
Dictionary<string, object?> Table(string source, int? group, params Dictionary<string, object?>[] rows) =>
    Row(("Source", source), ("GroupIndex", group), ("Status", "exported"), ("RowCount", rows.Length), ("Rows", rows));
Dictionary<string, object?> World(params Dictionary<string, object?>[] tables) =>
    Row(("schemaVersion", 1), ("languageCode", "_en"), ("errorRows", 0), ("tables", tables));
FlagAnalysis Analyze(Dictionary<string, object?> world)
{
    using var doc = JsonDocument.Parse(JsonSerializer.Serialize(world));
    return FlagAnalyzer.Analyze(doc.RootElement, "fixture.json", "fixture-hash");
}
void Reject(string name, Dictionary<string, object?> source)
{
    var rejected = false;
    try { Analyze(source); }
    catch (Exception ex) when (ex is InvalidDataException or KeyNotFoundException) { rejected = true; }
    Check(name, rejected);
}

var main = World(
    Table("flagSet", null, Flag(42, -7, 0), Flag(7, 123)),
    Table("townBlockData", null,
        Row(("rowIndex", 0), ("m_id", 1234), ("m_flag_name", 42), ("name", "Warehouse")),
        Row(("rowIndex", 1), ("m_id", 1235), ("m_flag_name", 42), ("name", "Item Shop")),
        Row(("rowIndex", 2), ("m_id", 1236), ("m_flag_name", 1402660926), ("name", "Unknown condition"))),
    Table("townAdditionDp", null, Row(("rowIndex", 0), ("m_Id", 7), ("m_Point", 42), ("name", "Not a flag reference"))));
var result = Analyze(main);
var flag42 = result.FlagSets.Single(f => f.FlagSetId == 42);
Check("all definitions retained and sorted", result.FlagSets.Select(f => f.FlagSetId).SequenceEqual(new uint[] { 7, 42 }));
Check("many-to-many source names retained", flag42.ContextLabels.Length == 2 && flag42.ContextLabels.Any(n => n.Contains("Warehouse")));
Check("source field and record ID preserved", flag42.References[0].SourceField == "m_flag_name" && flag42.References[0].SourceRecordId == 1234);
Check("all eight raw slots and signs preserved", flag42.RawConditionSlots.Length == 8 && flag42.RawConditionSlots[0] == -7 && flag42.RawConditionSlots[1] == 0);
Check("labels are not asserted engine names", flag42.NameFromSource is null && flag42.LabelStatus == "context-derived-label");
Check("logic and gameplay semantics remain unknown", flag42.ConditionLogic is null && flag42.GameplayMeaning is null);
Check("unrelated numeric collisions ignored", result.FlagSets.Single(f => f.FlagSetId == 7).References.Length == 0);
Check("unreferenced definition is not given unrelated name", result.FlagSets.Single(f => f.FlagSetId == 7).LabelStatus == "unidentified");
Check("unresolved placeholder preserved", result.UnresolvedReferences.Single().ReferenceValue == 1402660926);
Check("summary counts actual slots", result.Summary.ResolvedReferences == 2 && result.Summary.UnresolvedReferences == 1);
Check("repeated 4096 slots not decoded or removed", result.RawConditionValues.Single(v => v.RawValue == 4096).Occurrences == 13 &&
    result.RawConditionValues.Single(v => v.RawValue == 4096).DecodedMeaning is null);
Check("raw-value reverse index is distinct by definition", result.RawConditionValues.Single(v => v.RawValue == 4096).FlagSetIds.SequenceEqual(new uint[] { 7, 42 }));
Check("negative value not normalized to a positive bit", result.RawConditionValues.Any(v => v.RawValue == -7));

Dictionary<string, object?> Area(int row, int area) => Row(("rowIndex", row), ("m_areaNo", area), ("m_mapNo", 99),
    ("m_enableFlag", 42), ("m_root2EnableFlag", 42), ("m_scenarioFlag1", 42), ("m_scenarioFlag2", 42), ("m_selectOnFlag", 42));
var areaWorld = World(Table("flagSet", null, Flag(42)),
    Table("digiviceAreaDataArray", 1, Area(0, 5)),
    Table("areaNameDataArray", 1, Row(("rowIndex", 0), ("m_AreaNo", 5), ("name", "Correct area"))),
    Table("areaNameDataArray", 2, Row(("rowIndex", 0), ("m_AreaNo", 5), ("name", "Wrong source group"))),
    Table("mapNameData", null, Row(("rowIndex", 0), ("m_MapNo", 99), ("name", "Navigation destination"))));
var areas = Analyze(areaWorld).FlagSets.Single();
Check("area names join by group and area", areas.ContextLabels.Single().Contains("Correct area"));
Check("area owner not inferred from navigation map", !areas.ContextLabels.Single().Contains("Navigation destination"));
Check("source group remains explicit", areas.References.All(r => r.SourceGroupIndex == 1));
Check("same definition reused in different fields", areas.References.Length == 5);
var ambiguous = World(Table("flagSet", null, Flag(42)), Table("digiviceAreaDataArray", 1, Area(0, 5)),
    Table("areaNameDataArray", 1,
        Row(("rowIndex", 0), ("m_AreaNo", 5), ("name", "First possible name")),
        Row(("rowIndex", 1), ("m_AreaNo", 5), ("name", "Second possible name"))));
Check("ambiguous names are not guessed", Analyze(ambiguous).FlagSets.Single().ContextLabels.Length == 0);

var badName = World(Table("flagSet", null, Flag(42)), Table("townBlockData", null,
    Row(("rowIndex", 0), ("m_flag_name", 42), ("name", "language not found"), ("nameStatus", "missing-localization-resource"))));
Check("missing localization is not a flag label", Analyze(badName).Summary.FlagSetsWithContextLabels == 0);
var sourceNamed = Flag(42); sourceNamed["name"] = "Name actually exported";
Check("source name remains distinguishable from derived labels", Analyze(World(Table("flagSet", null, sourceNamed))).FlagSets.Single().LabelStatus == "name-present-in-source");

Reject("duplicate definitions rejected", World(Table("flagSet", null, Flag(42), Flag(42))));
var wrongSchema = World(Table("flagSet", null, Flag(42))); wrongSchema["schemaVersion"] = 2;
Reject("unknown schema rejected", wrongSchema);
var wrongLanguage = World(Table("flagSet", null, Flag(42))); wrongLanguage["languageCode"] = "_fr";
Reject("non-English catalogue not mislabeled English", wrongLanguage);
var badExport = World(Table("flagSet", null, Flag(42))); badExport["errorRows"] = 1;
Reject("incomplete source export rejected", badExport);
var incompleteTable = Table("flagSet", null, Flag(42)); incompleteTable["Status"] = "reading";
Reject("unfinished table rejected", World(incompleteTable));
var wrongCount = Table("flagSet", null, Flag(42)); wrongCount["RowCount"] = 2;
Reject("truncated table rejected", World(wrongCount));
var incompleteFlag = Flag(42); incompleteFlag.Remove("m_Flag07");
Reject("missing condition slot rejected", World(Table("flagSet", null, incompleteFlag)));
Reject("required reference field missing is visible", World(Table("flagSet", null, Flag(42)),
    Table("townBlockData", null, Row(("rowIndex", 0), ("name", "No flag field")))));
Check("report deterministic without wall clock", FlagAnalyzer.Report(result) == FlagAnalyzer.Report(Analyze(main)));
Console.WriteLine($"Passed {checks} flag-analysis checks. Static joins do not validate native flag semantics.");
