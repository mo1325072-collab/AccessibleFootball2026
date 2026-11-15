# Play-engine rebuild checkpoint - 2026-11-16 00:44 Cairo

This is a separate working branch based on main2e5fa3cc. It is NOT a release, main replacement, or proof of native playability. Old implementation plus repair plan remains separately preserved on round3-match-review at afa5c6af.

## Replaced and connected

- Time-based rolling/air/bounce ball physics, atomic owner/HasBall transitions and ball-at-feet control.
- Shared swept player-body locomotion for user, CPU, carrier, support and receiver. CPU can steer around a body, never through it. Formation correction speed is bounded too.
- Shared late nearest-eligible pickup after ball/rule checks. Live shots and dead balls cannot silently become foot possession. Same eligibility for either team, receiver and keeper.
- Shared geometry/skill challenge resolver and one challenge-effect path for both teams and manual inputs. No old1.5m push teleport or separate random challenge branch. Real approach evidence survives body sweeps; active slide follows its original actor if Ctrl switches.
- Independent physical input keys/edges. Shift+arrow+Ctrl+S/D do not cancel each other; attack charge fires on release, no charge tones, possession loss including loss/regain cancels queued charge. Pause/focus reset quarantines held keys until released.
- Typed attempt ledger connected before stats/audio/rating consumers: one identity for real launch and real terminal event. Actual save/block/post/miss/goal/annulment/interruption. Annulled/interrupted attempts are not invented misses or on-target stats. Untouched goal after a provisional deflection replaces that attempt result, not a fabricated second launch.
- Concise Space holder/distance request in either half; absolute arrows; actual deliberate carried own-goal event attribution in either half. Startup/replay/pause semantics retained.
- Existing tactics, referee/VAR, period changes, substitutions, menus, teams and recordings retained. No roster or audio-asset edits.

## Verification at this checkpoint

Linux compatibility harnesses, not native Windows gameplay:
- PlayKernelHarness113 checks, SettingsLifecycle485/0, MatchFlow271, PlayerJourney392, TacticalSystem111, TacticalAcceptance89, ContextualMechanics146, ContextualCommentary65, ChallengerPhysics19.
- IntegratedPlayHarness:67/0 per seed/difficulty run, three complete unpinned production-AI matches per run. Seven earlier runs plus fresh101Hard,7Easy,42Legendary,113Legendary after movement/contact fixes. Counts are not balance proof.
- Windows app cross-build: zero errors. Native MCI/NVDA/WinForms gameplay has NOT run.

## Test migration and paired protections

Old direct-AI keeper tests now invoke full update so the shared late pickup is exercised. Source-string guards became actual keeper/foul sequences. Space removed-key expectations were superseded by the user's retained Space request. CPU receiver fixture now really owns/launches from the passer rather than stale kickoff position. Guidance-selection guards inspect guidance targets, not grace (now shared by all pickups). Carrier body-block requires physical separation and either stopping or lateral avoidance, not permanent CPU immobility.

Former1.5m pre-contact foul expectations were replaced by48 paired before/after approaches (.01/.02/.04,75/100, sprint/stand/slide, both halves). No premature sanction; same repeated input advance against a stationary shielding carrier reaches real body contact and correct caution/no-card outcome without tunnelling. These isolate ProcessUserInput so autonomous carrier turning does not turn the case into an earlier clean ball win. Existing full-update oncoming high-speed SFP cases remain.485 is semantic coverage, not an attempt to preserve arbitrary test counts.

Pickup grace remains finite1.4s anti-instant-retackle protection, inherited from old manual recoveries and CPU guided receives and made symmetric for all common pickups. Repeated checks of an owned ball cannot renew it. Explicit manual challenge is not suppressed. Native balance review must check whether this timing still feels right; no claim of player/CPU balance from automated completion.

## Remaining gates

1. Complete remote branch byte/path verification after publication; do not call partial UI publication a complete candidate.
2. Fresh full-suite/seeded verification after any later edits; inspect all statistical/audio consumers and actual assembled asset routing.
3. Actual Windows/NVDA input, focus, sound and complete-play validation. No overnight user-PC action requested.
4. Up/down is presently combined lateral locomotion plus aim while carrying. Original latest words confirm angle adjustment but do not clearly settle aim-only versus lateral+aim. Preserve this uncertainty for next awake clarification/native review, not a silent requirement change.
5. Known pre-existing database harness issues are outside this no-roster-edit round.
6. No merge into main, release tag or completion claim before integrated/native gate.
