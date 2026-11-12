# Requirements and implementation audit

Checked 2026-11-12 against ORIGINAL_REQUEST.md, PROJECT.md, Tactical_Design_Specs.md and the later user corrections. This is an open work record, not a whole-game approval.

Later corrections win over older text: S passes and D crosses; Fares Awad is the selected commentary source; Enter releases incidents only after narration; recorded commentary precedes latest-only reader updates; only goals and penalties interrupt commentary. Optional automatic substitutions use fatigue or repeated-foul booking risk and the strongest exact-position replacement before a supported secondary position. Preserve source rosters, all reviewed commentary and user audio exclusions.

## Tested source behavior

| Requirement | Implementation and evidence | Remaining limitation |
| --- | --- | --- |
| Edge-triggered controls and continuous movement | KeyboardInputHandler and MatchFlowRegression exercise press/hold/release, Ctrl variants and reset | Native keyboard layout, NVDA focus and real WinForms handling not executed on Windows |
| Pass reception, physical shots and defending | Existing physics harness 19/19; mechanics harness 146/146, with explicit incident dismissal and pre-extra-time resume | Windows interaction not verified |
| Two halves and switched goals | PeriodTransitions and regression check kickoffs, directions, intervals and extra time | Native menu interaction unverified |
| Measured statistics | 25 comparative rows, navigation and producer tests | Visual/native reader interaction unverified |
| Substitutions and opportunities | 5/3, sixth/fourth in extra time, interval-free opportunities, no return after substitution or sent-off vacancy | Seven source roster gaps remain separate |
| Severe injuries and forced replacement | Reckless sliding fouls from behind can produce severe injury; compatible replacement selected; unreplaced player leaves; actual restart and taker survive | Injury tuning is a deterministic game rule, not a medical prediction |
| Temporary-period state | Run-up, advantage and pending clearance cleared before new-period kickoff | Further whole-game scenario review remains open |
| Tactical zero-dice design | Production tackle-duel result is deterministic from attributes, position, pressure and tactics; optional legacy forced rolls are test fixtures only | Other physical spread/shot random variation is separate from the duel rule |
| Crowd proximity | Follows the actual attacking team's current goal, including second-half reversal | Original effects transfer and Windows playback blocked |
| Radar and concise live speech | Both radar keys reach the engine query; semantic switch/missed-tackle tags replace unsafe substring detection; no tutorial in restart or replay announcements | Native audible behavior still unverified |
| National teams by confederation | All 211 source teams partition exactly once into six categories; memberships checked against official FIFA lists | No source roster, inclusion policy or team name changed |
| Tournament matchup choice | User chooses both participating teams; no longer silently starts first two entries | Full bracket/season progression is not implemented |
| Settings | Legendary difficulty preserved instead of downgraded to Hard | Full English UI coverage still missing; many menus remain Arabic |
| Optional advisory CLI | Portable Models lookup, safe argument boundaries, concurrent stderr/stdout reads, failed-exit rejection and process-tree timeout; query moved off UI thread | Tests use fake process fixtures, not actual model inference. No model/runtime downloaded |
| Referee/VAR truthfulness | Active offside and restarts use native geometry/rules; unused name-only VAR preview no longer fabricates random centimeters or decisions | Qwen is not the match referee. Installing/choosing a model is a separate unresolved task |
| Build and packaging | Release cross-build; actual DB external to single-file EXE; real native controller recovered | Current target is .NET8, unlike the old .NET9 request. Desktop deployment, native Windows/NVDA/SAPI/MCI execution and complete asset assembly are not verified |

## Checks for this checkpoint

- MatchFlowRegression: 131 assertions, including 29 added since the 102-assertion checkpoint. Run with the real database argument below.
- Existing ContextualMechanicsHarness: 146 assertions. Its tests now explicitly dismiss held restarts and resume the added interval; CPU readiness time starts only after dismissal.
- ContextualCommentaryHarness: 65 assertions passed. FootballPhysicsTestHarness: 19 assertions passed, rerun against these source changes.
- Before/after evidence: the new forced-goalkeeper-injury test fails against the pre-audit MatchEngine and passes with the repair. Adding tests did not merely restate successful old behavior.
- Older database harness has stale fixed census, bench-depth and center-spot assumptions, plus real unresolved source-policy/roster failures. Its 3/13 result is not reported as passing.
- Older audio harness cannot begin without original TackleWon.wav. No audio pass or native playback pass is claimed.

```sh
dotnet run --project tests/MatchFlowRegression -c Release -- --database AccessibleFootball2026.Core/Data/players_database.json
dotnet run --project tests/ContextualMechanicsHarness -c Release
dotnet build -c Release -p:EnableWindowsTargeting=true
```

## Open implementation work, not user testing homework

- Finish English menu/announcement coverage and native accessible settings verification.
- Full tournament/season progression remains absent. A matchup browser is not a tournament engine.
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
