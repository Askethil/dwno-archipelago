using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace Dwno.Archipelago;

// Boost only the active TalkMain's background/window Animators, never global time or asset loading.
internal static class StoryTransitionSkip
{
    private sealed record BorrowedSpeed(object Animator, PropertyInfo Property, float Original, float Applied);
    private static readonly Harmony harmony = new(Plugin.Id + ".dialogue-transitions");
    private static readonly Dictionary<IntPtr, BorrowedSpeed> borrowed = new();
    private static ManualLogSource? log;
    private static Il2CppObjectBase? owner;
    private static PropertyInfo? backgroundProperty, rootsProperty;
    private static DateTime started, nextScan;
    private static bool available, textReported;

    internal static void Install(Assembly assembly, ManualLogSource logger, bool requested)
    {
        log = logger;
        if (!requested) { logger.LogInfo("[TRANSITION] DISABLED by Story.AccelerateDialogueTransitions"); return; }
        try
        {
            var talk = assembly.GetType("TalkMain", true)!;
            backgroundProperty = talk.GetProperty("m_backGroundAnimator") ?? throw new MissingMemberException("TalkMain.m_backGroundAnimator");
            rootsProperty = talk.GetProperty("m_ui_root") ?? throw new MissingMemberException("TalkMain.m_ui_root");
            var initialize = talk.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly).Single(m => m.Name == "Initialize");
            if (initialize.ReturnType != typeof(void) || initialize.GetParameters().Length != 2 ||
                initialize.GetParameters().Any(p => p.ParameterType != typeof(string)))
                throw new InvalidOperationException("Unexpected TalkMain.Initialize signature");
            if (!InteropTraceGuard.CanTrace(initialize, out var reason)) throw new InvalidOperationException(reason);
            HarmonyMethod Typed(string name) => new(typeof(StoryTransitionSkip).GetMethod(name,
                BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(talk));
            harmony.Patch(initialize, prefix: Typed(nameof(CaptureOwner)));
            available = true;
            var observed = 0;
            foreach (var name in new[] { "OpenBackGround", "CloseBackGround", "ChangeUIMode" })
            {
                try
                {
                    var method = talk.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly).Single(m => m.Name == name);
                    if (!InteropTraceGuard.CanTrace(method, out var why))
                    { logger.LogWarning($"[TRANSITION] TRACE_SKIPPED {name}: {why}"); continue; }
                    harmony.Patch(method, prefix: Typed(nameof(TransitionCommand)));
                    observed++;
                }
                catch (Exception ex) { logger.LogWarning($"[TRANSITION] TRACE_FAILED {name}: {ex.GetBaseException().Message}"); }
            }
            logger.LogInfo($"[TRANSITION] READY ownerHook=1 commandTraces={observed} UIAnimatorFactor=10; runtime behavior unverified; original speeds restored on stop.");
        }
        catch (Exception ex)
        {
            available = false;
            harmony.UnpatchSelf();
            logger.LogWarning($"[TRANSITION] UNAVAILABLE {ex.GetBaseException().Message}");
        }
    }

    private static void CaptureOwner<T>(T __instance) where T : Il2CppObjectBase
    {
        Release();
        owner = __instance;
        started = DateTime.UtcNow;
        nextScan = default;
        textReported = false;
        log?.LogInfo($"[TRANSITION] {started:O} CONTEXT_BEGIN");
    }

    private static void TransitionCommand<T>(T __instance, MethodBase __originalMethod) where T : Il2CppObjectBase
    {
        if (owner?.Pointer != __instance.Pointer) return;
        log?.LogInfo($"[TRANSITION] {DateTime.UtcNow:O} COMMAND {__originalMethod.Name} elapsedMs={(DateTime.UtcNow - started).TotalMilliseconds:F0}");
        nextScan = default;
        Tick();
    }

    internal static void TextRequested()
    {
        if (!available || owner is null || textReported) return;
        textReported = true;
        log?.LogInfo($"[TRANSITION] {DateTime.UtcNow:O} FIRST_TEXT_COMMAND elapsedMs={(DateTime.UtcNow - started).TotalMilliseconds:F0}; command entry, not visual-render timing");
    }

    internal static void Tick()
    {
        if (!available || !StorySkip.CanAcceleratePresentation || !Alive(owner)) { Release(); return; }
        if (DateTime.UtcNow < nextScan) return;
        nextScan = DateTime.UtcNow.AddMilliseconds(100);
        try
        {
            Borrow(backgroundProperty!.GetValue(owner));
            var roots = rootsProperty!.GetValue(owner);
            foreach (var panel in Elements(roots, 16))
            {
                if (!Alive(panel)) continue;
                var animators = panel!.GetType().GetProperty("m_animators")?.GetValue(panel);
                foreach (var animator in Elements(animators, 32)) Borrow(animator);
            }
        }
        catch (Exception ex)
        {
            available = false;
            log?.LogWarning($"[TRANSITION] DISABLED {ex.GetBaseException().Message}");
            Release();
        }
    }

    private static void Borrow(object? animator)
    {
        if (!Alive(animator) || animator is not Il2CppObjectBase native || borrowed.ContainsKey(native.Pointer)) return;
        var speed = animator.GetType().GetProperty("speed");
        if (speed?.PropertyType != typeof(float) || !speed.CanWrite || speed.GetValue(animator) is not float original ||
            !StoryTimingPolicy.TryScaleUiSpeed(original, out var applied)) return;
        // Record before writing so a setter failure can still attempt restoration.
        borrowed.Add(native.Pointer, new BorrowedSpeed(animator, speed, original, applied));
        speed.SetValue(animator, applied);
        log?.LogInfo($"[TRANSITION] {DateTime.UtcNow:O} UI_SPEED from={original} to={applied}");
    }

    internal static void Release()
    {
        foreach (var entry in borrowed.Values)
        {
            try
            {
                if (Alive(entry.Animator) && entry.Property.GetValue(entry.Animator) is float current && current == entry.Applied)
                    entry.Property.SetValue(entry.Animator, entry.Original);
            }
            catch (Exception ex) { log?.LogWarning($"[TRANSITION] RESTORE_FAILED {ex.GetBaseException().Message}"); }
        }
        if (borrowed.Count > 0) log?.LogInfo($"[TRANSITION] {DateTime.UtcNow:O} RELEASE count={borrowed.Count}");
        borrowed.Clear();
    }

    private static bool Alive(object? value) => value is Il2CppObjectBase native && native.Pointer != IntPtr.Zero &&
        (value is not UnityEngine.Object unity || unity);

    private static IEnumerable<object?> Elements(object? array, int max)
    {
        if (array is null) yield break;
        var type = array.GetType();
        var length = Convert.ToInt32(type.GetProperty("Length")!.GetValue(array));
        if (length < 0 || length > max) throw new InvalidOperationException($"Unexpected UI array length {length}");
        var indexer = type.GetProperties().Single(p => p.Name == "Item" && p.GetIndexParameters().Length == 1);
        var indexType = indexer.GetIndexParameters()[0].ParameterType;
        for (var i = 0; i < length; i++) yield return indexer.GetValue(array, new[] { Convert.ChangeType(i, indexType) });
    }
}
