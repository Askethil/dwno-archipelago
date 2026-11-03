namespace Dwno.Archipelago;

internal sealed class StorySkipOptions
{
    internal bool EnableManualOnStart { get; init; }
    internal bool AutoSkipStoryDialogue { get; init; } = true;
    internal bool AutoSkipMiniStoryDialogue { get; init; }
    internal bool AutoSelectFirstChoice { get; init; } = true;
    internal bool AcceleratePresentation { get; init; } = true;
    internal bool FastChoiceConfirmation { get; init; } = true;
    internal bool AccelerateDialogueTransitions { get; init; } = true;
    internal bool FastDialogueOpening { get; init; } = true;
}
