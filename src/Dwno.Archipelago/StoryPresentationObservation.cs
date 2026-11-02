using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace Dwno.Archipelago;

internal static class StoryPresentationObservation
{
    private static readonly Harmony harmony = new(Plugin.Id + ".presentation-observation");
    private static ManualLogSource? log;

    internal static void Install(ManualLogSource logger)
    {
        log = logger;
        var assembly = Assembly.Load("Assembly-CSharp");
        var installed = 0;
        var failed = 0;
        // Command entry points only. Never hook shared coroutine MoveNext stubs or unit AI.
        foreach (var (typeName, methodName) in new[] {
            ("TalkMain", "Initialize"),
            ("TalkMain", "MoveModel"),
            ("TalkMain", "AnimationModel"),
            ("TalkMain", "MoveGameModel"),
            ("TalkMain", "RotationGameModel"),
            ("TalkMain", "MoveGameCamera"),
            ("TalkMain", "CameraZoom"),
            ("TalkMain", "FadeModel"),
            ("TalkMain", "Wait"),
            ("SceneMain", "Initialize"),
            ("SceneMain", "StartScene"),
            ("SceneMain", "EndScene"),
            ("SceneMain", "AnimationModel"),
            ("SceneMain", "DispText"),
            ("SceneMain", "PlayVoiceText") })
        {
            try
            {
                var methods = assembly.GetType(typeName, true)!.GetMethods(BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.DeclaredOnly).Where(m => m.Name == methodName).ToArray();
                if (methods.Length != 1)
                    throw new InvalidOperationException($"Expected one method, got {methods.Length}");
                harmony.Patch(methods[0], prefix: new HarmonyMethod(typeof(StoryPresentationObservation), nameof(Trace)));
                installed++;
                logger.LogInfo($"[PRESENTATION] PATCHED {typeName}.{methodName}");
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning($"[PRESENTATION] PATCH_FAILED {typeName}.{methodName}: {ex.GetBaseException().Message}");
            }
        }
        logger.LogInfo($"[PRESENTATION] READY installed={installed} failed={failed}; command calls only, not coroutine completion; no acceleration.");
    }

    private static void Trace(MethodBase __originalMethod, object[] __args)
    {
        try
        {
            var parameters = __originalMethod.GetParameters();
            var values = __args.Select((value, i) => {
                var key = i < parameters.Length ? parameters[i].Name : i.ToString(CultureInfo.InvariantCulture);
                string display;
                // Dialogue entry points identify the player in use; their text arguments stay omitted.
                var dialogue = __originalMethod.DeclaringType?.Name == "SceneMain" &&
                    __originalMethod.Name is "DispText" or "PlayVoiceText";
                if (value is string && dialogue) display = "<text omitted>";
                else if (value is string argument)
                    display = argument.Substring(0, Math.Min(argument.Length, 160))
                        .Replace('\r', ' ').Replace('\n', ' ');
                else if (value is null) display = "null";
                else if (value.GetType().IsPrimitive || value.GetType().IsEnum)
                    display = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
                else display = "<" + value.GetType().Name + ">";
                return $"{key}={display}";
            });
            log?.LogInfo($"[PRESENTATION] {DateTime.UtcNow:O} CALL {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name} {string.Join(" ", values)}");
        }
        catch (Exception ex) { log?.LogWarning($"[PRESENTATION] TRACE_FAILED {ex.Message}"); }
    }
}
