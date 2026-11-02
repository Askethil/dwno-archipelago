using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using BepInEx.Logging;
using HarmonyLib;

namespace Dwno.Archipelago;

internal static class Observation
{
    private static ManualLogSource? log;
    private static long sequence;
    private static readonly Harmony harmony = new(Plugin.Id + ".observation");

    internal static void Install(ManualLogSource logger)
    {
        log = logger;
        try
        {
            var assembly = Assembly.Load("Assembly-CSharp");
            var targets = new[] {
                ("ItemStorageData", "AddItemPlayer"),
                ("ItemStorageData", "_AddMaterialOrKeyItem"),
                ("uItemPickPanel", "PickItem"),
                ("uItemPickPanel", "PickDefault"),
                ("uItemPickPanel", "PickMaterial")
            };
            var installed = 0;
            var failed = 0;
            foreach (var (typeName, methodName) in targets)
            {
                var type = assembly.GetType(typeName);
                var methods = type?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Where(m => m.Name == methodName).ToArray() ?? Array.Empty<MethodInfo>();
                if (methods.Length == 0) { failed++; log.LogWarning($"[OBS] MISSING {typeName}.{methodName}"); }
                foreach (var method in methods)
                {
                    try
                    {
                        harmony.Patch(method,
                            prefix: new HarmonyMethod(typeof(Observation), nameof(Before)),
                            postfix: new HarmonyMethod(typeof(Observation), nameof(After)));
                        installed++;
                        log.LogInfo($"[OBS] PATCHED {typeName}.{methodName}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})");
                    }
                    catch (Exception ex) { failed++; log.LogError($"[OBS] PATCH_FAILED {typeName}.{methodName}: {ex}"); }
                }
            }
            log.LogInfo($"[OBS] READY revision=5 installed={installed} failed={failed}; timestamps=UTC; no gameplay changes.");
        }
        catch (Exception ex) { log.LogError($"[OBS] INIT_FAILED {ex}"); }
    }

    private static void Before(MethodBase __originalMethod, object[] __args, out long __state)
    {
        __state = Interlocked.Increment(ref sequence);
        try
        {
            var parameters = __originalMethod.GetParameters();
            var values = __args.Select((value, index) =>
                $"{(index < parameters.Length ? parameters[index].Name : index.ToString())}={Format(value)}");
            log?.LogInfo($"[OBS] {DateTime.UtcNow:O} ENTER #{__state} {Describe(__originalMethod)} {string.Join(" ", values)}");
        }
        catch (Exception ex) { log?.LogWarning($"[OBS] Trace failed: {ex.Message}"); }
    }

    private static void After(MethodBase __originalMethod, long __state)
    {
        // Returning from a method does not prove that inventory was credited.
        try { log?.LogInfo($"[OBS] {DateTime.UtcNow:O} RETURN #{__state} {Describe(__originalMethod)}"); }
        catch { /* An observation must not interrupt the game. */ }
    }

    private static string Describe(MethodBase method) =>
        $"{method.DeclaringType?.Name}.{method.Name}({string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name))})";

    private static string Format(object? value)
    {
        if (value is null) return "null";
        // Do not invoke ToString on game objects or enumerate their fields.
        if (value is int or uint or long or ulong or short or ushort or byte or sbyte or float or double or bool)
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
        if (value.GetType().IsEnum) return value.ToString() ?? "enum";
        return $"<{value.GetType().Name}>";
    }
}
