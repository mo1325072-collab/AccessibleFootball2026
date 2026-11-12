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

Reachable academy previews replace previous playback and close on Back. Existing academy category visibility is preserved while its history is checked. Detailed mode explanations remain reachable in the guide. Stadium/statistics titles no longer contain test prose or keyboard tutorials.

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

Current local results: regression 193, settings lifecycle 66, journey 81, contextual mechanics 146, contextual commentary 65 passed. Native tests are cross-build evidence only. Final checkpoint readback must be tested again before these results are attributed to a published revision.

## Remaining review and verification

The review is still open for match-end/new-match async callbacks, complete shootout navigation, standalone saved-game-plan behavior, and further menu-to-consumer paths. Existing source-roster gaps, missing original PES effects, model choice and target-framework/support decisions are not solved by these repairs. No players, recordings or runtime models were invented or downloaded.

Windows Forms focus, NVDA interaction, real audio playback and visual layout remain unexecuted in this environment. There is no public release, upload of a game build, desktop deployment or claim that the whole game is ready.
