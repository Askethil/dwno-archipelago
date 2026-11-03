using System;
using System.Globalization;

namespace Dwno.Archipelago;

internal static class StoryTimingPolicy
{
    // Small positive durations keep native interpolation alive for several frames.
    internal static bool TryShorten(string? input, out string? output)
    {
        output = input;
        if (!double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) ||
            !double.IsFinite(seconds) || seconds <= 0 || seconds > 120) return false;
        var shortened = Math.Min(seconds, Math.Max(0.05, seconds * 0.1));
        if (shortened >= seconds) return false;
        output = shortened.ToString("0.######", CultureInfo.InvariantCulture);
        return true;
    }
}
