namespace Dwno.Archipelago;

// Explicitly observed intro contexts. No filename-prefix classifier for unknown scenes.
internal static class StoryContextPolicy
{
    internal static bool ShouldSkip(bool enabled, bool focused, bool inputFailed, bool held,
        bool automaticStory, string? file, string? block) =>
        enabled && focused && !inputFailed && (held || (automaticStory && IsKnownStory(file, block)));

    internal static bool IsKnownStory(string? file, string? block) => file switch
    {
        "Event/Talk/EV_START" => block == "EVENT010",
        "Event/Talk/EV_MIN_1000" => block is "EVENT1" or "EVENT2",
        "Event/Talk/EV_MIN_1001" or "Event/Talk/EV_MIN_1010" or
        "Event/Talk/EV_MIN_1020" or "Event/Talk/EV_MIN_1021" or
        "Event/Talk/EV_MIN_1030" => block == "",
        _ => false
    };
}
