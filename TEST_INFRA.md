# Testing AccessibleFootball2026

## Test entry points

Run from the repository root with the .NET 8 SDK.

### Portable match-flow regression

```sh
dotnet run --project tests/MatchFlowRegression -c Release
```

To include the real source database audit:

```sh
dotnet run --project tests/MatchFlowRegression -c Release -- --database AccessibleFootball2026.Core/Data/players_database.json
```

The suite throws and exits nonzero on a failed assertion. The November 12, 2026 source-repair checkpoint passed 100 assertions with the database option. This is a bounded test result, not a whole-game readiness rating.

Covered behavior includes:

- Measured per-team statistics and keyboard navigation of the statistics menu.
- Regulation halves, switched ends, formation preservation, interval actions and extra-time boundaries.
- Kickoff placement, held restarts and explicit dismissal, actual restart team/taker state, and pause/resume state preservation.
- Latest-only pending reader updates, waiting for commentary, and goal/penalty-only urgent interruption signals using test readers.
- Five substitutions/three opportunities and the configured sixth/extra opportunity in extra time, interval exemptions, no re-entry and no sent-off replacement.
- Optional native auto-substitution and tactics, fatigue/card risk, strongest compatible position selection and goalkeeper inclusion.
- Input edges, held-key repeat suppression, movement/sprint and reset.
- Settings save/load through a temporary file.
- JSON player identity, draft completeness and no duplicate draft selections.
- Complete-match simulation smoke paths and finite positions.
- Actual database audit: 522 usable teams start and update; 100 seeded drafts create complete starting elevens.

The database option reads the supplied JSON. It does not certify real-world roster accuracy or recover missing roster data.

### Windows UI regression

On Windows 10/11 x64:

```sh
dotnet run --project AccessibleFootball2026.Tests -c Release
```

This project targets the same .NET 8 Windows framework as the app. It checks WinForms key suppression and focus behavior. A cross-build is not a run of these tests. The November 12 source-repair session cross-built the project but did not run it on Windows.

### Game diagnostic option

`AccessibleFootball2026.exe --test` runs roster/draft diagnostics. It is not the portable regression suite and does not prove sound playback, NVDA completion or a successful desktop deployment. It returns a failing status if its draft is incomplete or contains duplicate player IDs.

## Known validation gaps

- Native WinForms interaction, NVDA synchronous/older-API completion, SAPI fallback, MCI playback and actual audio overlap still require Windows execution.
- The recorded commentary catalog, audio assets, menu music and startup anthem must be present in a real release. Source compilation does not verify their presence, audibility or licensing. These repairs did not change the commentary catalog, mappings or audio assets.
- The current 529-team source JSON has seven records blocked by preflight: FC Aiglons (Tchad), Medina United FA, AS Port-Louis 2000, Panthere Sportive du Nde, AS ZAM, Uniao Bissau and DC Virunga. Six have fewer than seven players; Medina United FA has no assigned goalkeeper. The game explains the issue instead of fabricating players. Other squads with 7-10 players can start if they have a keeper.
- The JSON has no global player identifier. Normalized exact name plus age is used to share an identity across rows and avoid unrelated squad-row collisions. Real homonyms or spelling/age discrepancies need source-data review.
- This pass did not implement a new model runtime, download or model distribution policy. Native automatic assistance and optional model-backed reports remain distinct.

## Release validation still required

1. Build and publish a Windows x64 release with the actual native controller DLL, database and intended audio assets.
2. Run the Windows UI suite and play through startup, settings, team selection, pre-match plan, both halves, substitutions, extra time, shootout and return to menu.
3. Listen with NVDA enabled and disabled. Check that commentary finishes before ordinary reader updates, goals/penalties preempt as intended, and held incidents do not restart before Enter.
4. Confirm all required assets and mappings are present and that music/anthem playback matches the chosen release.

Do not replace these checks with prefilled success messages or a claimed fixed number of tests from an older README.
