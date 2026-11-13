# Tactical formations and ball-winning review
13 November 2026, 12:00 formation checkpoint

The later [tactical-system follow-up](TACTICAL_SYSTEM_REVIEW.md) supersedes the system-behavior and validation status below. This document preserves the research and findings of the earlier formation pass.

The catalogue now distinguishes roles and phases rather than calling familiar shapes obsolete. Three missing shapes were added: 4-1-4-1, 3-4-2-1 and 4-2-2-2. Existing valid shapes remain, with clearer names and wing-back roles. Most importantly, a reachable ball-first human tackle no longer bypasses player attributes: the same deterministic duel used by CPU challenges now decides contested possession.

## Which added shape fits which squad?

| Shape | Required roles | Benefit | Main risk |
|---|---|---|---|
| 4-1-4-1 | Screening pivot, two connecting midfielders, two hard-working wide midfielders, lone striker | Midfield coverage and recovery alongside full-backs | Isolated striker if support is late |
| 3-4-2-1 | Three centre-backs, two-way wing-backs, covering midfield pair, two tens, one striker | Width plus players between the lines | Space behind wing-backs and need for cover |
| 4-2-2-2 | Double pivot, two tens, two strikers, advancing full-backs | Central combinations and two forward outlets | Width depends on full-backs; transitions need cover |

These are options, not a ranking independent of the user's squad. The diamond is 4-1-2-1-2, not 4-1-4-1. Its two strikers and one ten are different from 4-1-4-1's one striker and four-player midfield line.

## Actual changes

1. Formation menu has an on-demand requirements and player-fit report in Arabic and English. It counts primary, supported secondary and unsupported assignments and names players outside their supported positions. The guide includes all 12 choices.
2. The tactical report now calculates the actual current formation's synergy and calls the resulting multiplier a duel-coordination multiplier rather than claiming it changes every action in the game.
3. Standing and sliding human tackles use the shared attribute duel on the ball-first path. Same-geometry tests distinguish a 95-rated from a 30-rated challenger against a 75-rated carrier. Fatigue and unsupported positions also change the actual command outcome. Body-first fouls and free-ball recovery retain their separate rules.
4. A discontinuous weak-defender compensation term was repaired. Ability sweeps now verify that a stronger defender is never made worse, and a stronger carrier never easier to dispossess, with all other inputs fixed.
5. Formation-specific targets are consumed by off-ball movement: wing-back width and recovery, narrow-shape full-back support, 4-1-4-1 support and screening. Defensive-line depth changes defender targets; pressing intensity changes the CPU engagement range. CPU teammates do not press their own carrier.
6. Existing numeric formation identities 0 through 8 are preserved. Added identities are 9 through 11, avoiding corruption of saved selections. Applying a shape preserves lineup order; it does not silently pick a different starting eleven.

## Football evidence versus game design

The role choices come from the coaching and competition sources below. Target distances (+12 for wing-back/full-back support, +14 for 4-1-4-1 wide support), line coefficient 0.24, ball-shift coefficient 0.3, screen limit 42 and pressing-range formula are game-tuning constants, not measurements published by FIFA or UEFA. Existing primary/secondary/out-of-position attribute multipliers and low-block synergy penalties are also game rules, not real-world football percentages.

The five-back choices are explicitly low-block presets. Their high-press penalty from the supplied specification is retained. This does not imply that real five-back systems cannot press. Changing this original design constraint requires a separate decision, not a false research claim.

## Verification

- TacticalAcceptanceHarness: 89 checks, including each added shape completing a full match through both attacking directions.
- PlayerJourneyHarness: 313 checks, including 60 complete matches and per-tick finite-coordinate, legal-owner and possession-flag invariants.
- MatchFlowRegression with the production database: 193 assertions, including real-team startup and 100 seeded drafts.
- ContextualMechanicsHarness: 146; ContextualCommentaryHarness: 65; SettingsLifecycleHarness: 66; ChallengerPhysicsHarness: 19.
- Windows-targeted test/app cross-build: zero warnings and zero errors. This is not a Windows runtime or audio acceptance test.

A separate old ChallengerDatabaseHarness still reports 3 passing and 10 failing groups. The same 10 groups fail in a reconstructed pre-tactical baseline. They mix obsolete fixed team/tournament counts with source-roster gaps and old naming/geometry assumptions. They were not hidden or weakened to claim a pass. Production roster data and exclusion decisions were not altered by this work. Seven incomplete source teams remain a previously identified data limitation.

## Delivery state and limits

All these changes are local and UNPUBLISHED. They are cumulative with the preserved overnight fixes against published commit 7008d0fcfd6ee3807aed70f651ab2f6c23bfc9a4. No new GitHub commit or downloadable build is claimed. Existing publication failures have not been retried without a new workable route.

Native Windows UI, NVDA and real MCI/audio output remain untested here. The new failed-tackle event reaches the existing standing/sliding effect route, but actual Windows sound output has not been heard. No authentic missing sound assets or source player data were invented. No full-game-readiness claim follows from these checks.

## Sources

Sources were read as full fetched text, not only search excerpts. This review is about the functions of shapes, not a popularity ranking or a promise that any formation is newly invented.

| Source | Publisher and type | Visible date | Claims used | Confidence |
|---|---|---|---|---|
| [Formation overview](https://learning.coachesvoice.com/cv/formations-football-tactics-explained-best-most-used/) | Coaches Voice, specialist coaching explainer | 2 Dec 2021 | Shapes have different midfield and width demands; no universal best shape | High for roles |
| [Diamond](https://learning.coachesvoice.com/cv/4-4-2-diamond-football-tactics-explained-klopp-potter-allegri/) | Coaches Voice, coaching explainer | 11 Nov 2022 | Single pivot, two eights, ten, two strikers; full-back width; wide-transition risk | High |
| [4-1-4-1](https://learning.coachesvoice.com/cv/4-1-4-1-formation-football-tactics-explained/) | Coaches Voice, coaching explainer | 28 Jun 2024 | Four midfielders ahead of a screen, wide defensive work, isolated-striker risk | High |
| [3-4-3](https://learning.coachesvoice.com/cv/3-4-3-formation-football-tactics-explained-tuchel-conte-pochettino-southgate/) | Coaches Voice, coaching explainer | 20 Jan 2022 | Wing-backs provide width and recovery; two-tens variant differs from wide forwards | High |
| [Salzburg 4-2-2-2](https://www.fifatrainingcentre.com/en/game/tournaments/fcwc/2025/team-analyses/fc-salzburg-4-2-2-2-build-up-and-sustaining-the-counter-press.php) | FIFA Training Centre, primary tournament analysis | 2025 tournament | Double pivot, two tens, two strikers, full-back width, counter-press cover | High |
| [4-3-3 v 5-3-2 session](https://www.fifatrainingcentre.com/en/practice/elite-sessions/in-possession/attacking-in-a-4-3-3-formation-v-5-3-2-defensive-set-up.php) | FIFA Training Centre, primary coaching session | 2022 | Central progression, third-player support, risk when wing-backs step to press | High |
| [Real Betis analysis](https://www.uefa.com/uefaconferenceleague/news/0299-1db751949210-058136238cb2-1000--uefa-conference-league-tactical-analysis-how-real-betis-/) | UEFA, primary competition technical analysis | 2025 competition | In-possession and defensive shapes can differ; adjust to opponent | High |
| [Finding space between lines](https://www.uefatechnicalreports.com/programme/uyl-2025/technical-topics/finding-space-between-the-lines/page/1) | UEFA, primary technical report | 2025 edition; fetched metadata gives conflicting 2024 date | Triangle roles, coordinated rotations and between-line receiving | High for text; publication day not relied on |
| [Every modern formation](https://www.fourfourtwo.com/features/formations-in-football) | FourFourTwo, independent editorial guide | Page says 19 Feb 2025, body includes 2026 wording | 4-1-4-1 / 3-4-2-1 / 4-2-2-2 tradeoffs, valid uses of older shapes | Medium-high, role claims cross-checked against FIFA and coaching sources |

A fresh 2026-targeted search was made. Search-only 2026 news results were not used to rank formations or change player data. Research does not justify deleting 4-4-2, the diamond or 4-3-3 just because their origins are older.
