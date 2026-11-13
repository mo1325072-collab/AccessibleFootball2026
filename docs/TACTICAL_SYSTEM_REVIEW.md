# Tactical system: benefits, costs and time
13 November 2026, follow-up to the formation review

## What was missing

The earlier formation pass did not prove that all tactical instructions changed the match. The human tackle fix, position penalties and duel coordination were real, but the CPU carrier still ran at goal, shot inside 24 metres and only attempted a random pass when the human was close. Most attacking instructions did not enter that decision. The offside toggle did not move defenders. Nominal age multipliers did not produce the requested minute-by-minute fatigue windows.

This pass replaces those missing behavior paths and adds separate tests. It does not promise that any tactic always creates more chances or wins a match.

## How coordination actually works

Coordination starts at 100. Conflicts deduct points. The result is clamped to 10-100 and converted to `clamp(score / 100 * 1.25, 0.40, 1.25)`. Positive descriptions identify compatible instructions; they do not add points above 100. Two conflict-free teams do not receive different hidden chance-creation percentages merely because their descriptions differ.

For a duel:

- Tackler: 50% effective defending + 30% physical + 20% speed.
- Carrier: 45% effective dribbling + 35% physical + 20% speed.
- Position fit already affects those effective attributes. Stamina factor is clamped to 0.60-1.00; coordination then scales the strength. The existing weaker-man-marker cap is retained.
- Shielding, facing, ball proximity, actual nearby teammates, commitment, a real short build-up near the box, and an active counter transition modify the score.
- The final advantage score is `42 + 0.55 * strength difference + situation modifiers + difficulty modifier`, clamped to 5-92. Production wins at 50 or above without rolling dice. The old field name `TacklerWinProbability` is a compatibility name, not a random chance in production.
- Body-first geometry can prevent a clean win or produce a foul. CPU and human calculations now use the same feet projection when the ball is stored at its carrier's centre.

## Instruction-to-behavior matrix

| Instruction | Actual benefit path | Actual cost or limitation | Acceptance evidence |
|---|---|---|---|
| Low / mid / active / high press | Pursuer count grows 1 / 2 / 3 / 4; range grows with the slider; high pressing also advances support. User teammates support the press without taking over the active human's commands. | Low block gives up territory and early recoveries; high press costs age-dependent energy and exposes the advanced line. Fatigue reduces chase, support, carry and receiving speed. | Same controlled six-second build-up: low press 0 recoveries / 0 shots; high press 1 recovery / 1 shot. This is a paired scenario, not a universal season average. |
| Defensive line depth | Actual defender targets advance or retreat, and the pivot screens ahead of the line. | Advancing leaves real space behind; high press with a manually deep line separates the units and also loses duel coordination. | Mirrored depth tests, actual trap-line positions and combined full matches. |
| Passing directness | CPU outlet selection scores distance, progress, freedom and blocked lanes. Direct passing lifts the ball and favours a farther outlet. | Short lanes can be intercepted under pressure; lifted passes need a real reception and can be contested. Offside receivers carry a selection penalty, not immunity. | Same players: short chooses the nearby outlet; long chooses the farther forward. Ball height differs in the executed kick. |
| Tempo | Changes actual CPU decision delay, pass release speed and off-ball support movement. | Slow play lets opponents recover; faster tempo costs energy. At the highest pressing load, the age-window schedule already represents sustained heavy work rather than stacking a second unbounded fatigue multiplier. | Fast tempo releases a pass before slow tempo in the same time window; ordinary-load energy cost is measured. |
| Zonal / man-to-man | Zonal holds shape and lanes; man-marking assigns opponents and physically follows them. The existing skill-gap duel effects remain. | A marked opponent can pull a defender out of a lane. Zonal does not automatically cover every free opponent. | Actual man-marker follows a wide bait more closely and vacates the original lane more than the zonal defender. |
| Tackling commitment | Cautious / normal / moderate / aggressive / reckless apply the specified ordered challenge bonuses. | Unsafe rear/body-first attempts cost fouls, cards and injuries. Automated caution contains rather than winning through the carrier's body. A human manual body-first tackle can override cautious team instructions and is still refereed. | All five commitment scores; real cautious CPU retains no unfair win or card; real reckless rear challenge yields foul, red and severe-injury choice. |
| Offside trap | Ready defenders and recovering wing-backs prepare an actual coordinated line. Tired or unfocused players do not make the same step. | One lagging player can keep the runner onside. There is no instant teleport or retrospective change to the line after a kick. | Actual second-last-opponent line: 55.69 ready, 60.90 with a tired defender. The same runner is offside only against the ready line. A real touch is required before the indirect restart. |
| Balanced focus | CPU chooses an available outlet without forcing wide or central preference. | Does not guarantee an overload in either area. | Executed pass, combined full-match cases. |
| Wings focus | Wide support and real wide receivers; a CPU carrier on a wing in range can cross. | Some central outlets are farther away; wide transitions need recovery. | Wide versus central receiver choice and existing wide-discipline movement tests. |
| Central focus | Narrows support and favours central receivers. | Gives up width and can encounter a crowded central lane. | Paired central versus wide outlet, real lane penalties and combined matches. |
| Long-ball focus | Favours an advancing forward outlet and a lifted direct ball. | Longer flight, contested reception and offside remain real costs. | Actual receiver and flight checks; no possession or completion is awarded by choosing the tactic. |
| Shoot-on-sight focus | CPU may shoot from 31 metres with space instead of waiting for the ordinary 24-metre range. | Longer distance, poorer shooting and fatigue widen the actual target spread. No free accuracy or goal is added. | Executed long-range shot; identical shot samples produce wider errors from farther out or with weaker shooting. |
| Counter-attack focus | After a real regain, support and forward outlet preference gain a six-second transition window; the duel transition modifier is also bounded to that window. | The benefit expires, does not exist at the initial kickoff and is cleared at a new period. Slow/short counter instructions lose coordination. | Actual possession changes, burst expiry, the exact transition modifier and halftime reset tests. |

All distances, intervals, scoring weights and threshold values in this table are game tuning. They are not claimed as measured FIFA or medical constants. Formation role choices and their football tradeoffs remain sourced in [the formation review](TACTICAL_FORMATION_REVIEW.md).

## Age and minute requirements

The owner's original document overlaps age 24. The owner explicitly confirmed that 24 belongs with the younger group. Boundaries are now under 19, 19-24, 25-29 and 30+.

The high-press drain is calibrated to the requested timing, replacing reliance on the old `28 / 90` base multiplied by nominal factors that did not meet those timings. The old coefficient ranges alone are not claimed to establish the new windows. The rate uses actual simulated minutes spent under the selected instructions, not the selected real-world match duration.

A controlled maintained-high-press timeline, including the existing +10 halftime recovery, measured:

| Age | Match minute | Energy remaining |
|---|---:|---:|
| 18 | 90 | 82.00 |
| 20 | 79 | 42.15 |
| 20 | 90 | 32.70 |
| 27 | 60 | 25.00 |
| 32 | 45 | 16.13 |

These rows use maximum pressure and tempo 50. Additional tests use tempo 0 and 100, age 24 and the age-25 boundary. Under-19 players retain ordinary base drain but avoid the additional high-press burden; "lasts 90 minutes" does not mean the energy display is frozen at 100. Below 40 is the game's movement/concentration fatigue threshold; 25 is its exhaustion threshold, not a medical definition or a forced stop. The minimum movement multiplier remains 0.55 so exhausted players do not freeze.

The windows assume sustained instructions and the normal interval recovery. Lowering pressure changes future drain without refunding spent energy. Substitutes enter with their own energy. Automatic substitutions or adaptive tactics can change the timeline by changing who is playing or the load itself. Goalkeepers retain a separate low drain.

## Controls, state and accessibility

The four controls now offer every integer from 0 through 100 rather than four preset jumps. Pressure can still link the line automatically, and directness can link tempo. Independent adjustments remain independent until the user explicitly relinks them. Copying a team preserves both values and whether the links were overridden. The line and tempo displays update when their linked controls change. Marking, commitment, offside and focus labels also update in the open menu.

Initial default formation setup now assigns the actual pivot and full-back roles. It does not change source primary positions, names, player IDs or roster membership. Manual assignments remain preserved when a match starts. The initial role assignment is not an automatic best-eleven optimizer.

The on-demand tactics report explains the selected benefits/costs and coordination limits. A new guide category explains coordination, duels, energy windows and instruction tradeoffs in Arabic and English. Live play does not gain unsolicited long tutorials.

## Verification and boundaries

- TacticalSystemHarness: 101 checks, including 12 full matches combining all six focuses with low/high opposing press, different shapes, both halves, offside and marking settings.
- Existing TacticalAcceptanceHarness: 89 checks, including three complete new-shape matches.
- PlayerJourneyHarness: 313 checks, including 60 complete matches and per-tick finite state / legal possession checks.
- Production-database MatchFlowRegression: 196 assertions. The count grew from 193 because the accessible tactics menu now has additional controls.
- ContextualMechanics: 146; ContextualCommentary: 65; SettingsLifecycle: 66; physics: 19.
- Windows-targeted app/test cross-build: zero warnings and zero errors.

These checks establish specific causal paths and controlled examples. They are not statistical balance testing across every roster, every tactic combination and every difficulty. They do not prove a higher average xG, guarantee a one-on-one or goal after every broken trap, or establish full-game readiness.

Windows UI, NVDA speech and real MCI/audio output have not been executed here. The recovery base is the previously verified published commit `7008d0fcfd6ee3807aed70f651ab2f6c23bfc9a4`; this pass and the preserved overnight/formation changes have not been pushed by this work and are still local. Recheck the current remote HEAD before publication. No new commit, downloadable build or successful publication is claimed. Existing source-roster gaps and the old fixed-census harness limitations are not repaired by this tactical pass. The separate legacy database harness still fails the same ten test groups. Its rating-range group now first reports an effective rating of 39 after the real assigned-position penalty, instead of the earlier age example; this is not a change to the source player rating. The underlying roster JSON hash is unchanged. No source roster, approved commentary asset or exclusion was changed to make a test pass.
