# Requirements and implementation audit

Checked 2026-11-12 against ORIGINAL_REQUEST.md, PROJECT.md, Tactical_Design_Specs.md and the later user corrections. This is an open work record, not a whole-game approval.

Later corrections win over older text: S passes and D crosses; Fares Awad is the selected commentary source; Enter releases incidents only after narration; recorded commentary precedes latest-only reader updates; only goals and penalties interrupt commentary. Optional automatic substitutions use fatigue or repeated-foul booking risk and the strongest exact-position replacement before a supported secondary position. Preserve source rosters, all reviewed commentary and user audio exclusions.

## Tested source behavior

### Edge-triggered controls and continuous movement

Implementation and evidence: KeyboardInputHandler and MatchFlowRegression exercise press/hold/release, Ctrl variants and reset

Remaining limitation: Native keyboard layout, NVDA focus and real WinForms handling not executed on Windows

### Pass reception, physical shots and defending

Implementation and evidence: Physics harness source 19/19 via a temporary .NET8 runner; mechanics harness 146/146, with explicit incident dismissal and pre-extra-time resume

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

Implementation and evidence: Reckless sliding fouls from behind can produce severe injury; compatible replacement selected; unreplaced player leaves; actual restart and taker survive

Remaining limitation: Injury tuning is a deterministic game rule, not a medical prediction

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

Remaining limitation: Native language-switch behavior is not executed on Windows. English menu/action paths are exercised by the source harness; original Arabic team/player names and recording transcripts remain source content.

### Optional advisory CLI

Implementation and evidence: Portable Models lookup, safe argument boundaries, concurrent stderr/stdout reads, failed-exit rejection and process-tree timeout; query moved off UI thread

Remaining limitation: Tests use fake process fixtures, not actual model inference. No model/runtime downloaded

### Referee/VAR truthfulness

Implementation and evidence: Active offside and restarts use native geometry/rules; unused name-only VAR preview no longer fabricates random centimeters or decisions

Remaining limitation: Qwen is not the match referee. Installing/choosing a model is a separate unresolved task

### Build and packaging

Implementation and evidence: Release cross-build; actual DB external to single-file EXE; real native controller recovered

Remaining limitation: Current target is .NET8, unlike the old .NET9 request. Desktop deployment, native Windows/NVDA/SAPI/MCI execution and complete asset assembly are not verified


### Team-selection callback routing

Implementation and evidence: Per-call navigation callbacks now reach the six-confederation submenu instead of silently falling back to a flat national-team list. Tests navigate the actual categories, confederations, teams and opponent exclusion.

Remaining limitation: Native keyboard and reader interaction still needs a Windows run.

### Explicit tactical radar

Implementation and evidence: Radar uses the selected UI language, identifies the current restart/taker, and measures geometric distance to the current attacking goal, including the switched second-half goal. Four event-output checks cover these paths. Four further keyboard-to-engine checks prove Space and explicit lineup requests remain available while an incident is held, without moving the ball/clock or firing a shot queued during narration.

Remaining limitation: Recorded Arabic commentary remains Arabic; no translated recordings are claimed.

### Native verification preparation

Implementation and evidence: The Windows-only test suite now uses the current S=pass/D=cross/Q=lofted-through-ball controls, supplies current optional match-start parameters, isolates menu key-routing tests from destructive selections, and invokes the real game timer instead of duplicating obsolete telemetry behavior. New native settings tests cover both language directions, accessible names and retention of unsaved selections. The suite cross-builds with zero errors.

Remaining limitation: It has not run on Windows. These are prepared tests, not passing runtime evidence. No native Windows executor is available in the current authorized environment; no emulator, service enrollment or security change was used to pretend otherwise.

### Production simulation diagnostics

Implementation and evidence: The --test path now bypasses audio initialization and runs seven deterministic engine checks for the interval, switched goal, explicit resume, correct kickoff, held restart, incident confirmation, measured statistics and full time. The same production method is executed successfully by MatchFlowRegression.

Remaining limitation: The actual Windows executable --test command still needs a Windows run. Diagnostic fixture teams do not verify source-roster accuracy or sound playback.

## Checks for this checkpoint

- MatchFlowRegression: 169 assertions, including 38 added since the 131-assertion checkpoint. These exercise actual bilingual menu navigation/actions, source-name preservation, no-mirror selection, and explicit restart/goal-side radar. Run with the real database argument below.
- Existing ContextualMechanicsHarness: 146 assertions. Its tests now explicitly dismiss held restarts and resume the added interval; CPU readiness time starts only after dismissal.
- ContextualCommentaryHarness: 65 assertions passed. ChallengerPhysicsHarness source: 19 assertions passed through a temporary .NET8 runner linked to its unchanged Program.cs. Its checked-in legacy project targets .NET9; the bare project command does not run with the available .NET8 SDK.
- Before/after evidence: the forced-goalkeeper-injury test fails against the pre-audit MatchEngine and passes with the repair. The new English-category test also fails against the prior TeamSelectionManager and passes after localization. The held-restart Space test fails against the previous engine and passes only after the held-input repair. Adding tests did not merely restate successful old behavior.
- Older database harness has stale fixed census, bench-depth and center-spot assumptions, plus real unresolved source-policy/roster failures. Its 3/13 result is not reported as passing.
- Older audio harness cannot begin without original TackleWon.wav. No audio pass or native playback pass is claimed.

```sh
dotnet run --project tests/MatchFlowRegression -c Release -- --database AccessibleFootball2026.Core/Data/players_database.json
dotnet run --project tests/ContextualMechanicsHarness -c Release
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
