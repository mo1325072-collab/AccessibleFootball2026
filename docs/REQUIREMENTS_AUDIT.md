# Requirements and implementation audit

Checked 2026-11-12 against ORIGINAL_REQUEST.md, PROJECT.md, Tactical_Design_Specs.md and the later user corrections. This is an open work record, not a whole-game approval.

Later corrections win over older text: S passes and D crosses; Fares Awad is the selected commentary source; Enter releases incidents only after narration; recorded commentary precedes latest-only reader updates; only goals and penalties interrupt commentary. Optional automatic substitutions use fatigue or repeated-foul booking risk and the strongest exact-position replacement before a supported secondary position. Severe injuries to the human team are different: name the injured player and let the user choose from the bench. The earlier compatible-automatic injury repair missed this intended choice; this checkpoint corrects that miss, not a newly invented feature. Preserve source rosters, all reviewed commentary and user audio exclusions.

## Tested source behavior

### Edge-triggered controls and continuous movement

Implementation and evidence: KeyboardInputHandler and MatchFlowRegression exercise press/hold/release, Ctrl variants and reset

Remaining limitation: Native keyboard layout, NVDA focus and real WinForms handling not executed on Windows

### Pass reception, physical shots and defending

Implementation and evidence: Checked-in .NET9 physics harness 19/19, run directly with an isolated SDK9; mechanics harness 146/146, with explicit incident dismissal and pre-extra-time resume

Remaining limitation: Windows interaction not verified

### Two halves and switched goals

Implementation and evidence: PeriodTransitions and regression check kickoffs, directions, intervals and extra time

Remaining limitation: Native menu interaction unverified

### Measured statistics

Implementation and evidence: 25 comparative rows, navigation and producer tests

Remaining limitation: Visual/native reader interaction unverified

### Substitutions and opportunities

Implementation and evidence: 5/3, sixth/fourth in extra time, interval-free opportunities, no return after substitution or sent-off vacancy

Remaining limitation: Seven source roster gaps remain separate

### Severe injuries and forced replacement

Implementation and evidence: Actual reckless sliding fouls can produce severe injury. The human team now enters a pending choice, freezing clock, ball and optional auto-assistance. A modal injury menu names the player and lists legal bench entries with position/rating. Arrows and Enter choose the replacement; Escape cannot silently resume. The user may continue short; exhausted substitutions remove the player without inventing a replacement. The penalty/free-kick restart, taker and its separate Enter confirmation survive. Opponent injury choices remain computer-managed. Arabic/English engine-to-menu-to-user-choice tests cover the weaker chosen reserve winning over an unchosen stronger reserve.

Remaining limitation: Native window focus and audible screen-reader interaction are prepared but not executed on Windows. Injury tuning is a deterministic game rule, not a medical prediction.

### Temporary-period state

Implementation and evidence: Run-up, advantage and pending clearance cleared before new-period kickoff

Remaining limitation: Further whole-game scenario review remains open

### Tactical zero-dice design

Implementation and evidence: Production tackle-duel result is deterministic from attributes, position, pressure and tactics; optional legacy forced rolls are test fixtures only

Remaining limitation: Other physical spread/shot random variation is separate from the duel rule

### Crowd proximity

Implementation and evidence: Follows the actual attacking team's current goal, including second-half reversal

Remaining limitation: Original effects transfer and Windows playback blocked

### Radar and concise live speech

Implementation and evidence: Both radar keys reach the engine query; semantic switch/missed-tackle tags replace unsafe substring detection; no tutorial in restart or replay announcements

Remaining limitation: Native audible behavior still unverified

### National teams by confederation

Implementation and evidence: All 211 source teams partition exactly once into six categories; memberships checked against official FIFA lists

Remaining limitation: No source roster, inclusion policy or team name changed

### Tournament matchup choice

Implementation and evidence: User chooses both participating teams; no longer silently starts first two entries

Remaining limitation: Full bracket/season progression is not implemented. The reviewed originals do not specify progression formats or season saves, so this is unconfirmed scope, not a missing required feature.

### Settings

Implementation and evidence: Legendary difficulty preserved instead of downgraded to Hard. English pre-match, lineup, bench, roles, formation, tactics, pause, statistics and team-selection paths have direct menu/action tests. Window, draft, shootout and library labels use explicit language pairs. The settings language switch relabels controls in place without dropping current selections; the main menu rebuilds after switching.

Settings lifecycle repair: Save now writes through a same-directory temporary file and reports failure rather than swallowing it; the native form stays open and does not announce success on failure. An explicit save stamps the migrated version so a first saved Off choice is not overwritten. Legacy Off remains Off, and migration writes back to the requested path. Closing without Save restores previewed values and live consumers.

SettingsLifecycleHarness checks every stored property after save/reload and in a fresh process, then exercises production playback commands through a recording transport instead of a native device. It covers zero master/category volumes, stadium volume before first play, chants using crowd rather than SFX, stopping active channels on mute, single rather than squared crowd scaling, no crowd/menu-music starts from unrelated settings changes, requested menu-song unmute, gameplay beacon scaling, anthem scaling, commentary Off/current voice changes, duration/difficulty, extra time/penalties, optional auto-sub/tactics and native close-offside review. The legacy spoken-events flag now controls automatic announcements without disabling explicit radar. Existing live-match silence remains in force.

Remaining limitation: No native MCI device or Windows audio/reader/focus behavior was executed. These tests verify production commands and behavior, not that a speaker produced sound. English menu/action paths are exercised by the source harness; original Arabic names and recording transcripts remain source content.

### Optional advisory CLI

Implementation and evidence: Portable Models lookup, safe argument boundaries, concurrent stderr/stdout reads, failed-exit rejection and process-tree timeout; query moved off UI thread

Remaining limitation: Tests use fake process fixtures, not actual model inference. No model/runtime downloaded

### Referee/VAR truthfulness

Implementation and evidence: Active offside and restarts use native geometry/rules; unused name-only VAR preview no longer fabricates random centimeters or decisions

Remaining limitation: Qwen is not the match referee. Installing/choosing a model is a separate unresolved task

### Build and packaging

Implementation and evidence: Release cross-build; actual DB external to single-file EXE; real native controller recovered

Remaining limitation: Current production target is .NET8; ORIGINAL_REQUEST/PROJECT say .NET9, while the current README specifies .NET8 or higher. The discrepancy is preserved, not declared mandatory or revoked. SDK9 is locally available and a separate scratch retarget/build/test/publish succeeded, so SDK availability is not a blocker and production targets were not silently changed. Desktop deployment, native Windows/NVDA/SAPI/MCI execution and complete asset assembly are not verified.


### Team-selection callback routing

Implementation and evidence: Per-call navigation callbacks now reach the six-confederation submenu instead of silently falling back to a flat national-team list. Tests navigate the actual categories, confederations, teams and opponent exclusion.

Remaining limitation: Native keyboard and reader interaction still needs a Windows run.

### Explicit tactical radar

Implementation and evidence: Radar uses the selected UI language, identifies the current restart/taker, and measures geometric distance to the current attacking goal, including the switched second-half goal. Four event-output checks cover these paths. Four further keyboard-to-engine checks prove Space and explicit lineup requests remain available while an incident is held, without moving the ball/clock or firing a shot queued during narration.

Remaining limitation: Recorded Arabic commentary remains Arabic; no translated recordings are claimed.

### Native verification preparation

Implementation and evidence: The Windows-only test suite now uses the current S=pass/D=cross/Q=lofted-through-ball controls, supplies current optional match-start parameters, isolates menu key-routing tests from destructive selections, and invokes the real game timer instead of duplicating obsolete telemetry behavior. New native settings tests cover both language directions, accessible names, unsaved selections, all slider/checkbox saves and reloads, cancellation, and failed-save feedback. Native injury tests invoke the real timer, Escape guard, Down/Enter selection and focus restoration. The suite cross-builds with zero errors.

Remaining limitation: It has not run on Windows. These are prepared tests, not passing runtime evidence. No native Windows executor is available in the current authorized environment; no emulator, service enrollment or security change was used to pretend otherwise.

### Production simulation diagnostics

Implementation and evidence: The --test path now bypasses audio initialization and runs seven deterministic engine checks for the interval, switched goal, explicit resume, correct kickoff, held restart, incident confirmation, measured statistics and full time. The same production method is executed successfully by MatchFlowRegression.

Remaining limitation: The actual Windows executable --test command still needs a Windows run. Diagnostic fixture teams do not verify source-roster accuracy or sound playback.

### Actual shot direction and aiming

Implementation and evidence: Four direct behavior checks verify normalized up/down aiming and actual ball velocity toward the first-half right goal and the switched second-half left goal.

Remaining limitation: Native keyboard focus and audible goal-beacon direction remain unverified on Windows.

### Repeated and off-roster discipline

Implementation and evidence: Card sanctions now require membership in the match and an active on-pitch player. A dismissed player cannot accumulate duplicate sanctions/statistics, and an outsider cannot be silently attributed to the away team. The repeated-card assertion fails against the previous engine. A second yellow still removes exactly one active player.

Remaining limitation: This does not claim exhaustive coverage of all football disciplinary cases.

## Checks for this checkpoint

- MatchFlowRegression: 193 assertions. These exercise actual bilingual menu navigation/actions, source-name preservation, no-mirror selection, and explicit restart/goal-side radar. Run with the real database argument below.
- Existing ContextualMechanicsHarness: 146 assertions. Its tests now explicitly dismiss held restarts and resume the added interval; CPU readiness time starts only after dismissal.
- ContextualCommentaryHarness: 65 assertions passed. ChallengerPhysicsHarness: 19 assertions passed directly with SDK9 against the checked-in project.
- SettingsLifecycleHarness: 66 assertions passed. Native transport is recorded, not executed. The original settings logic fails 24 checks from the initial 60-check version of this harness, with only the transport seam added for fair command capture.
- Before/after evidence: the new pending-human-choice injury assertion fails against the earlier automatic-replacement engine. The old compatible-automatic assertion was corrected, not counted as proof of the user's intended choice. The new English-category test also fails against the prior TeamSelectionManager and passes after localization. The held-restart Space test fails against the previous engine and passes only after the held-input repair. Adding tests did not merely restate successful old behavior.
- Older database harness has stale fixed census, bench-depth and center-spot assumptions, plus real unresolved source-policy/roster failures. Its 3/13 result is not reported as passing.
- Older audio harness cannot begin without original TackleWon.wav. No audio pass or native playback pass is claimed.

```sh
dotnet run --project tests/MatchFlowRegression -c Release -- --database AccessibleFootball2026.Core/Data/players_database.json
dotnet run --project tests/ContextualMechanicsHarness -c Release
dotnet run --project tests/SettingsLifecycleHarness -c Release
dotnet build -c Release -p:EnableWindowsTargeting=true
```

## Open implementation work, not user testing homework

- Verify native bilingual settings, keyboard focus and screen-reader behavior on Windows. Menu/action source tests are not a substitute for that runtime check.
- Tournament/season progression is unconfirmed scope. The reviewed originals require competition categories but do not specify fixtures, standings, bracket formats or season saves. Do not invent official formats; recover an attributed original instruction before treating progression as required.
- Review remaining original acceptance criteria and add behavioral tests where absent.
- Recover original general effects, finish package integrity checks, then run native Windows accessibility/audio verification without asking the user to discover source defects.
- Source roster gaps, inclusion-policy conflict, actual optional-model decision and literal .NET target/deployment differences remain explicit. Do not delete source teams or invent players to satisfy old tests.

## Sources for national-team grouping

Football confederations, not geographic sovereignty, determine these categories. Australia is AFC; American Samoa is OFC. Exact source names remain untouched.

- https://inside.fifa.com/en/associations/AFC
- https://inside.fifa.com/associations/CAF
- https://inside.fifa.com/associations/CONCACAF
- https://inside.fifa.com/associations/CONMEBOL
- https://inside.fifa.com/associations/OFC
- https://inside.fifa.com/associations/UEFA
