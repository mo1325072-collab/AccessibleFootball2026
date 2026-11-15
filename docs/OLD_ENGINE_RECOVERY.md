# Old engine fallback candidate

This branch preserves the old match implementation and its latest reviewed repair candidate. It is a fallback while a separate branch replaces movement, contact, ball control, challenges and attempt/event registration. It is not the released game and has not been verified on Windows with NVDA.

## Preserved repairs

- Keep live shots from being silently collected as loose balls.
- Drop possession-only queued actions after possession is lost, and consume actions entered during a held restart before resuming.
- Resolve attempts explicitly when the ball dies short, leaves over a touchline, is annulled or is stopped by a period whistle. Interruption is not a miss, a target result or a player-rating penalty. All four period ends are covered.
- Keep invalid or interrupted attempts out of the spoken shot total. Do not predict an on-target result from a projectile trajectory.
- Preserve the no-reserve injury acknowledgment and prevent a sent-off injury choice from blocking later choices.
- Offer radar through the pause menu, without restoring retired match keys or speaking the radar report twice.
- Make player-switch debounce use active simulation time and continue decaying after kickoff protection expires.
- Update outdated test fixtures to actual input charging, referee evidence, restart/review confirmation and second-half kickoff ownership.

No team roster, source recording or approved sound asset was changed.

## Historical controls versus the replacement target

This is a frozen fallback, not the newly requested controls implementation. The replacement target is S pass, D cross, A through pass, W shoot, Shift sprint, independent Ctrl player switching, Space for a concise holder-name and goal-distance report, and Enter for skips. Keys must work together without modifier conflicts. Direction arrows remain physical pitch directions after ends change, and deliberate carried own goals must remain possible. Attack power remains duration-based without charge-start or charge-full tones, only execution ball audio. These latest requirements are not claimed as verified by this old snapshot.

## Verification of this candidate

The Windows app and the two Windows-only test projects build with Windows targeting enabled. Native Windows Forms, sound playback and NVDA have not been exercised by these checks.

Passed runnable checks: SettingsLifecycleHarness 415; MatchFlowRegression 271; PlayerJourneyHarness 392; ContextualMechanicsHarness 146; ContextualCommentaryHarness 65; TacticalAcceptanceHarness 89; TacticalSystemHarness 111; ChallengerPhysicsHarness 19. InputSequenceRepro completes matches. An additional full-match simulation checked 24 seeded runs with 67 assertions per run. These numbers are source/simulation evidence, not proof of playability.

## Remaining work if this fallback is resumed

1. Run the actual Windows build, native-only checks and screen-reader journeys. Play full matches at more than one difficulty, including holding sprint and an arrow while switching with Ctrl and repeatedly pressing S. Check manual winning, carrying, passing, shooting, restarts, all periods, injuries and substitutions, and end-of-match statistics and audio.
2. Check the assembled distribution, not the raw source audio folder, with OriginalAssetHarness on a copy. Confirm all approved commentary, music, crowd and effects remain present and audible. Preserve the known rejected musical crowd-bed hashes; do not invent replacement recordings.
3. Resolve the nine ChallengerDatabaseHarness failures individually: incomplete roster/bench/keeper content; pack census expectations; the exclusion-rule violation; and Arabic lookup aliases. Recover the relevant source requirements before changing data. Do not invent missing players or a goalkeeper.
4. Check real AI challenge severity with actual approach motion. A standstill press is not evidence of a dangerous sprinting challenge. Do not fabricate red cards or severe injuries to meet old assertions.
5. Finish the in-game update download/install and release cycle on Windows while preserving installed sounds, settings and bandwidth. A successful build is not proof that the installer or updater completed.

## Rollback

Keep this branch independent of the replacement. Compare the complete integrated replacement and native evidence before choosing either implementation for main. This branch can be built or continued if the replacement does not meet the gameplay requirements; it should not be merged merely because its automated checks pass.
