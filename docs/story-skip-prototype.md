# Story skip — experimental prototype

## Goal and controls

Find a way to accelerate story presentation while keeping the game interpreter and completion callbacks. This feature is **not yet validated for story progression**. No new game is created and no save slot is selected by the mod.

- Current source defaults: `Story.AutoSkipStoryDialogue=true`, `Story.AutoSkipMiniStoryDialogue=false`, `Story.AutoSelectFirstChoice=true`, `Story.AcceleratePresentation=true`, `Story.FastChoiceConfirmation=true`, `Story.AccelerateDialogueTransitions=true`, `Dialogue.FastOpening=true`, legacy manual startup setting `Story.EnableHoldToSkip=false`. Any enabled automatic dialogue category enables the master switch at startup.
- F9: toggle the master skip switch for the session. Turning it off stops automatic dialogue and held-key skip requests. Independent fast opening remains enabled when `Dialogue.FastOpening=true`.
- F8 held: manually request skip while the master is enabled and the game is focused, including contexts outside the observed story list.
- F10: toggle automatic first responses for the session. Turning this off also pauses the master skip switch so the player can answer manually. F9 resumes dialogue skipping; F10 must be enabled separately to restore automatic responses.
- Releasing F8 stops manual requests. Automatic story mode does not require F8; use F9 to stop it. Disabling does not undo a response already selected or a scene already advanced.

These controls are an experimental prototype. **Automatic story/choice handling and timed-presentation acceleration were deployed and exercised on 2026-11-03. The skip-scoped dialogue-transition and fast-choice refinements are also deployed; manual F8 on NPC dialogue has been exercised, while the shorter confirmation timer still needs a choice test. Category separation and always-fast opening are now deployed and startup-verified; a normal NPC conversation without F8 remains to test on this build.** Use a separate test save created manually by the player. Automatic story classification currently covers only the explicitly observed intro contexts listed below, not every story dialogue in the game. F10 manual recovery and save/reload progression still need explicit validation.

For an observation-only startup, disable AutoSkipStoryDialogue, AutoSkipMiniStoryDialogue, EnableHoldToSkip and Dialogue.FastOpening. Setting Dialogue.FastOpening false restores the dependency on an actual skip request for UI acceleration.

## Paths inspected

`TalkMain` contains `IsSkip`, `DispTextMain`, `PlayVoiceText`, `DispText`, `DispChoices`, `PlayMovie` and `EventScene`. `Movie` contains `Play(movie_path, call_back, skipButton)`, `Update`, `Stop`, `_PlayEnd` and `OnDestroy`. `CScenarioScript` exposes `_RequestStartTalkEvent` and `TalkEventEndCB`.

The new dialogue prototype sets `TalkMain.IsSkip` when the master switch is enabled, the game is focused, input has not failed, and either F8 is held or an explicitly observed story context is active with automatic mode enabled. It does not remove commands from the scenario interpreter. `DispChoices` blocks dialogue skip requests during selection. With the automatic-choice feature available and enabled, its native coroutine supplies first-response confirmation under the same request policy. Otherwise choices pause the master switch for a manual response and F9 resumes it. Text commands do not clear this guard; native choice completion does. Other selection UIs remain manual and unsupported.

For movies, the prototype records the skip-button value passed to `Movie.Play`. It supplies that same button only within the synchronous native `Movie.Update` call, through `PadManager` queries. With a zero value, the prototype injects no button; **this does not establish that the movie is unskippable**. Other input paths may handle Start. This avoids calling `Stop`, `_PlayEnd`, callbacks or save/flag setters directly. Multiple concurrent movie players and other asynchronous input paths remain unsupported/unverified.

All core story patches must install successfully; otherwise they are removed and skipping is unavailable for the session. Optional first-choice installation failures leave choices manual. `READY` means installation only, not successful skipping. Input errors disable skip requests. Text bodies are omitted from traces. Event IDs, movie paths, choice boundaries and completion events are logged.

## Validation plan

1. Confirm startup with feature disabled and no regression in normal controls.
2. On a player-created test save, observe one normal dialogue and one normal movie to establish their native completion trace.
3. Toggle F9, hold F8 during one dialogue, release, then verify control returns and expected story flags/rewards occur.
4. Test automatic first response with an observed story choice, then a manually requested NPC choice. Verify index 0 selects the displayed first branch and the native interpreter follows it. Test F10, F9, focus loss, released F8 outside automatic story, and a repeated-answer loop requiring a different manual response. Menus, egg selection and other selection UIs must remain unaffected.
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

### Additional illustrated tutorial files

The same replay captured four more tutorial files, each using `CmdExeStartTutorialUI` → `TutorialMain.Initialize` → inherited `TalkMain.Initialize` → `TutorialUiEndCB`. All initialized with `block_name=null`. Times are Paris, 2026-11-02:

| File under `event/tutorial/` | TutorialMain.Initialize | TutorialUiEndCB |
| --- | --- | --- |
| `EV_TUTORIAL_000` | 20:14:46.183 | 20:15:09.451 |
| `EV_TUTORIAL_001` | 20:15:19.682 | 20:15:22.869 |
| `EV_TUTORIAL_002` | 20:15:27.349 | 20:15:33.141 |
| `EV_TUTORIAL_003` | 20:15:36.457 | 20:15:39.963 |
| `EV_TUTORIAL_004` | 20:16:32.921 | 20:16:37.470 |

The last observed tutorial (004) immediately follows the callback for `Event/Talk/EV_MIN_1001`, with an empty string block ID, at 20:16:32.914. Preserve the distinction between an empty story block and a null tutorial block. No `SetTutorialAlreadyFlag` call is present in the captured sequence. These are five observed script identifiers, not an exhaustive tutorial list; their player-facing subjects have not been recorded. The repeated dedicated lifecycle supports identifying these tutorial windows separately from NPC dialogue, but no suppression/fast-forward behavior is validated.

### Additional intro animation evidence

In `Event/Talk/EV_MIN_1000`, block EVENT2, the replay records `AnimationModel` with `time=-1` and `MoveModel` with `time=-0.5` (model IDs include PLAYR, MUGED, WOGUR and METAG), interleaved with fades, voice/text and positive Wait values. These concrete negative values must not be treated as ordinary positive durations or blindly clamped. No SceneMain call is recorded in this captured segment: these particular 3D character vignettes are being driven through TalkMain commands. This does not establish that SceneMain is unused elsewhere in the game.

## Prepared automatic first response and story activation

The player explicitly requested taking the first response automatically, a way to stop this if it loops, and automatic activation during story dialogue. The source now implements these policies; it is not installed in the ongoing session yet.

`StoryChoiceSkip` resolves the generated `TalkMain._DispChoices_d__*` iterator by name, validates its bool MoveNext signature and expected owner/panel/cursor/count properties, and screens its native entry with InteropTraceGuard before patching. A typed closed-generic prefix/postfix is used for that exact iterator wrapper. During its synchronous native MoveNext step only, after the panel exists and the choice count is within 1–3, the owner's cursor is set to index 0 and native PadManager confirmation queries receive the game's `BUTTON.bOK` value (128 in current metadata). Directional queries are suppressed within that scope to keep the first response selected. No global button state, branch label, selection result or scenario callback is replaced. The native iterator remains responsible for input timing, branch selection and completion.

Index 0 matching the displayed first response and successful native confirmation are hypotheses still requiring a game test. The guard does not establish absence of every shared native body. Unknown layouts/counts, concurrent armed iterators, exceptions or an input attempt exceeding ten seconds disable automatic choices for the session and pause skipping for manual recovery. A repeated completed dialogue loop is handled by F10/F9, not detected automatically by this timeout. F10 stops automatic confirmation immediately on the next input update, pauses master skipping and allows another answer. Already executed responses cannot be undone. Outside the choice coroutine's synchronous step, its synthetic input scope is restored even on exceptions.

Automatic story activation is separate from generic skip mechanics. `StoryContextPolicy` recognizes these exact observed file/block pairs:

| File under `Event/Talk/` | Allowed block(s) |
| --- | --- |
| `EV_START` | `EVENT010` |
| `EV_MIN_1000` | `EVENT1`, `EVENT2` |
| `EV_MIN_1001`, `EV_MIN_1010`, `EV_MIN_1020`, `EV_MIN_1021`, `EV_MIN_1030` | empty string only |

The context is set by a core TalkMain.Initialize prefix independent of Observation.Enabled, and cleared by a matching event/block completion callback or replaced by the next initialization. Null and empty block IDs remain distinct. Ordinary NPCs, unknown story blocks, illustrated tutorials and `event/event_digitama_partner` are excluded from automatic activation. Holding F8 deliberately requests the generic path in other TalkMain dialogues. A filename prefix alone is not accepted as proof of story context.

The replay also confirmed actual SceneMain use: `TalkMain.EventScene(event_name=EV3D_1010_01)` at 20:17:59.730 (Paris), followed by `SceneMain.Initialize` at 20:17:59.938 and StartScene at 20:18:00.054. This is distinct from the earlier TalkMain-driven vignettes. The source does not yet accelerate native SceneMain playback, camera/animation waits, illustrated tutorials or movies with zero skip masks.

Initial validation before deployment: Release build with zero warnings/errors; `dotnet run --project tests/Dwno.Archipelago.PolicyTests -c Release` passed 26 checks covering observed intro pairs, ordinary NPC/tutorial/egg contexts, unknown contexts, null versus empty blocks, F9 master gating, manual F8, focus loss and input failure. These tests do not execute IL2CPP/Harmony or establish native choice behavior. The later timing extension and startup deployment are recorded below. A test-save first-choice and F10 manual-recovery comparison remains required. Preserve logs and confirm expected branch, progression and saved state before broadening automatic story coverage.

## Timed-presentation acceleration and first deployment (2026-11-03)

`Story.AcceleratePresentation=true` installs three optional prefixes on the exact TalkMain commands already observed during the intro: `Wait` (argument 0), `MoveGameCamera` (argument 3) and `RotationGameModel` (argument 2). All six parameters and the expected duration parameter name must match the current string signatures. The screening guard rejects unsafe entries; an incomplete timing patch set is removed independently so original timings remain available.

During an eligible skip request, with no choice or movie active, positive finite invariant-culture durations up to 120 seconds are multiplied by 0.1, with a 0.05-second minimum. Existing durations below that minimum are not increased. Zero, negative values, nonnumeric variables, comma-decimal strings, NaN/infinity and values above 120 are preserved. Logs record every changed command as `[TIMING] SHORTEN ... from=... to=...`. The original native command still executes; no enumerator is replaced or completion callback forced.

This accelerates these presentation waits and camera/rotation commands, not every character animation or cutscene. `MoveModel`, `AnimationModel`, native SceneMain playback, tutorial IsSkip overrides and video masks are unchanged. The relationship between a shortened wait and parallel animation/fade cleanup must still be checked in gameplay. F9 prevents further skip requests; it cannot restore the duration of a command already started. F10 disables first-response automation and pauses skipping for manual recovery.

InteropTraceGuard now records the MethodInfo owning each reserved entry. It permits an observer and accelerator to compose on the **same** method while still rejecting a different method sharing that entry. This is conservative screening, not full ABI/alias validation. Previously excluded EndScene and TutorialMain.OnDisable remain excluded.

Release build passed with zero warnings/errors. The policy harness now passes **47 checks**, including positive timing reductions and preservation of negative/zero/special/short values. The old installed DLL and session log were preserved, then the new DLL deployed while the game was closed. Startup at 05:04:04 (Paris) reported:

```text
[CHOICE] READY iterator=TalkMain+_DispChoices_d__20 firstIndex=0 confirmMask=128
[TIMING] READY installed=3 factor=0.1 floorSeconds=0.05
[STORY] READY installed=19 failed=0 enabled=True automaticKnownStory=True
[TUTORIAL] READY installed=4 skipped=0 failed=0
[PRESENTATION] READY installed=14 skipped=0 failed=0
```

Chainloader completion, snapshot heartbeat and catalogue exports followed; the process was responding without an error in the startup log. This validates installation, not native first-response ordering, successful acceleration, story rewards/flags, final camera/actor transforms or persistence. The next test is a player-started intro on a separate test slot without F8, followed by a choice/F10/manual-answer comparison. The agent did not create a new game, select a save slot or confirm a response.

## First live autoskip test and feedback (2026-11-03)

The player started a test intro without F8 and reports that the acceleration works well, including camera/character presentation. Traces show a timed camera request reduced from 2 to 0.2 seconds at 05:06:07.664 (Paris); the next Wait command starts at 05:06:07.885, roughly 0.22 seconds later. That Wait is reduced from 1 to 0.1 seconds; the next camera command starts at 05:06:07.987, roughly 0.10 seconds later. EV_START/EVENT010 emits its normal scenario completion callback at 05:06:10.126. This is runtime evidence for these sampled commands and automatic dialogue requests, not proof that every actor/camera end state is correct. AnimationModel and negative MoveModel timing values are still unmodified.

Two native choice iterators complete after synthetic confirmation:

| Story file | Armed | Request confirmation | Native completed |
| --- | --- | --- | --- |
| `EV_MIN_1010` | 05:10:42.487, count 2 | 05:10:43.495 | 05:10:43.497 |
| `EV_MIN_1030` | 05:11:47.694, count 3 | 05:11:48.696 | 05:11:48.698 |

Subsequent contexts EV_MIN_1020 and EV_TOWN_B001 are recorded, respectively. The player observes the automatic acceptance as slow. These samples establish about one second from arming to the request, followed by native coroutine completion. First displayed response identity, F10/manual branch recovery and persistence are not yet independently checked.

The replay also encountered `event/tutorial/EV_TUTORIAL_000_1`, which remains outside automatic story activation like the other tutorials. Ordinary or unknown contexts including EV_TOWN_B003, EV_SUB_SETU_01 and EV_SUB_0000_010 remain manual. No prefix-based reclassification was added.

### Prepared refinement: dialogue opening and confirmation delay

The user reports another 1–2-second wait while the dialogue environment opens before text appears. Metadata identifies TalkMain.OpenBackGround, CloseBackGround, ChangeUIMode, the background Animator and the EventWindowPanel UI Animator arrays. These are candidates; the remaining visual wait is not yet proven to come entirely from their animations rather than asynchronous loading.

`StoryTransitionSkip` captures the exact TalkMain owner at Initialize using a typed prefix. During an eligible skip request only, it borrows positive speeds from that owner's background/window Animators and applies a factor of 10, capped at 100. Zero/negative/nonfinite/already-high speeds are left intact. It does not modify character Animators, global timeScale, asset loading, scene flags or completion callbacks. Original UI speeds are restored when skipping stops, focus is lost, a choice/movie blocks acceleration, the owner changes/dies or the input component is disabled. Restoration only overwrites a speed still equal to the applied value, preserving intervening external changes. Reflection/layout errors disable this optional feature and attempt restoration.

New `[TRANSITION]` traces mark initialization, background/UI-mode commands, first text-command entry, UI speed changes and restoration. First text-command timing is not a measurement of the first rendered glyph. These traces will distinguish an opening animation that responds to speed changes from remaining loading delays.

`Story.FastChoiceConfirmation` inspects the generated choice iterator's float `_button_ok_time_5__4` when available. It shortens only a plausible absolute deadline lying 0.05–2 seconds ahead of Unity Time.time, to now + 0.05 seconds. Countdown/elapsed/unknown/expired timers are not rewritten, and native confirmation still occurs through the original iterator/input path. Raw native timer and Unity time are logged at arming to verify this hypothesis in the game. Missing timer metadata retains native delay. F10/F9 controls remain available.

The combined refinement builds with zero warnings/errors and **66 policy checks** pass, including deadline bounds and UI speed rules. It was deployed after the prior game process had closed, with the old log preserved. Startup at 05:17:33 (Paris) reported `[TRANSITION] READY ownerHook=1 commandTraces=3 UIAnimatorFactor=10`, the native choice iterator ready, all three timing hooks and 19 core story patches with zero failures. Chainloader completion, heartbeats and catalogue exports followed; the process was responding without an error in the startup log. DLL/build verification passed before this refinement's deployment; the deployment script independently verifies the copied DLL hash.

### NPC manual-override test and normal-conversation boundary

The player reiterates that ordinary NPC conversations must remain normal, then reports that deliberate F8 on a PNJ works. The deployed trace records EV_TOWN_B002 blocks TALK_000 and TALK_001 with `knownStory=False`, explicit skip requests while F8 is used, UI speeds changing from 1 to 10 and RELEASE events. The next acquisitions again start at speed 1, supporting restoration in these samples. In warmed TALK_001 samples the first text-command entry occurs about 134 ms after initialization; TALK_000 samples vary (268–572 ms in the captured interval). These are command-entry timings, not visual-render measurements or a controlled before/after comparison.

At 05:19:03.570, EV_TOWN_B003/TALK_S09 initializes with `knownStory=False`; its first text-command entry follows at 05:19:04.145, and CloseBackGround at 05:19:05.732. No UI_SPEED or REQUEST_SKIP occurs in that captured interaction, consistent with a normal NPC conversation without the manual override. No classification was broadened to town/NPC filenames. The generic F8 route was used for testing; automatic activation remains limited to the explicit story file/block pairs.

No choice was encountered in the captured post-refinement NPC test, so the new bounded confirmation-timer adjustment is still unverified in-game. A short TalkMain choice with deliberate F8 can exercise it without replaying the intro; check NATIVE_TIMER, SHORTEN_CONFIRM_WAIT, native completion and F10 manual recovery. Unknown/deadline formats must retain their native delay.

## Three dialogue categories and independently fast openings

The player identifies Patamon as recruitable and confirms that its larger dialogue vignette passes with F8. The last captured vignette uses `Event/Talk/EV_SUB_0000_010`, block EVENT, with model c004, TalkMain.OpenBackGround, character fades and regular TalkMain.DispTextMain calls. Its first text command arrives about 2.427 seconds after initialization in the captured manual-skip sample. The larger visual layout does not by itself identify a different text engine. No global common-message window was forced or changed.

The player requests three categories, classified by the dialogue event rather than by NPC identity:

| Category | Default text/choice behavior | Current evidence coverage |
| --- | --- | --- |
| Story | Automatic when the skip master is enabled | Eight observed intro file/block pairs |
| Mini-story / recruitment | Manual; optional `Story.AutoSkipMiniStoryDialogue=true` | Patamon-correlated EV_SUB_0000_010 / EVENT only |
| Basic NPC dialogue | Manual; deliberate F8 override remains available | Observed B002 TALK_000/001, B003 TALK_S06/S09, B011 TALK_000/002 |

Unclassified is an evidence status, not a fourth narrative category. Unknown blocks and files remain manual. No EV_SUB or town filename-prefix rule is used to infer narrative category. A recruitable Digimon can have basic conversations as well as recruitment mini-story events; recruitment effects/flags are not established by this classifier. CONTEXT logs now include the category and both independent automatic-category settings. `StorySkipOptions` groups configuration values so these policies are not coupled to positional booleans.

### Always-fast visual opening

The user explicitly requests shortening the black-edge fade and character/window appearance **even when dialogue is read normally**. `Dialogue.FastOpening=true` is independent of text/choice autoskip and remains effective when F9 turns skipping off. It never enables native synthetic confirmation or changes the category of a dialogue.

The opening phase starts at TalkMain.Initialize for Event/Talk files and ends 350 ms after the first text-command request, or at the matching scenario completion. During that phase, while focused and with no choice/movie blocking it, the owner's UI/background Animators can run at the bounded faster speed. Positive Wait, MoveGameCamera, RotationGameModel and newly added FadeModel durations can also be shortened by the existing factor/floor policy. Model animation commands and negative movement timings remain intact. This covers appearance fades without globally increasing character/game time or bypassing asset requests.

The Event/Talk prefix is accepted here only to identify a visual opening, not to enable narrative autoskip. Tutorial files and egg selection are excluded from this always-fast phase. After the short opening grace, a basic NPC's text waits for normal player input and UI speeds return to their original values. If actual skip remains requested, the separate skip-scoped acceleration can continue. Asset loading can still contribute delay; the source does not promise zero wall-clock opening time. New logs identify the accelerated target as background or dialogue-window and whether the change occurs during opening.

The source builds without warnings/errors and **85 policy checks pass**, covering category boundaries, recruitment default-off, basic NPC text remaining manual despite opening eligibility, timer bounds and speed preservation. After the prior game process had closed, its log was preserved and this version deployed. Startup at 05:33:17 (Paris) reports four timing hooks including FadeModel, fastOpening=True, 19 story patches with zero failures, automaticKnownStory=True and automaticMiniStory=False. The transition owner hook and three command traces install successfully, followed by chainloader completion, heartbeats and catalogue exports. The process responds, no startup error appears, and the script confirms identical built/installed DLL hashes.

The next test is an ordinary NPC conversation **without F8**: observe opening acceleration but no REQUEST_SKIP, automatic response or shortened reading interval. Then verify F9 stops automatic story while the independent visual-opening option still works. Startup verification does not validate these gameplay outcomes or guarantee instantaneous asset loading.
