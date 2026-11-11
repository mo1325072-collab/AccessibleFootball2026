# Context-aware Fares Awad commentary

This is a game implementation and a source accounting ledger, not a claim that every recording is automatically usable or that Windows playback has been tested.

## Installation and compatibility

The separate private audio package installs under `Assets/Audio/Commentary/FaresAwad`. Do not delete the Raouf assets. The contextual selector is enabled only when `commentary_context.json` is installed beside the Fares audio. Without that file, the existing manifest/folder path remains available. An invalid contextual catalog fails closed rather than falling back to unrelated speech.

The full library menu allows original recordings to be heard separately from automatic match selection. Browsing a held recording does not approve its automatic use. Distinct recordings remain distinct even when their words or event are similar. Only byte-identical source slots share a canonical recording.

The original source archive is retained separately. MP3 files in the playable package are decoded listening copies, not the original ATRAC bytes. `source_inventory.json` retains the original source slot, byte offset, size, channels, MD5 and duplicate relationships. `review_ledger.json` records transcription and contextual decisions, including unresolved items. No source recording is deleted by an uncertain transcript.

## Actual game changes

- Event-time immutable match context: scores, scorer ordinals, lead/equalizer/own goal, player/team identity, measured possession, pass sequence, pressure, shot flight and results.
- Header, bicycle, volley, lob and curled shots have height/range/possession checks and physical flight. H/J/V/L/F select those techniques. W remains the driven shot.
- R begins a real short, slow run-up to a stationary ball. The attempt can be intercepted. It is not allowed on indirect free kicks or corners.
- Q plays a lofted through pass. Existing through-pass and cross controls remain. Shift+N adds a ground far-post cross.
- Fouls create actual placed free-kick/penalty restarts. Indirect kicks must be passed. Corners have a stopped ready phase and a distinct take, including the CPU.
- Offside is assessed at the kick against the ball and second-last opponent, then penalized on involvement. Direct goal-kick/corner/throw exceptions, deliberate control, deflection/save continuity and disallowed goals are distinct.
- Goal attribution uses the incoming on-target trajectory for deflections; not every defender touch is an own goal. Clearances report an actual outcome, not a guessed safe destination.
- Save context distinguishes held/parried, reach/height/speed difficulty, real goalkeeper movement and diving, pressure and repeat saves. Player support velocity is measured from actual frame-to-frame movement.
- The player's in-game concentration state changes defensive reaction speed. It does not stand for an observed real-world mood or facial expression.
- The cup-final scenario has an actual trophy outcome. Extra time changes ends and has its own interval. Final-result analysis is not emitted when regulation merely continues into extra time/shootout.
- Shootout outcomes use placement, accuracy, goalkeeper dive/reach, shot speed and flight time. Slow approaches, held saves, misses, must-score and deciding kicks are separate contexts.
- Replays sonify captured ball trajectory and hold live play while replay audio is queued. They are labeled trajectory sonification, not original stadium sound or a re-simulated goal. Final winning-goal analysis is only available after a real final result.
- Recorded clips and completion callbacks are ordered. Existing Raouf backup and indexed crowd-chant logic remain. Raouf callnames are not fired while Fares is selected.

## Audio and accessibility limits

The repository includes the official NVDA 2026.3.1 x64 controller client, upstream header, license and source provenance under `ThirdParty/NVDA`. The synchronous SSML completion path is used when supported. Older working NVDA installations retain the user's NVDA voice and use an explicitly estimated compatibility wait. SAPI is only the existing fallback when NVDA itself is unavailable. No working NVDA user is silently moved to SAPI to obtain a completion signal.

Windows Forms, NVDA, SAPI and MCI playback have not been executed on Windows for this change. Build and portable regression results are not a substitute for that runtime check.

The commentary source archive does not contain the game's missing stadium-effects library. Protest speech requires a successful audible indexed whistle, not a generic fallback. Applause and coach-face assertions remain held where the actual sound/state is absent. No procedural applause is shipped as a substitute for genuine stadium response.

Some recordings contain unresolved names, short unclear speech, foreign coach voices, non-game promotional material, or a historical cause not present in an ordinary simulated match. Their individual reasons remain in the ledger. Full-library accounting and listening access do not constitute automatic integration of these held recordings.

## Portable checks

```
dotnet build -p:EnableWindowsTargeting=true
dotnet run --project tests/ContextualMechanicsHarness
dotnet run --project tests/ContextualCommentaryHarness -- <FaresAwad-package-folder> AccessibleFootball2026.Core/Data/players_database.json
dotnet run --project tests/ChallengerPhysicsHarness
```

The catalog harness checks the actual decoded files, all source-slot accounting, positive predicates and missing/conflicting-fact rejection. Those predicate checks are not proof of spoken meaning or producer reachability. The mechanics harness separately checks real restarts, physical actions, offside, attribution, replay capture, final states and audio-completion ordering.
