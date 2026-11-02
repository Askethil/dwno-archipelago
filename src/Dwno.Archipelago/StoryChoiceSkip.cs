using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;

namespace Dwno.Archipelago;

// Confine synthetic confirmation to the native dialogue-choice coroutine step.
internal static class StoryChoiceSkip
{
    private static readonly Harmony harmony = new(Plugin.Id + ".first-choice");
    private static ManualLogSource? log;
    private static PropertyInfo? ownerProperty, panelProperty, cursorProperty, countProperty;
    private static uint confirmButton, navigationButtons;
    private static IntPtr activeChoice;
    private static DateTime attemptStarted;
    private static bool confirmationReported;
    [ThreadStatic] private static bool confirmationScope;
    internal static bool Available { get; private set; }
    internal static bool Enabled { get; private set; }

    internal static void Install(Assembly assembly, ManualLogSource logger, bool requested)
    {
        log = logger;
        if (!requested) { logger.LogInfo("[CHOICE] DISABLED by Story.AutoSelectFirstChoice"); return; }
        try
        {
            var talk = assembly.GetType("TalkMain", true)!;
            var iterator = talk.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .Single(t => t.Name.StartsWith("_DispChoices_d__", StringComparison.Ordinal));
            if (!typeof(Il2CppObjectBase).IsAssignableFrom(iterator))
                throw new InvalidOperationException("Choice iterator is not an IL2CPP wrapper");
            var move = iterator.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
            if (move is null || move.ReturnType != typeof(bool) || move.GetParameters().Length != 0)
                throw new InvalidOperationException("Choice MoveNext signature differs");
            ownerProperty = Required(iterator, "__4__this");
            panelProperty = Required(iterator, "_panel_5__2");
            cursorProperty = Required(talk, "m_cursor");
            countProperty = Required(talk, "m_maxChoiceNum");
            if (ownerProperty.PropertyType != talk || cursorProperty.PropertyType != typeof(int) ||
                !cursorProperty.CanWrite || countProperty.PropertyType != typeof(int))
                throw new InvalidOperationException("Choice owner/cursor/count schema differs");
            var buttonType = assembly.GetType("PadManager", true)!.GetNestedType("BUTTON")!;
            uint Button(string name) => Convert.ToUInt32(Enum.Parse(buttonType, name), CultureInfo.InvariantCulture);
            confirmButton = Button("bOK");
            navigationButtons = new[] { "dUp", "dDown", "dLeft", "dRight", "slUp", "slDown", "slLeft", "slRight" }
                .Aggregate(0u, (bits, name) => bits | Button(name));
            if (confirmButton == 0 || (confirmButton & (confirmButton - 1)) != 0 ||
                (confirmButton & navigationButtons) != 0)
                throw new InvalidOperationException("Unexpected confirmation button mask");
            if (!InteropTraceGuard.CanTrace(move, out var reason))
                throw new InvalidOperationException("Unsafe choice coroutine entry: " + reason);
            HarmonyMethod Typed(string method) => new(typeof(StoryChoiceSkip).GetMethod(method,
                BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(iterator));
            harmony.Patch(move, prefix: Typed(nameof(BeforeStep)), postfix: Typed(nameof(AfterStep)),
                finalizer: new HarmonyMethod(typeof(StoryChoiceSkip), nameof(EndStep)));
            Available = true;
            Enabled = true;
            logger.LogInfo($"[CHOICE] READY iterator={iterator.FullName} firstIndex=0 confirmMask={confirmButton}; held F8 or verified automatic story context; F10 toggles automatic choices; runtime behavior unverified.");
        }
        catch (Exception ex)
        {
            harmony.UnpatchSelf();
            Available = false;
            logger.LogWarning($"[CHOICE] UNAVAILABLE: {ex.GetBaseException().Message}; choices remain manual.");
        }
    }

    private static PropertyInfo Required(Type type, string name) => type.GetProperty(name,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new MissingMemberException(type.FullName, name);

    internal static void Toggle()
    {
        if (!Available)
        {
            log?.LogInfo("[CHOICE] UNAVAILABLE: automatic choices cannot be enabled this session.");
            return;
        }
        Enabled = !Enabled;
        confirmationScope = false;
        attemptStarted = default;
        log?.LogInfo($"[CHOICE] {DateTime.UtcNow:O} ENABLED={Enabled}");
        if (!Enabled)
            StorySkip.PauseForChoice("F10 stopped automatic choices; respond manually, then F9 to resume dialogue skipping");
    }

    private static void BeforeStep<T>(T __instance, out bool __state) where T : Il2CppObjectBase
    {
        __state = confirmationScope;
        confirmationScope = false;
        if (!Available || !Enabled || !StorySkip.SkipRequested) { attemptStarted = default; return; }
        try
        {
            var owner = ownerProperty!.GetValue(__instance);
            // Let the coroutine construct its panel before supplying any input.
            if (owner is null || panelProperty!.GetValue(__instance) is null) return;
            var count = (int)countProperty!.GetValue(owner)!;
            if (count < 1 || count > 3) throw new InvalidOperationException($"Unexpected choice count {count}");
            if (activeChoice != IntPtr.Zero && activeChoice != __instance.Pointer)
                throw new InvalidOperationException("Concurrent choice coroutines are unsupported");
            if (activeChoice == IntPtr.Zero)
            {
                activeChoice = __instance.Pointer;
                confirmationReported = false;
                log?.LogInfo($"[CHOICE] {DateTime.UtcNow:O} ARMED firstIndex=0 count={count}");
            }
            if (attemptStarted == default) attemptStarted = DateTime.UtcNow;
            if (DateTime.UtcNow - attemptStarted > TimeSpan.FromSeconds(10))
                throw new TimeoutException("Native choice did not complete within ten seconds of requested skipping");
            cursorProperty!.SetValue(owner, 0);
            confirmationScope = true;
        }
        catch (Exception ex) { Fail(ex); }
    }

    private static void AfterStep<T>(T __instance, bool __result) where T : Il2CppObjectBase
    {
        if (__result) return; // Native iterator has not completed yet.
        if (activeChoice == __instance.Pointer)
        {
            log?.LogInfo($"[CHOICE] {DateTime.UtcNow:O} NATIVE_COMPLETED confirmationRequested={confirmationReported}");
            activeChoice = IntPtr.Zero;
            attemptStarted = default;
        }
        StorySkip.ChoiceFinished();
    }

    private static Exception? EndStep(bool __state, Exception? __exception)
    {
        confirmationScope = __state;
        if (__exception is not null) Fail(__exception);
        return __exception; // Preserve the original exception.
    }

    private static bool CanConfirm => Available && Enabled && confirmationScope && StorySkip.SkipRequested;
    internal static uint FilterButtons(uint bits)
    {
        if (!CanConfirm) return bits;
        ReportConfirmation();
        return (bits & ~navigationButtons) | confirmButton;
    }

    internal static bool FilterQuery(uint buttons, ref bool result)
    {
        if (!CanConfirm) return false;
        if (buttons == confirmButton) { result = true; ReportConfirmation(); return true; }
        if (buttons != 0 && (buttons & ~navigationButtons) == 0) { result = false; return true; }
        return false;
    }

    private static void ReportConfirmation()
    {
        if (confirmationReported) return;
        confirmationReported = true;
        log?.LogInfo($"[CHOICE] {DateTime.UtcNow:O} REQUEST_CONFIRM firstIndex=0 via=native-choice-input");
    }

    private static void Fail(Exception ex)
    {
        Available = false;
        Enabled = false;
        confirmationScope = false;
        log?.LogWarning($"[CHOICE] DISABLED {ex.GetBaseException().Message}");
        StorySkip.PauseForChoice("automatic choice unavailable; choose manually, then F9 to resume dialogue skipping");
    }
}
