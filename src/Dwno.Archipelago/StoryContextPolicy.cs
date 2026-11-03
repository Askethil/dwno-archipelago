using System;

namespace Dwno.Archipelago;

internal enum DialogueKind { Unclassified, Story, MiniStory, BasicNpc }

// Explicitly observed intro contexts. No filename-prefix classifier for unknown scenes.
internal static class StoryContextPolicy
{
    internal static bool ShouldSkip(bool enabled, bool focused, bool inputFailed, bool held,
        bool automaticStory, string? file, string? block, bool automaticMiniStory = false) =>
        enabled && focused && !inputFailed && (held || (Classify(file, block) switch {
            DialogueKind.Story => automaticStory,
            DialogueKind.MiniStory => automaticMiniStory,
            _ => false
        }));

    // This prefix is used for visual opening effects only, never story/autoskip classification.
    internal static bool HasDialogueOpening(string? file) =>
        file?.StartsWith("Event/Talk/", StringComparison.OrdinalIgnoreCase) == true;

    internal static DialogueKind Classify(string? file, string? block)
    {
        if (IsKnownStory(file, block)) return DialogueKind.Story;
        // Correlated with the player's Patamon recruitment vignette; not all EV_SUB files are recruitments.
        if (file == "Event/Talk/EV_SUB_0000_010" && block == "EVENT") return DialogueKind.MiniStory;
        if ((file == "Event/Talk/EV_TOWN_B002" && block is "TALK_000" or "TALK_001") ||
            (file == "Event/Talk/EV_TOWN_B003" && block is "TALK_S06" or "TALK_S09") ||
            (file == "Event/Talk/EV_TOWN_B011" && block is "TALK_000" or "TALK_002"))
            return DialogueKind.BasicNpc;
        return DialogueKind.Unclassified;
    }

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
