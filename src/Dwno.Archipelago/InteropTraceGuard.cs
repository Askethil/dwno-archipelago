using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Dwno.Archipelago;

// Conservative screening for optional native traces, not a proof that a detour is safe.
internal static class InteropTraceGuard
{
    private static readonly HashSet<IntPtr> tracedEntries = new();

    internal static bool CanTrace(MethodInfo method, out string reason)
    {
        reason = "";
        var fields = method.DeclaringType!.GetFields(BindingFlags.Static | BindingFlags.NonPublic |
            BindingFlags.Public | BindingFlags.DeclaredOnly).Where(f =>
            f.Name.StartsWith("NativeMethodInfoPtr_" + method.Name + "_", StringComparison.Ordinal) &&
            f.FieldType == typeof(IntPtr)).ToArray();
        if (fields.Length != 1 || fields[0].GetValue(null) is not IntPtr info || info == IntPtr.Zero)
        { reason = "native method info not uniquely resolved"; return false; }

        // Unity 2019 IL2CPP MethodInfo begins with the native method pointer.
        var entry = Marshal.ReadIntPtr(info);
        using var process = Process.GetCurrentProcess();
        var module = process.Modules.Cast<ProcessModule>().FirstOrDefault(m =>
            string.Equals(m.ModuleName, "GameAssembly.dll", StringComparison.OrdinalIgnoreCase));
        if (module is null || entry.ToInt64() < module.BaseAddress.ToInt64() ||
            entry.ToInt64() > module.BaseAddress.ToInt64() + module.ModuleMemorySize - 8)
        { reason = "entry outside expected game module"; return false; }
        if (tracedEntries.Contains(entry))
        { reason = "entry already used by another optional trace"; return false; }

        var bytes = new byte[8];
        Marshal.Copy(entry, bytes, 0, bytes.Length);
        if (bytes[0] is 0xC3 or 0xC2 ||
            ((bytes[0] is 0x33 or 0x31) && bytes[1] == 0xC0 && bytes[2] == 0xC3) ||
            (bytes[0] == 0xB0 && bytes[2] == 0xC3) ||
            (bytes[0] == 0x0F && bytes[1] == 0x57 && bytes[2] == 0xC0 && bytes[3] == 0xC3))
        { reason = "trivial native return stub; shared entry risk"; return false; }

        // Reserve before patching; a failed patch must not make an alias eligible later.
        tracedEntries.Add(entry);
        return true;
    }
}
