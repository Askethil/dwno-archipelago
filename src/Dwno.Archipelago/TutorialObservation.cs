using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace Dwno.Archipelago;

// Observation only: no skip result, callback, seen flag or scenario command is changed.
internal static class TutorialObservation
{
    private static readonly Harmony harmony = new(Plugin.Id + ".tutorial-observation");
    private static ManualLogSource? log;

    internal static void Install(ManualLogSource logger)
    {
        log = logger;
        var assembly = Assembly.Load("Assembly-CSharp");
        var installed = 0;
        var failed = 0;
        foreach (var (typeName, methodName) in new[] {
            ("TutorialMain", "Initialize"),
            ("TutorialMain", "OnDisable"),
            ("CScenarioScript", "CmdExeStartTutorialUI"),
            ("CScenarioScript", "TutorialUiEndCB"),
            ("PlayerData", "SetTutorialAlreadyFlag") })
        {
            try
            {
                var methods = assembly.GetType(typeName, true)!.GetMethods(BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.DeclaredOnly).Where(m => m.Name == methodName).ToArray();
                if (methods.Length != 1)
                    throw new InvalidOperationException($"Expected one method, got {methods.Length}");
                harmony.Patch(methods[0], prefix: new HarmonyMethod(typeof(TutorialObservation), nameof(Trace)));
                installed++;
                logger.LogInfo($"[TUTORIAL] PATCHED {typeName}.{methodName}");
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning($"[TUTORIAL] PATCH_FAILED {typeName}.{methodName}: {ex.GetBaseException().Message}");
            }
        }
        logger.LogInfo($"[TUTORIAL] READY installed={installed} failed={failed}; observation only; no tutorial suppression.");
    }

    private static void Trace(MethodBase __originalMethod, object[] __args)
    {
        try
        {
            var parameters = __originalMethod.GetParameters();
            var values = __args.Select((value, i) => {
                var key = i < parameters.Length ? parameters[i].Name : i.ToString(CultureInfo.InvariantCulture);
                string display;
                // Only these known identifiers are strings; do not dump text or native objects.
                if (value is string identifier && __originalMethod.DeclaringType?.Name == "TutorialMain" &&
                    key is "command_csv_name" or "block_name")
                    display = identifier.Substring(0, Math.Min(identifier.Length, 160))
                        .Replace('\r', ' ').Replace('\n', ' ');
                else if (value is null) display = "null";
                else if (value.GetType().IsPrimitive || value.GetType().IsEnum)
                    display = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
                else display = "<" + value.GetType().Name + ">";
                return $"{key}={display}";
            });
            log?.LogInfo($"[TUTORIAL] {DateTime.UtcNow:O} CALL {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name} {string.Join(" ", values)}");
        }
        catch (Exception ex) { log?.LogWarning($"[TUTORIAL] TRACE_FAILED {ex.Message}"); }
    }
}
