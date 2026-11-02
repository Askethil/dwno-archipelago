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

For movies, the prototype records the skip-button value passed to `Movie.Play`. It supplies that same button only within the synchronous native `Movie.Update` call, through `PadManager` queries. With a zero value, the prototype injects no button; **this does not establish that the movie is unskippable**. Other input paths may handle Start. This avoids calling `Stop`, `_PlayEnd`, callbacks or save/flag setters directly. Multiple concurrent movie players and other asynchronous input paths remain unsupported/unverified.

All patches must install successfully; otherwise they are removed and the feature is unavailable for the session. `READY` means installation only, not successful skipping. Input errors disable skip requests. Text bodies are omitted from traces. Event IDs, movie paths, choice boundaries and completion events are logged.

## Validation plan

1. Confirm startup with feature disabled and no regression in normal controls.
2. On a player-created test save, observe one normal dialogue and one normal movie to establish their native completion trace.
3. Toggle F9, hold F8 during one dialogue, release, then verify control returns and expected story flags/rewards occur.
4. Test a dialogue with a choice. Verify no choice is automatically confirmed and the selected branch is respected.
5. Test a skippable movie. Verify native movie completion and scenario completion callbacks, then restored camera/input/audio.
6. Test native Start on videos separately from F8. A zero recorded skip mask does not determine whether Start can skip the movie. Scripted 3D scenes are distinct from video playback and need separate validation.
7. Save/reload the test slot and verify progression. Compare normal vs skipped paths before enabling an automatic mode.

Recruitments and region unlocks are deliberately outside this prototype. No promise of preserved progression is made until the comparison tests pass.

## Installation checks (2026-11-02)

Release build passed with zero warnings/errors. The first attempt detected that `PadManager.GetInput/GetTrigger` return the native `BUTTON` enum rather than UInt32; the incomplete patch set was removed automatically. The corrected version closes a typed generic postfix over the actual enum and validates signatures. Startup then reported `installed=18 failed=0 enabled=False`; the process remained responding. This validates installation only. Native skip behavior, callbacks, input restoration and save progression still require user tests.

## First dialogue test (2026-11-02)

The user reports that holding F8 appeared to advance the dialogue. Logs correlate with this observation: prototype enabled at 19:48:50 (Paris), `REQUEST_SKIP via=TalkMain.IsSkip` at 19:48:52 and 19:48:55, followed by `CScenarioScript.TalkEventEndCB` at 19:48:56. Dialogue progression and a scenario completion callback are therefore observed for this test. This does not yet validate every dialogue, choice, reward, story flag or save/reload result.

The preceding `movie02` sequence used `nativeSkipButton=0`. The prototype did not force it; `_PlayEnd` occurred at 19:48:11 and `TalkEventEndCB` at 19:48:14. This establishes completion with no F8 movie request, not whether playback ended naturally or through another native input.

Next tests: a dialogue with choices, restored movement/camera after the completed dialogue, and persistence of expected story progress. Automatic F8 handling of movies with zero skip masks remains unsupported; native Start is available for manual testing.

## Native Start feedback (2026-11-02)

The user reports that videos can be skipped with Start. This corrects the earlier interpretation of `nativeSkipButton=0`: it must not be used to classify a movie as unskippable. `movie03` was captured with a zero value and `_PlayEnd` at 19:50:03 (Paris), but the log alone does not establish which input ended it. Prioritize dialogue and scripted-scene tests; use the game's native Start behavior for videos for now. The number of remaining videos has not been established.

## Comparing scripted scenes and ordinary NPC dialogue

At 19:50:11 (Paris), after movie03 ended, the current session records `TalkMain.DispText` and `DispTextMain` without a new movie start. This distinguishes text presentation from a video event, not story dialogue from optional NPC dialogue. The same text methods can be shared.

The initial trace masked all strings in CScenarioScript calls, including `_eventId` and `_blockId`. The source is corrected to retain these bounded identifiers while still omitting dialogue text. This logging-only correction requires a later redeployment; it is not active in the ongoing tutorial session and cannot recover identifiers omitted from older logs.

Preserve the current scripted-scene snapshot, then record an ordinary NPC conversation after the tutorial. Compare movie events, TalkMain entry points, scenario requests/completion and choices. No automatic story/NPC classifier is established yet. For a reliable classification, collect event/block identifiers after redeployment and compare multiple examples rather than relying on text-method names alone.

## Tutorial windows: separate investigation (2026-11-02)

The user reports that F8 does not advance the current illustrated tutorial window. During the earlier F8 test, the log records `TalkMain.IsSkip` requests at 19:53:01–02 (Paris), several text/voice commands and `TalkEventEndCB` at 19:53:06. These demonstrate dialogue processing, not that the illustrated tutorial was skipped. The existing running binary has no tutorial-specific hooks, so silence during the window cannot identify its implementation.

Local interop metadata exposes these candidates:

| Candidate | Evidence from metadata | Runtime status |
| --- | --- | --- |
| `TutorialMain.Initialize(command_csv_name, block_name)` | Dedicated type derived from TalkMain; also declares its own `IsSkip` | Unverified for the current illustrated window |
| `CScenarioScript.CmdExeStartTutorialUI(_vrec)` | Dedicated scenario command | Unverified |
| `CScenarioScript.TutorialUiEndCB()` | Dedicated completion callback | Unverified; do not invoke manually |
| `PlayerData.SetTutorialAlreadyFlag(_flag, _value)` / `IsTutorialAlreadyFlag(_flag)` | Persistent seen-state accessors; `m_TutorialAlready` bitset | Scope and effect unverified; do not set flags to suppress tutorials yet |
| `AppInfo.m_battleTutorialType` | Separate battle tutorial state | Not established as relevant to illustrated help windows |

`TutorialObservation` prepares five observation-only prefixes: Initialize, OnDisable, the two scenario methods above, and SetTutorialAlreadyFlag. It logs bounded script/block identifiers and scalar arguments, without reading native objects, changing skip results, changing flags or calling completion callbacks. Install failures are reported independently from the dialogue prototype. It is gated by `Observation.Enabled`. This source change requires a later deployment/restart and is **not active in the ongoing session**.

For a future `SkipTutorials` option, first match opening, manual completion and seen-flag traces for the same window. Determine whether native completion can be requested while preserving scenario continuation and input restoration. Distinguish illustrated help from interactive battle/training tutorials. Merely hiding an interface, bypassing its command or marking all tutorials as seen has not been validated and is not implemented.

## Movement and animation waits (2026-11-02)

The user requests skipping the pauses caused by characters moving or animating during scripted scenes. The existing F8 implementation changes `TalkMain.IsSkip` only. The current log contains requests and subsequent dialogue commands, but does not trace movement or animation commands, so it cannot attribute gaps between lines to a particular action.

Local interop metadata identifies these presentation paths:

| Command | Available arguments | What remains unknown |
| --- | --- | --- |
| `TalkMain.MoveModel` | id, pos, roty, time, no, key | Time unit, interpolation and completion behavior |
| `TalkMain.AnimationModel` | id, mtn, time, dummy3–5 | Whether time is a duration, sentinel or wait control |
| `TalkMain.MoveGameModel` | unit_id, pos, dummy2–5 | No explicit duration; field movement completion needs investigation |
| `TalkMain.RotationGameModel` | unit_id, target_id, time, dummy3–5 | Completion and interaction with field actors |
| `TalkMain.MoveGameCamera` | target_id, pos, rot, time, dummy4–5 | Camera restoration and final transform |
| `TalkMain.CameraZoom` / `FadeModel` | z/time or id/alpha/time | Presentation timing and interaction with parallel tasks |
| `TalkMain.Wait` | dummy0–5 | Which argument encodes the wait and in what units |
| `SceneMain` | Initialize, StartScene, EndScene, AnimationModel | Separate 3D scene player, with cut/frame/Animator fields; no dedicated IsSkip found in its declared methods |

The dump also shows `TalkMain.MoveModel` as a coroutine state machine with a string `time` field. Method signatures and field names do not establish native behavior. A command-call timestamp is not the start/end time of its coroutine. Do not replace an enumerator with an empty one: that could omit final positions or cleanup. Do not zero durations or signal completion without understanding the command.

`StoryPresentationObservation` prepares 15 observation-only prefixes for these paths plus `TalkMain.Initialize` to retain command-file/block context, and `SceneMain.DispText` / `PlayVoiceText` to identify the dialogue player in use. It records bounded, single-line presentation arguments and omits string arguments for these two dialogue methods. No dialogue bodies, per-frame updates, shared coroutine stubs, field actor AI or native object contents are logged. It is enabled with `Observation.Enabled`; patch failures are reported separately from the dialogue prototype. It changes neither arguments nor return values. **The source is compiled but requires a later deployment/restart; these hooks are not in the ongoing session.**

The user describes animated 3D character vignettes with dialogue overlays. This is compatible with SceneMain, but TalkMain also has 3D actor/camera commands. Visual appearance alone cannot establish which player handles this particular scene. Compare the new entry-point traces before selecting an acceleration strategy.

Next comparison: enter the same scene normally and with F8, correlate presentation calls with dialogue and scenario completion, identify timed versus condition-based waits, then test a narrowly scoped acceleration path. Verify final actor/camera positions, returned controls, choices, expected rewards/flags and save/reload. No movement/animation acceleration option is implemented or runtime-validated yet; global timeScale acceleration is not part of this experiment.
