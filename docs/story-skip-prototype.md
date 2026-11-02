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

### Ordinary NPC samples: Koromon and Tokomon

The player identified two conversations during the same session (times in Paris, 2026-11-02):

| Player-identified NPC | Captured trace | Completion |
| --- | --- | --- |
| Koromon | `TalkMain.DispText` then `DispTextMain` at 20:03:34.175 | `CScenarioScript.TalkEventEndCB` at 20:03:35.589 |
| Tokomon | `TalkMain.DispText` then `DispTextMain` at 20:03:52.459; later text at 20:04:28.162 and `DispChoices` at 20:04:28.956 | Next `CScenarioScript.TalkEventEndCB` at 20:04:31.769 |

The NPC names come from the player's reports, not decoded event identifiers. The Tokomon interval is correlated by timing, with no intervening recorded completion before 20:04:31.769. A choice disables the prototype at 20:04:28.956, then text continues. At 20:04:32.794, `ItemStorageData.AddItemPlayer(2278877539, 10)` is followed by an inventory delta 0→10 at 20:04:33.047. The English catalogue resolves this ID to **Meat**. This establishes an item grant following the dialogue; it does not by itself establish a unique NPC/reward trigger.

No movie or voice-text command is recorded within these captured conversation intervals. No skip request appears in either captured interval; this is not an F8 behavior test. The earlier intro/scripted samples also used `DispText` / `DispTextMain`, and one ended through `TalkEventEndCB`. Consequently neither these display methods, the completion callback, choices nor the absence of voice establishes an ordinary-NPC classifier. The running binary still masks `_eventId` / `_blockId`; the prepared identifier fix needs deployment before comparing those values. Raw snapshots are kept locally under `.local/observations/` and are excluded from Git.

### Identifiers captured after deployment: Tokomon and Nyaromon

With the corrected tracing DLL active, the player reported speaking to Tokomon and Nyaromon, in that order. The following mapping is correlated with that reported order; names are not read from the game's NPC data. Times are Paris, 2026-11-02.

| Reported NPC | Event / command file | Block | Captured interval |
| --- | --- | --- | --- |
| Tokomon | `Event/Talk/EV_TOWN_B003` | `TALK_S06` | Initialize 20:10:24.503; matching TalkEventEndCB 20:10:26.243 |
| Nyaromon | `Event/Talk/EV_TOWN_B011` | `TALK_000` | Initialize 20:10:30.168; choice 20:10:32.989; matching callback 20:10:34.976 |
| Nyaromon, following block | `Event/Talk/EV_TOWN_B011` | `TALK_002` | Initialize 20:10:35.027; matching callback 20:10:35.936 |

Each initial block contains two `TalkMain.RotationGameModel` calls with `time=0.2`: unit 12 and player unit 0 turn toward each other in B003; unit 8 and player unit 0 turn toward each other in B011. Consecutive rotation calls are about 0.217 seconds apart. This is evidence of presentation commands inside ordinary NPC interactions; the presence of character motion alone cannot classify a story scene. The time unit and acceleration behavior still need native/runtime testing before modifying durations.

Both use `TalkMain.DispText` / `DispTextMain`. No SceneMain call, movie event, tutorial event, voice-text call or skip request is recorded within these intervals. The choice disables the F8 prototype as designed. Importantly, B011 emits a completion callback for TALK_000 and then starts TALK_002: `TalkEventEndCB` can mark a block boundary within one interaction, not necessarily the end of the whole NPC conversation.

The same event/block strings are now visible at initialization and completion. This validates identifier logging for these examples. It does not prove an exhaustive NPC-to-event mapping: story state and dialogue choices may select other blocks. Do not classify every `Event/Talk/` file as optional dialogue without scripted-story comparison samples. Inventory deltas immediately before these interactions coincide with loading the save and must not be interpreted as new conversation rewards.

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

`TutorialObservation` prepares four observation-only prefixes: Initialize, the two scenario methods above, and SetTutorialAlreadyFlag. It logs bounded script/block identifiers and scalar arguments, without reading native objects, changing skip results, changing flags or calling completion callbacks. Install failures are reported independently from the dialogue prototype. It is gated by `Observation.Enabled`. Runtime installation and stability checks are recorded below.

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

`StoryPresentationObservation` now has 14 observation-only prefixes for these paths plus `TalkMain.Initialize` to retain command-file/block context, and `SceneMain.DispText` / `PlayVoiceText` to identify the dialogue player in use. `SceneMain.EndScene` is excluded after the startup failure described below. It records bounded, single-line presentation arguments and omits string arguments for these two dialogue methods. No dialogue bodies, per-frame updates, shared coroutine stubs, field actor AI or native object contents are logged. It is enabled with `Observation.Enabled`; patch failures are reported separately from the dialogue prototype. It changes neither arguments nor return values. Runtime installation and stability checks are recorded below.

The user describes animated 3D character vignettes with dialogue overlays. This is compatible with SceneMain, but TalkMain also has 3D actor/camera commands. Visual appearance alone cannot establish which player handles this particular scene. Compare the new entry-point traces before selecting an acceleration strategy.

Next comparison: enter the same scene normally and with F8, correlate presentation calls with dialogue and scenario completion, identify timed versus condition-based waits, then test a narrowly scoped acceleration path. Verify final actor/camera positions, returned controls, choices, expected rewards/flags and save/reload. No movement/animation acceleration option is implemented or runtime-validated yet; global timeScale acceleration is not part of this experiment.

## Deployment and startup correction (2026-11-02)

After the player saved and closed the game, the previous session log was preserved and the new DLL deployed. The first startup installed all 18 story, 5 tutorial and 15 presentation patches but exited before the first snapshot heartbeat. Windows reported an access violation in coreclr.dll. Removing the optional `TutorialMain.OnDisable` hook alone did not resolve it. The next attempt recorded an IL2CPP native-to-managed trampoline error for `EndScene`, where conversion of a supposed string argument failed with a negative length. This is evidence of an unsafe detour/entry signature interaction, not an exception thrown by scene progression. The exact native alias/root cause has not been established.

Both `TutorialMain.OnDisable` and `SceneMain.EndScene` are excluded. Optional tutorial/presentation traces now use `InteropTraceGuard`: resolve a unique generated MethodInfo pointer field, require a native entry inside GameAssembly.dll, reject recognized trivial return stubs and entries already reserved by another optional trace. This screening is tied to this Unity 2019 IL2CPP layout and is conservative, not a complete shared-entry/ABI verification. A method that cannot be resolved is skipped rather than patched. The excluded EndScene method was not reintroduced to test the guard.

The corrected deployment built with zero warnings/errors and started at 20:08:42 (Paris). Logs reported `STORY installed=18 failed=0`, `TUTORIAL installed=4 skipped=0 failed=0`, and `PRESENTATION installed=14 skipped=0 failed=0`, followed by chainloader completion, snapshot heartbeat and successful catalogue exports. The process was responding. These are startup/installation checks, not validation of every hook during gameplay or animation skipping. Scenario identifiers, tutorial observation and the 14 presentation traces are now deployed; older sections referring to masked identifiers describe the previous session. F8 acceleration of animations and suppression of tutorial windows remain unimplemented.

## Intro replay: timed camera commands and waits (2026-11-02)

The player restarted the intro with the corrected tracing DLL. Times below are Paris:

- 20:13:17.881: `TalkMain.Initialize(Event/Talk/EV_MIN_1000, EVENT1)`, followed by movie02 at 20:13:18.604. Movie completion at 20:13:32.849, then `TalkMain.Wait(dummy0=1)` and a matching scenario callback for EVENT1 at 20:13:36.007.
- 20:13:36.057: `TalkMain.Initialize(Event/Talk/EV_START, EVENT010)`. This is a confirmed intro context inside TalkMain, not proof of SceneMain involvement.
- 20:13:36.311: `MoveGameCamera(time=2)`, next `Wait(dummy0=1)` at 20:13:38.329; then another camera command at 20:13:39.331 with `time=3`, followed by `Wait(dummy0=2)` at 20:13:42.349 and dialogue at 20:13:44.354.
- 20:13:48.208: `REQUEST_SKIP via=TalkMain.IsSkip`, followed by `MoveGameCamera(time=1)` at 20:13:48.210 and `Wait(dummy0=1)` at 20:13:49.225. The prototype is toggled off at 20:13:50.004 and back on at 20:13:53.601. More camera commands follow with times 2, 0.2, 0.5 and 0.15.

The player reports F8 appearing ineffective. The trace confirms at least one dialogue skip request, while presentation commands still consume time. For these sampled commands, observed intervals closely match seconds encoded in camera `time` and Wait `dummy0`; this supports a timed-wait hypothesis for these examples, not every argument value or command variant. Do not reinterpret existing zero durations or negative/special values without investigation. A future scoped duration adjustment could target these commands, but no adjustment has been implemented or tested yet.

Both ordinary NPC files and this intro use the `Event/Talk/` directory and TalkMain. Neither directory prefix, 3D presentation nor TalkMain alone is a story/NPC classifier. Retain full file/block identifiers and explicitly verified contexts when designing automatic behavior. The running session is left uninterrupted for further tutorial and animation observations.

## Illustrated tutorial: opening and normal completion captured

During the intro replay, the player identified the illustrated tutorial window. The same-session trace now establishes a dedicated path (Paris times, 2026-11-02):

| Time | Observed call |
| --- | --- |
| 20:14:46.175 | `CScenarioScript.TalkEventEndCB(Event/Talk/EV_MIN_1000, EVENT2)` |
| 20:14:46.179 | `CScenarioScript.CmdExeStartTutorialUI(_vrec=<SCsvbVRecAccessInfo>)` |
| 20:14:46.183 | `TutorialMain.Initialize(command_csv_name=event/tutorial/EV_TUTORIAL_000, block_name=null)` |
| 20:14:46.185 | Inherited `TalkMain.Initialize` with the same file and null block |
| 20:14:46.312 | `TalkMain.Wait(dummy0=1)` |
| 20:14:47.348 onward | `TalkMain.DispText` / `DispTextMain`, repeated as tutorial pages advance |
| 20:15:09.451 | `CScenarioScript.TutorialUiEndCB()` |

No `SetTutorialAlreadyFlag` call or tutorial skip request is present within this captured interval. Do not assume that a seen flag was written through the monitored method. The command record is intentionally opaque, so a numeric tutorial UI ID is not decoded. The file identifier is available and usable as an observed example; the mapping is not a complete tutorial catalogue.

The dedicated TutorialMain initializer and TutorialUiEndCB distinguish this sample from the ordinary NPC interactions captured earlier. The shared text methods alone do not. Metadata shows that TutorialMain declares its own IsSkip override, while the prototype currently patches TalkMain.IsSkip only. This is a plausible explanation for F8 failing on this window, but the override's native implementation and whether it shares a native stub have not been verified. Do not patch it blindly, especially after the EndScene trampoline failure.

This provides a baseline for a future tutorial skip option: identify this context, find a safe native advancement path, retain normal tutorial completion and verify control/scenario continuation. Directly invoking TutorialUiEndCB, hiding the UI, or marking all tutorials seen remains unimplemented. The observed end callback is from the current unmodified tutorial flow, not evidence that skipping works.

### Additional intro animation evidence

In `Event/Talk/EV_MIN_1000`, block EVENT2, the replay records `AnimationModel` with `time=-1` and `MoveModel` with `time=-0.5` (model IDs include PLAYR, MUGED, WOGUR and METAG), interleaved with fades, voice/text and positive Wait values. These concrete negative values must not be treated as ordinary positive durations or blindly clamped. No SceneMain call is recorded in this captured segment: these particular 3D character vignettes are being driven through TalkMain commands. This does not establish that SceneMain is unused elsewhere in the game.
