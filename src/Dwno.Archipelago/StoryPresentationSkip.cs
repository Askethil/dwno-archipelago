using System;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace Dwno.Archipelago;

internal static class StoryPresentationSkip
{
    private static readonly Harmony harmony = new(Plugin.Id + ".timed-presentation");
    private static ManualLogSource? log;

    internal static void Install(Assembly assembly, ManualLogSource logger, bool requested)
    {
        log = logger;
        if (!requested) { logger.LogInfo("[TIMING] DISABLED by Story.AcceleratePresentation"); return; }
        try
        {
            var talk = assembly.GetType("TalkMain", true)!;
            foreach (var (methodName, parameterName, parameterIndex, patch) in new[] {
                ("Wait", "dummy0", 0, nameof(Duration0)),
                ("MoveGameCamera", "time", 3, nameof(Duration3)),
                ("RotationGameModel", "time", 2, nameof(Duration2)) })
            {
                var method = talk.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly).Single(m => m.Name == methodName);
                var parameters = method.GetParameters();
                if (parameters.Length != 6 || parameters.Any(p => p.ParameterType != typeof(string)) ||
                    parameters[parameterIndex].Name != parameterName)
                    throw new InvalidOperationException($"Unexpected {methodName} duration signature");
                if (!InteropTraceGuard.CanTrace(method, out var reason))
                    throw new InvalidOperationException($"Unsafe {methodName} entry: {reason}");
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(StoryPresentationSkip), patch));
                logger.LogInfo($"[TIMING] PATCHED TalkMain.{methodName}");
            }
            logger.LogInfo("[TIMING] READY installed=3 factor=0.1 floorSeconds=0.05; positive numeric durations <=120 only; native commands still execute; runtime behavior unverified.");
        }
        catch (Exception ex)
        {
            harmony.UnpatchSelf();
            logger.LogWarning($"[TIMING] UNAVAILABLE {ex.GetBaseException().Message}; original durations retained.");
        }
    }

    private static void Duration0(ref string? __0, MethodBase __originalMethod) => Shorten(ref __0, __originalMethod);
    private static void Duration2(ref string? __2, MethodBase __originalMethod) => Shorten(ref __2, __originalMethod);
    private static void Duration3(ref string? __3, MethodBase __originalMethod) => Shorten(ref __3, __originalMethod);
    private static void Shorten(ref string? duration, MethodBase method)
    {
        if (!StorySkip.CanAcceleratePresentation || !StoryTimingPolicy.TryShorten(duration, out var shortened)) return;
        var original = duration;
        duration = shortened;
        log?.LogInfo($"[TIMING] {DateTime.UtcNow:O} SHORTEN {method.DeclaringType?.Name}.{method.Name} from={original} to={shortened}");
    }
}
