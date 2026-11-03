using System;
using System.Globalization;

namespace Dwno.Archipelago;

internal static class StoryTimingPolicy
{
    internal static bool TryShortenChoiceDeadline(float deadline, float now, out float output)
    {
        output = deadline;
        if (!float.IsFinite(deadline) || !float.IsFinite(now) || now < 0) return false;
        var remaining = deadline - now;
        // Only adjust a plausible, near-future absolute deadline. Unknown/countdown timers stay intact.
        if (remaining <= 0.05f || remaining > 2f) return false;
        var shortened = now + 0.05f;
        if (shortened <= now || shortened >= deadline) return false;
        output = shortened;
        return true;
    }

    internal static bool TryScaleUiSpeed(float original, out float output)
    {
        output = original;
        if (!float.IsFinite(original) || original <= 0) return false;
        var scaled = Math.Min(100f, original * 10f);
        if (!float.IsFinite(scaled) || scaled <= original) return false;
        output = scaled;
        return true;
    }

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
