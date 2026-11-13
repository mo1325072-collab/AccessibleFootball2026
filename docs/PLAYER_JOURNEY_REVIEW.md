# Player journey review - 13 November 2026

This is an ongoing acceptance-path review, not a release certificate. It follows the original project requests and later corrections through menus, action handlers and actual consumers. Passing a build or finding a method does not prove the journey works.

## New reproduced defects and repairs

### Four-club selection and redraw

Back/reselect now replaces the numbered source-club choice and removes downstream choices. It cannot silently append beyond four clubs, skip a required step, or announce a failed Add as success. Opposing squads may still use the same source club; selected player IDs remain globally unique.

A draft pool can contain duplicate records for a player. Selection now checks IDs at each actual pick, not only when it first snapshots the pool. A redraw clears old squad-role references and match counters before assigning the new captain and set-piece takers.

Tests exercise normal choices, duplicate rejection, out-of-order actions, changing an earlier choice after completing both sides, and redraw. The production-database regression also runs 100 seeded drafts.

### Lineup and specialist menus

Sent-off/off-pitch players no longer appear as legal swap targets or captain/set-piece candidates. Actions captured before a dismissal revalidate when selected. A failed swap cannot announce success. Source primary positions are not rewritten.

### Formation choice to field and AI

3-4-3 and diamond previously fell through to 4-3-3 coordinates. They now have their own field slots. Deliberate formation changes assign matching positions used by player suitability and the AI. Starting a match or swapping ends does not overwrite a subsequent individual position edit.

Tests use the real formation menu, check field slots and assigned positions, preserve source primary positions, then construct a match and cross halftime.

### Ready restarts

Radar and lineup requests now work while aiming a ready free kick/corner/penalty and while waiting for an opponent restart. Pause works there too. These checks cover the state after the separate held-incident confirmation, rather than only the initial replay hold.

Invalid/nonpositive frame durations cannot poison the clock and ball state with NaN or run the simulation backward.

### Spatial effects and live settings

Six kick/tackle paths now apply left/right channel volumes before actual playback, including extracted samples. Changing master/category volume retains their pan. A failed spatial setup does not play an incorrectly centered effect. Centered fallback recordings avoid double-applying a pre-panned synthetic sample.

These are production playback-command tests using an injected transport. They do not establish what a Windows driver or headphones actually sound like.

### Pause Back and sound academy lifecycle

Returning from a nested pause menu no longer requests main-menu music and stops match crowd. A native window test exercises Escape into pause, Back from a child and resume. It cross-builds but has not run on Windows here.

Reachable academy previews replace previous playback and close on Back. All five documented categories are restored after recovering the original academy scope and checking for later exclusions. Previews explain the selected sound. Missing recordings, mute and playback failures are reported instead of silently substituting a different effect. Match-impact previews do not use generated impact fallbacks; the intentional earcons and goal beacon still demonstrate their actual gameplay signals. The goalkeeper-save preview no longer plays a shot kick. Commentary clips and exclusions are unchanged. Detailed mode explanations remain reachable in the guide. Stadium/statistics titles no longer contain test prose or keyboard tutorials.

### Full-match finite-state failure

Mixed-input whole-match tests exposed a zero-vector normalization in CPU pressing: a defender already at the cached ball position acquired NaN coordinates. Later physics could pass the corruption to the ball. A direct colocated-player fixture reproduces it without relying on random play.

Pressing, exact receiver arrival, a cross from its own center target and a CPU carrier at goal center now use safe zero-distance handling. The added deterministic boundary tests are separate from the complete-match runs.

Sixty full matches now reach full time, with checks after every tick for finite player/ball coordinates, a legal active ball owner and consistent possession flags. Runs cover 0.02, 0.1 and 0.25 second frame durations and all four difficulty settings. Inputs are seeded; internal match randomness is not claimed deterministic. These are engine journeys, not a Windows UI/audio test.

### Replacement and dismissal while an action is pending

Pausing between a pass and arrival, replacing a restart taker, or replacing a shooter while the ball is still moving previously left stale player references. Reproduced failures included bench players receiving the ball, a dismissed restart taker staying assigned, wrong-team boundary/goalkeeper decisions, lost own-goal/assist attribution and a pending run-up restoring possession to a substituted player.

Live receiving duties move to the incoming player. Historical touches keep the original player's team identity. Removed restart takers are reassigned to legal active teammates, and a substituted player's pending run-up is cancelled. Coach praise uses the same historical scoring/assist identities. The journey harness checks each transition directly, in addition to the full-match runs.

### Terminal handoff and standalone plan semantics

Game-over key routing now rejects field/pause commands during final commentary, like the existing shootout-resolution guard. The native regression sends Escape, Enter and a field key and checks that no pause menu or timer restart appears. This regression compiles but has not been executed on Windows. Two older native cleanup calls also now supply the actual end-match boolean parameter instead of throwing a reflection parameter-count exception before their lifecycle assertions can finish.

The standalone game-plan flow no longer announces a match start followed by a fictional disk save. Its primary item applies the existing in-memory team choices for the current session. Real pre-match menus still say Start match. Both languages are exercised through the actual menu callback. No durable saved-plan format is invented.

### Abandonment and shootout eligibility

A reproduced five-dismissal sequence left a six-player team playing normally. Law 3's minimum now stops match play with a reason and score at stoppage. It never creates an administrative score, winner, trophy, final-match analysis or winning-goal replay. A pending legal injury replacement remains the user's choice; a sixth-player continuation instead stops the match. Seven still permits play. Post-match shootouts do not use the seven-player cutoff. A severe foul that causes abandonment cannot reopen a pending injury choice or restart later in the same call.

Shootouts snapshot eligible active players separately from the on-pitch squad. The larger side reduces to the opponent's eligible count. The human team selects named exclusions in a menu, keeping its goalkeeper; the CPU chooses its exclusions. Exclusion does not count as a substitution, and all eligible players take one kick before anyone repeats. Menu callback, stale-selection, equal-count, sent-off-player and full-cycle tests exercise the actual shared production code. Full native shootout navigation is still not executed.

Rules checked against IFAB Law 3 and Law 10 on 2026-11-13:
- https://www.theifab.com/laws/latest/the-players/
- https://theifab.com/laws/latest/determining-the-outcome-of-a-match/

## Validation commands

Run the complete regression with the production database, not only its default fixture branch:

```sh
dotnet run --project tests/MatchFlowRegression -c Release -- --database /path/to/players_database.json
dotnet run --project tests/SettingsLifecycleHarness -c Release
dotnet run --project tests/PlayerJourneyHarness -c Release
dotnet run --project tests/ContextualMechanicsHarness -c Release
dotnet run --project tests/ContextualCommentaryHarness -c Release
```

The regression has 178 assertions without `--database`; the supplied production database enables 15 additional checks, for 193. This is not removal of previous coverage. Injury-choice, held-restart, statistics, period, substitution, input and settings checks remain present.

Current local results: regression 193, settings lifecycle 66, journey 313, contextual mechanics 146, contextual commentary 65 passed. Native tests are cross-build evidence only. Final checkpoint readback must be tested again before these results are attributed to a published revision.

## Remaining review and verification

The review is still open for match-end/new-match async callbacks, complete shootout navigation, durable saved-game-plan requirements, and further menu-to-consumer paths. Existing source-roster gaps, missing original PES effects, model choice and target-framework/support decisions are not solved by these repairs. No players, recordings or runtime models were invented or downloaded.

Windows Forms focus, NVDA interaction, real audio playback and visual layout remain unexecuted in this environment. There is no public release, upload of a game build, desktop deployment or claim that the whole game is ready.
