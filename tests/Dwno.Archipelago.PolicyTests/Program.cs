using System;
using Dwno.Archipelago;

var checkedCases = 0;
void Check(string name, bool actual, bool expected)
{
    checkedCases++;
    if (actual != expected) throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
}

// Captured story blocks versus captured NPC/tutorial contexts are the regression boundary.
foreach (var (file, block) in new[] {
    ("Event/Talk/EV_START", "EVENT010"),
    ("Event/Talk/EV_MIN_1000", "EVENT1"), ("Event/Talk/EV_MIN_1000", "EVENT2"),
    ("Event/Talk/EV_MIN_1001", ""), ("Event/Talk/EV_MIN_1010", ""),
    ("Event/Talk/EV_MIN_1020", ""), ("Event/Talk/EV_MIN_1021", ""),
    ("Event/Talk/EV_MIN_1030", "") })
    Check($"observed intro {file}/{block}", StoryContextPolicy.IsKnownStory(file, block), true);

foreach (var (file, block) in new (string? File, string? Block)[] {
    ("Event/Talk/EV_TOWN_B003", "TALK_S06"), ("Event/Talk/EV_TOWN_B011", "TALK_000"),
    ("Event/Talk/EV_TOWN_B011", "TALK_002"), ("event/tutorial/EV_TUTORIAL_000", null),
    ("event/event_digitama_partner", null), ("Event/Talk/EV_MIN_9999", ""),
    ("Event/Talk/EV_START", "UNKNOWN"), ("Event/Talk/EV_MIN_1001", null), (null, null) })
    Check($"manual context {file}/{block}", StoryContextPolicy.IsKnownStory(file, block), false);

const string story = "Event/Talk/EV_START";
const string npc = "Event/Talk/EV_TOWN_B003";
Check("known story runs without F8", StoryContextPolicy.ShouldSkip(true, true, false, false, true, story, "EVENT010"), true);
Check("master stop overrides automatic story", StoryContextPolicy.ShouldSkip(false, true, false, false, true, story, "EVENT010"), false);
Check("master stop overrides held F8", StoryContextPolicy.ShouldSkip(false, true, false, true, true, story, "EVENT010"), false);
Check("focus loss stops automation", StoryContextPolicy.ShouldSkip(true, false, false, true, true, story, "EVENT010"), false);
Check("input failure stops automation", StoryContextPolicy.ShouldSkip(true, true, true, true, true, story, "EVENT010"), false);
Check("disabled automatic mode waits for F8", StoryContextPolicy.ShouldSkip(true, true, false, false, false, story, "EVENT010"), false);
Check("ordinary NPC remains manual", StoryContextPolicy.ShouldSkip(true, true, false, false, true, npc, "TALK_S06"), false);
Check("held F8 can skip NPC deliberately", StoryContextPolicy.ShouldSkip(true, true, false, true, true, npc, "TALK_S06"), true);
Check("released F8 stops manual NPC skipping", StoryContextPolicy.ShouldSkip(true, true, false, false, false, npc, "TALK_S06"), false);
foreach (var (input, expected) in new[] { ("3", "0.3"), ("2", "0.2"), ("1.5", "0.15"),
    ("0.5", "0.05"), ("0.25", "0.05"), ("0.2", "0.05"), ("1e1", "1"), ("120", "12") })
{
    var changed = StoryTimingPolicy.TryShorten(input, out var output);
    Check($"positive timing {input}", changed && output == expected, true);
}
foreach (var input in new string?[] { "0", "-1", "-0.5", "0.01", "0.05", "NaN", "Infinity",
    "1e309", "121", "1,5", "TIME_VARIABLE", "", null })
{
    var changed = StoryTimingPolicy.TryShorten(input, out var output);
    Check($"preserve special/short timing {input}", !changed && output == input, true);
}
foreach (var (deadline, now, expected) in new (float Deadline, float Now, float? Expected)[] {
    (101f, 100f, 100.05f), (102f, 100f, 100.05f), (100.02f, 100f, null),
    (99f, 100f, null), (1f, 100f, null), (103f, 100f, null),
    (float.NaN, 100f, null), (float.PositiveInfinity, 100f, null), (101f, float.NaN, null), (1f, -1f, null) })
{
    var changed = StoryTimingPolicy.TryShortenChoiceDeadline(deadline, now, out var output);
    Check($"choice timer {deadline}/{now}", expected.HasValue
        ? changed && Math.Abs(output - expected.Value) < 0.001f
        : !changed && output.Equals(deadline), true);
}
foreach (var (speed, expected) in new (float Speed, float? Expected)[] {
    (1f, 10f), (0.5f, 5f), (12f, 100f), (100f, null), (200f, null),
    (0f, null), (-1f, null), (float.NaN, null), (float.PositiveInfinity, null) })
{
    var changed = StoryTimingPolicy.TryScaleUiSpeed(speed, out var output);
    Check($"UI animation speed {speed}", expected.HasValue
        ? changed && output == expected.Value : !changed && output.Equals(speed), true);
}
Console.WriteLine($"Passed {checkedCases} story, stop-control, timing and transition policy checks. Native game behavior is not covered.");
