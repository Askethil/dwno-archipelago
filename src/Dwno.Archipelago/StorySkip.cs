using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace Dwno.Archipelago;

internal static class StorySkip
{
    private static readonly Harmony harmony = new(Plugin.Id + ".story");
    private static ManualLogSource? log;
    private static bool enabled;
    private static bool choiceBlocked;
    private static bool movieActive;
    private static uint movieButton;
    private static bool inputFailed;
    private static DateTime nextSkipLog;
    [ThreadStatic] private static int movieUpdateDepth;
    internal static bool Held;

    internal static void Install(ManualLogSource logger, bool initiallyEnabled)
    {
        log = logger;
        enabled = initiallyEnabled;
        var assembly = Assembly.Load("Assembly-CSharp");
        var ok = 0;
        var failed = 0;
        bool Patch(string type, string method, string? prefix = null, string? postfix = null, string? finalizer = null, Type? returns = null)
        {
            try
            {
                var targets = assembly.GetType(type, true)!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(m => m.Name == method).ToArray();
                if (targets.Length != 1) throw new InvalidOperationException($"Expected one method, got {targets.Length}");
                if (returns is not null && targets[0].ReturnType != returns) throw new InvalidOperationException("Return type differs");
                HarmonyMethod? post = postfix is null ? null : new HarmonyMethod(typeof(StorySkip), postfix);
                if (postfix == nameof(MovieButtonBits))
                {
                    if (!targets[0].ReturnType.IsEnum) throw new InvalidOperationException("Expected native button enum");
                    var patch = typeof(StorySkip).GetMethod(nameof(MovieButtonBits), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(targets[0].ReturnType);
                    post = new HarmonyMethod(patch);
                }
                harmony.Patch(targets[0], prefix is null ? null : new HarmonyMethod(typeof(StorySkip), prefix),
                    post,
                    finalizer: finalizer is null ? null : new HarmonyMethod(typeof(StorySkip), finalizer));
                ok++;
                logger.LogInfo($"[STORY] PATCHED {type}.{method}");
                return true;
            }
            catch (Exception ex) { failed++; logger.LogError($"[STORY] PATCH_FAILED {type}.{method}: {ex.GetBaseException().Message}"); return false; }
        }
        Patch("TalkMain", "IsSkip", postfix: nameof(DialogueSkip), returns: typeof(bool));
        foreach (var method in new[] { "DispTextMain", "PlayVoiceText", "DispText", "DispChoices", "EventScene", "PlayMovie" })
            Patch("TalkMain", method, prefix: nameof(Trace));
        Patch("CScenarioScript", "_RequestStartTalkEvent", prefix: nameof(Trace));
        Patch("CScenarioScript", "TalkEventEndCB", prefix: nameof(Trace));
        Patch("Movie", "Play", prefix: nameof(MovieStart));
        Patch("Movie", "_PlayEnd", prefix: nameof(MovieEnd));
        Patch("Movie", "Stop", prefix: nameof(MovieStop));
        // Pad changes are scoped to native Movie.Update and the exact configured movie button.
        Patch("Movie", "Update", prefix: nameof(MovieUpdateStart), finalizer: nameof(MovieUpdateEnd));
        foreach (var method in new[] { "IsInput", "IsTrigger", "CheckTrigger" })
            Patch("PadManager", method, postfix: nameof(MovieButtonQuery), returns: typeof(bool));
        foreach (var method in new[] { "GetInput", "GetTrigger" })
            Patch("PadManager", method, postfix: nameof(MovieButtonBits));
        if (failed != 0)
        {
            harmony.UnpatchSelf();
            enabled = false;
            inputFailed = true;
            log.LogError("[STORY] DISABLED: incomplete patch set removed; prototype unavailable this session.");
        }
        log.LogInfo($"[STORY] READY installed={ok} failed={failed} enabled={enabled}; F9 toggle, hold F8; progression callbacks are not replaced.");
    }

    internal static void Toggle()
    {
        if (inputFailed) return;
        enabled = !enabled;
        if (enabled) choiceBlocked = false;
        log?.LogInfo($"[STORY] {DateTime.UtcNow:O} ENABLED={enabled}");
    }
    internal static void InputFailed(Exception ex)
    {
        if (!inputFailed) log?.LogError($"[STORY] INPUT_FAILED {ex.Message}; skipping disabled");
        inputFailed = true; enabled = false; Held = false;
    }
    private static void Trace(MethodBase __originalMethod, object[] __args)
    {
        try
        {
            var name = __originalMethod.Name;
            if (name == "DispChoices")
            {
                choiceBlocked = true;
                enabled = false;
                Held = false;
                log?.LogInfo("[STORY] PAUSED_FOR_CHOICE: choose manually, then F9 to resume prototype.");
            }
            var parameters = __originalMethod.GetParameters();
            var values = __args.Select((v, i) => {
                var key = i < parameters.Length ? parameters[i].Name : i.ToString();
                if (v is string s) return name is "EventScene" or "PlayMovie" ? $"{key}={s.Substring(0, Math.Min(s.Length, 160))}" : $"{key}=<text omitted>";
                return $"{key}={(v is null ? "null" : v.GetType().IsPrimitive || v.GetType().IsEnum ? Convert.ToString(v, CultureInfo.InvariantCulture) : "<" + v.GetType().Name + ">")}";
            });
            log?.LogInfo($"[STORY] {DateTime.UtcNow:O} EVENT {__originalMethod.DeclaringType?.Name}.{name} {string.Join(" ", values)} choiceBlocked={choiceBlocked}");
        }
        catch (Exception ex) { log?.LogWarning($"[STORY] TRACE_FAILED {ex.Message}"); }
    }
    private static void DialogueSkip(ref bool __result)
    {
        if (!enabled || !Held || choiceBlocked || inputFailed) return;
        __result = true;
        ReportSkip("TalkMain.IsSkip");
    }
    private static void MovieStart(object[] __args)
    {
        movieActive = true;
        movieButton = __args.Length > 2 && __args[2] is uint button ? button : 0;
        log?.LogInfo($"[STORY] {DateTime.UtcNow:O} MOVIE_PLAY file={__args.FirstOrDefault()} nativeSkipButton={movieButton}");
    }
    private static void MovieStop() => log?.LogInfo($"[STORY] {DateTime.UtcNow:O} MOVIE_STOP (callback completion not assumed)");
    private static void MovieEnd(MethodBase __originalMethod)
    {
        movieActive = false; movieButton = 0;
        log?.LogInfo($"[STORY] {DateTime.UtcNow:O} MOVIE_END via={__originalMethod.Name}");
    }
    private static void MovieUpdateStart() => movieUpdateDepth++;
    private static Exception? MovieUpdateEnd(Exception? __exception) { movieUpdateDepth = Math.Max(0, movieUpdateDepth - 1); return __exception; }
    private static bool CanRequestMovie() => movieUpdateDepth > 0 && enabled && Held && movieActive && movieButton != 0 && !choiceBlocked && !inputFailed;
    private static void MovieButtonQuery(object[] __args, ref bool __result)
    {
        if (CanRequestMovie() && __args.Length == 1 && __args[0] is not null &&
            unchecked((uint)Convert.ToInt64(__args[0], CultureInfo.InvariantCulture)) == movieButton)
        { __result = true; ReportSkip("Movie native button query"); }
    }
    private static void MovieButtonBits<T>(ref T __result) where T : struct, Enum
    {
        if (CanRequestMovie())
        {
            var bits = unchecked((uint)Convert.ToInt64(__result, CultureInfo.InvariantCulture));
            __result = (T)Enum.ToObject(typeof(T), bits | movieButton);
            ReportSkip("Movie native button bits");
        }
    }
    private static void ReportSkip(string path)
    {
        if (DateTime.UtcNow < nextSkipLog) return;
        nextSkipLog = DateTime.UtcNow.AddSeconds(1);
        log?.LogInfo($"[STORY] {DateTime.UtcNow:O} REQUEST_SKIP via={path}");
    }
}
