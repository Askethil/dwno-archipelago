# Story skip — experimental prototype

## Goal and controls

Find a way to accelerate story presentation while keeping the game interpreter and completion callbacks. This feature is **not yet validated for story progression**. No new game is created and no save slot is selected by the mod.

- Default: observation only, `Story.EnableHoldToSkip=false`.
- F9: toggle the prototype for the current session (logged).
- Hold F8: request skipping while enabled and the game is focused.
- Release F8 or press F9 again: stop making skip requests. Disabling does not undo a scene already advanced.

These controls are a test prototype, not an automatic randomizer option. Use a separate test save created manually by the player. Story dialogue context has not yet been distinguished from every optional NPC conversation.

## Paths inspected

`TalkMain` contains `IsSkip`, `DispTextMain`, `PlayVoiceText`, `DispText`, `DispChoices`, `PlayMovie` and `EventScene`. `Movie` contains `Play(movie_path, call_back, skipButton)`, `Update`, `Stop`, `_PlayEnd` and `OnDestroy`. `CScenarioScript` exposes `_RequestStartTalkEvent` and `TalkEventEndCB`.

The dialogue prototype sets the result of `TalkMain.IsSkip` only while enabled and F8 is held. It does not remove commands from the scenario interpreter. `DispChoices` disables the prototype and blocks further requests. Choose manually, then press F9 to re-enable after the choice has completed. No automatic resume on text commands. This guard does not prove that every selection UI is protected; explicitly test choices and keep them manual. Normal native controls remain available.

For movies, the prototype records the skip-button value passed to `Movie.Play`. It supplies that same button only within the synchronous native `Movie.Update` call, through `PadManager` queries. A zero button is respected as non-skippable. This avoids calling `Stop`, `_PlayEnd`, callbacks or save/flag setters directly. Multiple concurrent movie players and other asynchronous input paths remain unsupported/unverified.

All patches must install successfully; otherwise they are removed and the feature is unavailable for the session. `READY` means installation only, not successful skipping. Input errors disable skip requests. Text bodies are omitted from traces. Event IDs, movie paths, choice boundaries and completion events are logged.

## Validation plan

1. Confirm startup with feature disabled and no regression in normal controls.
2. On a player-created test save, observe one normal dialogue and one normal movie to establish their native completion trace.
3. Toggle F9, hold F8 during one dialogue, release, then verify control returns and expected story flags/rewards occur.
4. Test a dialogue with a choice. Verify no choice is automatically confirmed and the selected branch is respected.
5. Test a skippable movie. Verify native movie completion and scenario completion callbacks, then restored camera/input/audio.
6. Test a non-skippable sequence: zero skip mask must not be forced. Scripted 3D scenes are distinct from video playback and need separate validation.
7. Save/reload the test slot and verify progression. Compare normal vs skipped paths before enabling an automatic mode.

Recruitments and region unlocks are deliberately outside this prototype. No promise of preserved progression is made until the comparison tests pass.

## Installation checks (2026-11-02)

Release build passed with zero warnings/errors. The first attempt detected that `PadManager.GetInput/GetTrigger` return the native `BUTTON` enum rather than UInt32; the incomplete patch set was removed automatically. The corrected version closes a typed generic postfix over the actual enum and validates signatures. Startup then reported `installed=18 failed=0 enabled=False`; the process remained responding. This validates installation only. Native skip behavior, callbacks, input restoration and save progression still require user tests.

## First dialogue test (2026-11-02)

The user reports that holding F8 appeared to advance the dialogue. Logs correlate with this observation: prototype enabled at 19:48:50 (Paris), `REQUEST_SKIP via=TalkMain.IsSkip` at 19:48:52 and 19:48:55, followed by `CScenarioScript.TalkEventEndCB` at 19:48:56. Dialogue progression and a scenario completion callback are therefore observed for this test. This does not yet validate every dialogue, choice, reward, story flag or save/reload result.

The preceding `movie02` sequence used `nativeSkipButton=0`. The prototype did not force it; `_PlayEnd` occurred at 19:48:11 and `TalkEventEndCB` at 19:48:14. This is evidence of natural completion, not successful video skipping. A separate path for this sequence remains to be studied.

Next tests: a dialogue with choices, restored movement/camera after the completed dialogue, and persistence of expected story progress. Keep videos with zero skip masks unsupported until their completion path has been validated.
