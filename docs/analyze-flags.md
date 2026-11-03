# Analyze flag conditions from the existing catalogue

The analyzer reads the already exported English world catalogue and builds a searchable flag-set reference index. It runs offline with the repository's .NET 6 SDK. It does not need the game, BepInEx, generated interop, a running save or a new deployment.

## Run

From the repository root in PowerShell:

```powershell
./scripts/analyze-flags.ps1
```

Equivalent direct command:

```powershell
dotnet run --project tools/AnalyzeFlags -c Release
```

Outputs:

- [data/flag-conditions.en.json](../data/flag-conditions.en.json): every flag-set definition, descriptive labels, exact references, unresolved references and reverse index of raw condition values.
- [flag-conditions.en.md](flag-conditions.en.md): coverage and a readable table of definitions associated with names.

Check that committed outputs reproduce from the current source:

```powershell
./scripts/analyze-flags.ps1 -Check
```

Alternative source and destinations:

```powershell
./scripts/analyze-flags.ps1 -WorldCataloguePath .local/world-review.json -JsonOutputPath .local/flags-review.json -ReportPath .local/flags-review.md
```

The source must use the current world-catalogue schema, English language code `_en`, fully exported tables, matching row counts and no reported read errors. Input and output paths must be distinct. The tool rejects duplicate flag-set IDs and missing required fields. Output contains a SHA-256 of the exact input bytes and no generation timestamp, so repeated runs are deterministic. Changing the source's formatting also changes that hash.

## What gets associated

Exact ID joins are restricted to these source fields:

| Table | Reference fields |
| --- | --- |
| `townBlockData` | `m_flag_name` |
| `townGradeUpData` | `m_flag_name_0` through `m_flag_name_7` |
| `townJumpData` | `m_flag_name_0` through `m_flag_name_7` |
| `digiviceMapData` | `m_enableFlag` |
| `digiviceAreaDataArray` | `m_enableFlag`, `m_root2EnableFlag`, `m_scenarioFlag1`, `m_scenarioFlag2`, `m_selectOnFlag` |

Names come from exported row names. Digivice map names are joined by map number; area names are joined by the same source-group index and area number only when the resulting name is unambiguous. Missing-localization messages are not used as names. Source-group indices are not converted to region numbers, and destination map numbers are not used to name a region containing an area.

Unrelated numbers, such as a development-point record ID or material quantity, are not interpreted as flag references simply because they match an ID. References to missing definitions are retained separately. Every resolved reference includes its source table, group, row, record ID where present, and exact field name.

## Result with the current source

The current catalogue contains **1,738 flag sets**, **35 with context-derived labels**, **992 resolved reference slots** and **323 unresolved slots**. All unresolved slots refer to value `1402660926`, whose meaning remains unverified. The other 1,703 definitions have no consumer in the selected exported reference fields; this does not mean they are unused by story scripts or other game systems.

Examples from the current data:

| Flag-set ID | Raw first slot | Observed references |
| ---: | ---: | --- |
| `4212119345` | `1000` | Warehouse; Central-Warehouse |
| `2272649181` | `1006` | Item Shop; Business-Item Shop |
| `854107969` | `100` | Lab; Research-Lab 1F |

The full eight slots remain in the JSON. These examples name a usage context, not an official flag or the exact requirement for recruiting a character or opening a facility. Multiple records can reference the same definition; a definition can contain repeated raw values. The analysis deliberately leaves `gameplayMeaning`, `conditionLogic` and each raw value's `decodedMeaning` null.

No official flag-set names were present in the current exported `ParameterFlagSet` rows. Local interop inspection also found ID and slot accessors but no `GetName` method on that type. A label such as `Referenced by townGradeUpData / Warehouse` is generated from the named consumer; it is not a recovered engine identifier. If an input row later has an actual exported name, `nameFromSource` and `labelStatus=name-present-in-source` keep it distinct from generated context labels.

The value `4096` occurs in all 1,738 sets and occupies 12,167 slots. This is a frequency observation. The script does not treat it as an empty slot, true value, bit maximum or sentinel. Signed and zero raw values are preserved if a later export contains them. Neither AND/OR semantics nor numeric bit encoding is inferred.

## Validation and next evidence

```powershell
dotnet run --project tests/Dwno.FlagAnalysis.Tests -c Release
```

The tests exercise exact joins, shared references, preserved raw slots/signs/repeated values, unrelated numeric collisions, unresolved references, area-group disambiguation, missing localization, invalid/incomplete inputs and deterministic reporting. This does not test native game flag behavior.

To turn descriptive labels into validated requirements, compare native scenario-flag state before and after one known action, correlate it with the active scene/block and preserve control-flow and save/reload evidence. Existing scene IDs alone do not establish which flag changes they cause. References from tables or scenario scripts not present in the current export will require additional extraction or observation; this analyzer cannot recover those missing names from numeric IDs alone.
