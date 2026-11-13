# Requirements and implementation audit

Checked 2026-11-12 against ORIGINAL_REQUEST.md, PROJECT.md, Tactical_Design_Specs.md and the later user corrections. This is an open work record, not a whole-game approval.

Later corrections win over older text: S passes and D crosses; Fares Awad is the selected commentary source; Enter releases incidents only after narration; recorded commentary precedes latest-only reader updates; only goals and penalties interrupt commentary. Optional automatic substitutions use fatigue or repeated-foul booking risk and the strongest exact-position replacement before a supported secondary position. Severe injuries to the human team are different: name the injured player and let the user choose from the bench. The earlier compatible-automatic injury repair missed this intended choice; this checkpoint corrects that miss, not a newly invented feature. Preserve source rosters, all reviewed commentary and user audio exclusions.

## Tested source behavior

### Edge-triggered controls and continuous movement

Implementation and evidence: KeyboardInputHandler and MatchFlowRegression exercise press/hold/release, Ctrl variants and reset

Remaining limitation: Native keyboard layout, NVDA focus and real WinForms handling not executed on Windows

### Pass reception, physical shots and defending

Implementation and evidence: Checked-in .NET9 physics harness 19/19, run directly with an isolated SDK9; mechanics harness 146/146, with explicit incident dismissal and pre-extra-time resume

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

Implementation and evidence: Actual reckless sliding fouls can produce severe injury. The human team now enters a pending choice, freezing clock, ball and optional auto-assistance. A modal injury menu names the player and lists legal bench entries with position/rating. Arrows and Enter choose the replacement; Escape cannot silently resume. The user may continue short; exhausted substitutions remove the player without inventing a replacement. The penalty/free-kick restart, taker and its separate Enter confirmation survive. Opponent injury choices remain computer-managed. Arabic/English engine-to-menu-to-user-choice tests cover the weaker chosen reserve winning over an unchosen stronger reserve.

Remaining limitation: Native window focus and audible screen-reader interaction are prepared but not executed on Windows. Injury tuning is a deterministic game rule, not a medical prediction.

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

Settings lifecycle repair: Save now writes through a same-directory temporary file and reports failure rather than swallowing it; the native form stays open and does not announce success on failure. An explicit save stamps the migrated version so a first saved Off choice is not overwritten. Legacy Off remains Off, and migration writes back to the requested path. Closing without Save restores previewed values and live consumers.

SettingsLifecycleHarness checks every stored property after save/reload and in a fresh process, then exercises production playback commands through a recording transport instead of a native device. It covers zero master/category volumes, stadium volume before first play, chants using crowd rather than SFX, stopping active channels on mute, single rather than squared crowd scaling, no crowd/menu-music starts from unrelated settings changes, requested menu-song unmute, gameplay beacon scaling, anthem scaling, commentary Off/current voice changes, duration/difficulty, extra time/penalties, optional auto-sub/tactics and native close-offside review. The legacy spoken-events flag now controls automatic announcements without disabling explicit radar. Existing live-match silence remains in force.

Remaining limitation: No native MCI device or Windows audio/reader/focus behavior was executed. These tests verify production commands and behavior, not that a speaker produced sound. English menu/action paths are exercised by the source harness; original Arabic names and recording transcripts remain source content.

### Optional advisory CLI

Implementation and evidence: Portable Models lookup, safe argument boundaries, concurrent stderr/stdout reads, failed-exit rejection and process-tree timeout; query moved off UI thread

Remaining limitation: Tests use fake process fixtures, not actual model inference. No model/runtime downloaded

### Referee/VAR truthfulness

Implementation and evidence: Active offside and restarts use native geometry/rules; unused name-only VAR preview no longer fabricates random centimeters or decisions

Remaining limitation: Qwen is not the match referee. Installing/choosing a model is a separate unresolved task

### Build and packaging

Implementation and evidence: Release cross-build; actual DB external to single-file EXE; real native controller recovered

Remaining limitation: Current production target is .NET8; ORIGINAL_REQUEST/PROJECT say .NET9, while the current README specifies .NET8 or higher. The discrepancy is preserved, not declared mandatory or revoked. SDK9 is locally available and a separate scratch retarget/build/test/publish succeeded, so SDK availability is not a blocker and production targets were not silently changed. Desktop deployment, native Windows/NVDA/SAPI/MCI execution and complete asset assembly are not verified.


### Team-selection callback routing

Implementation and evidence: Per-call navigation callbacks now reach the six-confederation submenu instead of silently falling back to a flat national-team list. Tests navigate the actual categories, confederations, teams and opponent exclusion.

Remaining limitation: Native keyboard and reader interaction still needs a Windows run.

### Explicit tactical radar

Implementation and evidence: Radar uses the selected UI language, identifies the current restart/taker, and measures geometric distance to the current attacking goal, including the switched second-half goal. Four event-output checks cover these paths. Four further keyboard-to-engine checks prove Space and explicit lineup requests remain available while an incident is held, without moving the ball/clock or firing a shot queued during narration.

Remaining limitation: Recorded Arabic commentary remains Arabic; no translated recordings are claimed.

### Native verification preparation

Implementation and evidence: The Windows-only test suite now uses the current S=pass/D=cross/Q=lofted-through-ball controls, supplies current optional match-start parameters, isolates menu key-routing tests from destructive selections, and invokes the real game timer instead of duplicating obsolete telemetry behavior. New native settings tests cover both language directions, accessible names, unsaved selections, all slider/checkbox saves and reloads, cancellation, and failed-save feedback. Native injury tests invoke the real timer, Escape guard, Down/Enter selection and focus restoration. The suite cross-builds with zero errors.

Remaining limitation: It has not run on Windows. These are prepared tests, not passing runtime evidence. No native Windows executor is available in the current authorized environment; no emulator, service enrollment or security change was used to pretend otherwise.

### Production simulation diagnostics

Implementation and evidence: The --test path now bypasses audio initialization and runs seven deterministic engine checks for the interval, switched goal, explicit resume, correct kickoff, held restart, incident confirmation, measured statistics and full time. The same production method is executed successfully by MatchFlowRegression.

Remaining limitation: The actual Windows executable --test command still needs a Windows run. Diagnostic fixture teams do not verify source-roster accuracy or sound playback.

### Actual shot direction and aiming

Implementation and evidence: Four direct behavior checks verify normalized up/down aiming and actual ball velocity toward the first-half right goal and the switched second-half left goal.

Remaining limitation: Native keyboard focus and audible goal-beacon direction remain unverified on Windows.

### Repeated and off-roster discipline

Implementation and evidence: Card sanctions now require membership in the match and an active on-pitch player. A dismissed player cannot accumulate duplicate sanctions/statistics, and an outsider cannot be silently attributed to the away team. The repeated-card assertion fails against the previous engine. A second yellow still removes exactly one active player.

Remaining limitation: This does not claim exhaustive coverage of all football disciplinary cases.

## Checks for this checkpoint

- MatchFlowRegression: 193 assertions. These exercise actual bilingual menu navigation/actions, source-name preservation, no-mirror selection, and explicit restart/goal-side radar. Run with the real database argument below.
- Existing ContextualMechanicsHarness: 146 assertions. Its tests now explicitly dismiss held restarts and resume the added interval; CPU readiness time starts only after dismissal.
- ContextualCommentaryHarness: 65 assertions passed. ChallengerPhysicsHarness: 19 assertions passed directly with SDK9 against the checked-in project.
- SettingsLifecycleHarness: 66 assertions passed. Native transport is recorded, not executed. The original settings logic fails 24 checks from the initial 60-check version of this harness, with only the transport seam added for fair command capture.
- Before/after evidence: the new pending-human-choice injury assertion fails against the earlier automatic-replacement engine. The old compatible-automatic assertion was corrected, not counted as proof of the user's intended choice. The new English-category test also fails against the prior TeamSelectionManager and passes after localization. The held-restart Space test fails against the previous engine and passes only after the held-input repair. Adding tests did not merely restate successful old behavior.
- Older database harness has stale fixed census, bench-depth and center-spot assumptions, plus real unresolved source-policy/roster failures. Its 3/13 result is not reported as passing.
- Older audio harness cannot begin without original TackleWon.wav. No audio pass or native playback pass is claimed.

```sh
dotnet run --project tests/MatchFlowRegression -c Release -- --database AccessibleFootball2026.Core/Data/players_database.json
dotnet run --project tests/ContextualMechanicsHarness -c Release
dotnet run --project tests/SettingsLifecycleHarness -c Release
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

## Audio audit ledger - 13 November 2026

This is a subject-by-subject coverage record, not a declaration that the game is ready.
Portable tests record the production playback commands against the assembled files. They do not execute Windows MCI, NVDA, Windows Forms focus or audible mixing. Cross-building the Windows application is not a native runtime test.

### Stage 1: recovery and preservation

- Recovered all 874 items in the supplied PES2013_SFX tree, including 873 audio files. Exact Git blob hashes verified for 1,793,052,627 bytes.
- Merged with the previously approved commentary and menu assets. The assembled audio tree has 1,813 files including catalogs and supporting data. No conflicting-byte overwrite was needed.
- All supplied WAV containers parse. Container integrity is not proof of the recording's identity or origin.
- Gemini MenuMusic.mp3 and approved Move/Select v2 bytes remain unchanged. Legacy MenuMusic.wav is not substituted for Gemini music.

### Stage 2: selected root, action sounds and academy consumers

Files: SoundEffectsPlayer.cs, CallnameManager.cs, CommentaryEngine.cs, StadiumSoundEngine.cs, AnthemEngine.cs and tests/OriginalAssetHarness.

- All default audio consumers now use the startup-selected root. They cannot independently select a stale D: installation.
- Ground pass, shot and cross prefer their checked named supplied recordings rather than an unrelated random numeric kick pool. Real-file regression covers the actual methods and MCI commands.
- Academy woodwork playback reaches the supplied checked PostHit file.
- Supplied GoalNet.wav is identical to the generated fallback. It is preserved, but the academy does not claim it is an original recording. Missing goalkeeper-save samples are reported unavailable in the academy.
- This stage's passing checks do not authenticate all match effects. Match-event-to-effect coverage remains an independent stage below.

### Stage 3: national anthem consumers and ceremony lifecycle

Files: AnthemEngine.cs, AccessibleFootballWindow.cs and tests/OriginalAssetHarness.

- The ten available filename codes are mapped from exact country names in the real JSON database, and legacy NAT_ IDs. A club cannot trigger a national ceremony merely through its name.
- Missing-country playback does not substitute another country's file. Device failure does not claim successful playback. Stop clears playback state.
- Match ceremonies use an awaited, nonblocking path. They wait for playback completion rather than truncating each recording after four seconds. Enter, Space or Escape can cancel the ceremony. The second recording does not overlap the first.
- Tests cover all ten real database factories, a recording that stays active past four seconds, sequencing, cancellation, state and missing-file/device failure paths.
- Recording identity is OPEN: all ten supplied National WAVs duplicate other supplied chant/whistle files byte-for-byte. Example: EGY equals SP_R/Chant_600, ARG equals SP_C/Chant_200, TUN equals SP_C/Chant_153, ALG equals Whistle_152/Whistle.wav. This establishes ambiguous provenance, not which label is wrong. No recording has been disabled or replaced on that basis.

### Stage 4: stadium and chant consumers

Files: StadiumSoundEngine.cs, AccessibleFootballWindow.cs, tools/AudioExtractor/Program.cs and tests/OriginalAssetHarness.

- Default ground is neutral. Normal matches reset the previous special-ground selection. The home/ground gate recognizes actual database Madrid/Barcelona without accepting Atletico Madrid through a substring match.
- Home or unknown fouls do not masquerade as away-team fouls for home crowd protest. Away fouls still reach the existing home chant route. New matches reset chant cooldown.
- A missing chosen acoustic no longer silently selects any available stadium or retains the previous stadium. It clears the old selection and uses general crowd ambience. Removed duplicate acoustic application at match startup.
- Identity is OPEN: extractor filenames Chant_N use archive entry Index, not team ID. Original AINF labels are preserved only in chants_index.json. That index and original dt01.img are absent from the recovered tree; supplied WAVs have no original-label metadata.
- The existing numeric generic chant fallback is retained pending identity evidence. Its existence does not verify Madrid/Barcelona identity, nor does a passing route test authenticate its recording. The selected Acoustic_2/3/10 ground identities also remain source claims, not independently authenticated recordings.

### Stage 5: match-event effects

Files: SmartAudioCoordinator.cs, SoundEffectsPlayer.cs and tests/OriginalAssetHarness.

- Six failures reproduced before repair: typed cross/high-pass and sliding/standing tackle facts were ignored in favor of Arabic description words, and a goalkeeper save played a pass kick despite having no dedicated save recording.
- Structured action.kind, pass.height and tackle.sliding now select the production effect route. Old prose heuristics apply only when the relevant structured facts are absent.
- Goalkeeper-save events now use an existing SFX/Saves/GoalkeeperSave recording if available. They never substitute a pass kick. The recovered pack contains no dedicated save recording, so that asset gap remains open.
- Production-route checks cover shot, woodwork, corner, goal kick, throw-in, foul, offside, halftime, fulltime, penalty and opponent tackle in addition to the typed routing cases. Passing a route check does not certify the acoustic meaning of every supplied recording.

### Stage 6: reader lifetime and crowd ducking

Files: SmartAudioCoordinator.cs and tests/OriginalAssetHarness; existing MatchFlowRegression and ContextualMechanicsHarness.

- Reproduced early crowd-volume restoration during an unfinished utterance. Reader ducking now follows SpeakUntilFinishedAsync rather than a fixed 1.4-second timer.
- Overlapping announcements share ducking claims: the first completion cannot restore full crowd volume while a newer announcement still has not finished.
- Controlled-completion tests cover speech longer than the old timer and final restoration. Existing flow tests cover commentary-first latest-only reader updates and goal/penalty-only preemption. Goal announcement tests wait for actual reader completion.
- Canceling a match now cancels its waiting or already-started goal announcement, releases its ducking claim and suppresses stale completion callbacks. Tests cover cancellation before commentary ends, cancellation during speech and a clean next goal.
- User correction at 17:51 Cairo: crowd reduction applies ONLY to screen-reader speech, never to the recorded commentator. The briefly published commentator-ducking addition was removed. Regression checks verify unchanged normal crowd during recorded commentary and preserved reader-only reduction when commentary stops.
- Native NVDA completion, actual audible mixing and older-NVDA estimated waits still need separate verification. These are not inferred from mocked completion.

### Stage 7: beacon and integrated academy/volume checks

Files: GoalBeaconPlayer.cs, menus/SoundAcademyMenuBuilder.cs, tests/OriginalAssetHarness, SettingsLifecycleHarness and PlayerJourneyHarness.

- Twelve additional real-asset beacon checks pass without a production-code change: own-half silence in both directions, rising proximity strength, halftime perspective flip, left/center/right stereo commands before play at saved master/category volume, no possession/no pulse, and exact supplied-byte preservation.
- Refreshed integrated settings lifecycle (66 checks), contextual commentary (65 checks) and player journeys (313 checks) remain green. Academy checks exercise every documented category in Arabic and English, second-preview replacement, Back cleanup and truthful missing-recording behavior.
- This records the portable paths that passed. Native focus, actual spatial perception and audible balance remain unexecuted.

### Stage 8: internal build continuity and archive integrity

- Checked all 187 non-SFX files against public main at 16826a43c8dcd233493eb9be4b3697a67719ba64: no missing or mismatched source content (final-newline normalization only).
- Cross-published a self-contained Windows x64 application and staged all 1,813 audio/catalog/support files with the executable, database and license notices.
- Internal candidate: 1,820 payload files, 1,982,264,444 uncompressed bytes. Every payload was streamed back from ZIP and checked for size/SHA256: zero errors.
- Candidate ZIP: 1,754,969,263 bytes, SHA256 56cf9d3492ec84c058d3c007ea02ab0b8192c1a1f9ea78ae493db3ed017e9fb0. It carries an Arabic candidate-not-release notice and a full payload manifest with known gaps.
- The candidate is retained internally, not published as a release or sent for download. It predates these test/document-only additions. No large redundant transfer is needed while work continues.

### Coverage evidence and remaining stages

Latest OriginalAssetHarness result: 156 passed, zero failed. Reproduced failures before repairs include action-pool routing, stale roots, anthem substitution/identity lookup/state, assumed stadium/home-foul behavior, and wrong/stale acoustic fallback.

Regression evidence: settings lifecycle 66/66, commentary contexts 65/65, player journeys 313/313, match/database 196 assertions, contextual mechanics 146, physics 19/19. Not all older suites exercise the latest changes; logs identify each run. Windows application cross-build succeeds with no errors; one Windows-only API warning can appear on clean portable builds.

Remaining work, not marked complete:

1. Authenticate anthem/chant/stadium recording identities from source labels or independently verified recordings with suitable use permission.
2. Complete trap/tackle and remaining match-event variants, missing-file behavior and authentic missing-save audio; the listed production routes are tested, not every possible event.
3. Exercise commentary and reader ordering under overlapping events, interruptions, cancellation, silence settings and long utterances; verify ducking lifetime rather than assuming a fixed duration is sufficient.
4. Audit beacon, positional panning, replays, academy stop/Back, menu transitions and volume settings as integrated journeys.
5. Build/package the assembled assets reproducibly, verify final package content and run actual Windows/NVDA/native audio acceptance. No release is claimed before that evidence exists.


### Stage 9: supplied chant index and team identity

- The supplied 643-record JSON (SHA256 461ac2da17ecb401cbc020d23d32c94abf4008c9cddc131c6d6357663bfa510b) matches all recovered filenames, durations within 1ms, sample rates and channels. Its authenticity, category and slot-description text is a supplied claim, not independently established provenance. The original envelope is preserved unchanged.
- The reader now accepts both the extractor Index/Label array and the supplied chants/konami_ainf_label envelope. Exact labels, including A_ prefixes, are preserved. Invalid roots/JSON and conflicting duplicate labels fail closed.
- Historical PES2013 Dt01.img mapping: https://www.pesgaming.com/forums/threads/chant-map.92044/ (Oct26 2012 thread, community transcription attributed to OmarAhmed). It assigns Barcelona541-549 and RealMadrid550-557. A later transcription https://pes2013editor.blogspot.com/2017/09/chants-map.html repeats it, not independent official verification.
- These concrete ranges agree with supplied SP_R0108 and SP_R0109 labels. Only existing files at those corroborated ranges+labels enter the two team pools. Entry549 is absent, so Barcelona currently uses541-548. Participation+correct-ground restrictions apply regardless of Home/Away slot, per the latest user correction. National teams and other clubs do not enter those pools.
- Old fallback Chant_173/Chant_153 confused legacy team IDs with archive entry numbers: supplied173=A_SP_C0044_03 and153=A_SP_C0001_03. Those fallbacks are removed. Missing index/files never silently restore them.
- The evidence establishes team-pool identity, not per-suffix goal, comeback or whistle meaning. The old suffix01..05 semantic claims were removed. Non-protest triggers choose a team-pool recording without claiming its acoustic subtype. Unverified protest triggers stay silent instead of playing a cheerful generic chant or raising a whistle callback. Dedicated verified protest/celebration meanings remain open.
- The 2018 relinking guide https://pesnewupdate.com/tutorial-relinking-chants-by-predator002/ also names R0108 Barcelona, but describes CPK/ACB/AWB rather than this Dt01.img. It is not used to infer new archive ranges. SP_R cannot safely be classified as national-only: the corroborated club chants are there.
- Anthem files are not replaced from the historical chant list. Byte equality with chants and inconsistent names remains a provenance gap. No claim of genuine anthem identity, licensing, native playback or Windows/NVDA acceptance is made here.
- Real-asset command tests pass112/112 after reproducing the prior pool/parser failures; they exercise supplied and legacy schemas, absent/malformed/conflicting indices, and keep the reader-only crowd reduction checks. Windows-target application builds with0errors. The older internal candidate is unchanged and does not yet include this source/index repair. It is not a release.


### Stage 10: stadium scope and participation correction (in progress)

- User clarification at18:03:41 defines home as ownership of the selected stadium, not a UI slot. A team must participate and the selected stadium must be its real ground. Home/Away slot is irrelevant. Madrid+Barcelona at Bernabeu selects Madrid, at CampNou selects Barcelona, at SanSiro stays neutral. Context handling now selects that ground's participating owner and its own score/opponent, rather than always the Home slot.
- Live production roster blob5bda1b220c73fa2c350c7301b6e57a54f8b753b3 contains529teams:318clubs+211national teams across13groups.643is the chant-record count. There are no stadium fields. Only stadiums bound to actually present production teams may be added; shared/multiple grounds are not forced into1:1counts. Complete stadium coverage is still in progress, not claimed here.
- Venue identity is separate from recording identity: confirmed grounds do not authenticate Acoustic_2/3/10 as recordings of those grounds.


### Stage 11: sourced stadium catalog foundation and match selection (partial)

- Eight source-backed grounds currently cover nine production teams, leaving520of529without verified ground entries. This is explicitly partial coverage, not the requested full catalog completed.
- SanSiro is one shared entry forACMilan+InterMilan, source https://sansirostadium.com/the-stadium/?lang=en . Bernabeu/RealMadrid source https://bernabeu.realmadrid.com/ . SpotifyCampNou/FCBarcelona source https://www.fcbarcelona.com/en/card/4510623/spotify-camp-nou-202627-pass .
- National grounds: Wembley/England source https://www.wembleystadium.com/about/stadium-facts-and-features ; HampdenPark(currentBarclaysHampden)/Scotland sources https://www.scottishfa.co.uk/en/stadium-history and https://www.scottishfa.co.uk/en/barclays-hampden . CardiffCityStadium/Wales,Parken/Denmark,Ullevaal/Norway source https://faw.cymru/news/venues-confirmed-for-nations-league-fixtures/ . These are documented grounds, never a claim that each nation has only one stadium.
- Every entry carries its sourceURL and is filtered against actual production JSON teams at runtime. No unrelated team or fabricated stadium is added. Missing evidence stays missing. Stadium identity does not imply a matching authentic acoustic recording or team chant exists.
- Stadium selection now intercepts all valid match starts, not just Clasico. Cup-final and rivalry flags survive the choice; explicit selection is consumed once before match initialization. Neutral is always available, and other catalog grounds may be selected as neutral venues for unrelated teams. The old ordinary-match hard reset is removed only after an explicit selection.
-112/112real-asset and catalog command checks, settings66/66 and player journeys313/313 pass. Windows-target application builds with0errors. The checks include sharedground membership, production-only filtering, nationalgroundtypes, honest520pendingcount, unknownkeyrefusal and selected stadium naming. Native WinForms keyboard/focus and actual Windows/NVDA/audio behavior are not certified by these checks.


### Stage 12: complete production English-league ground pass

- All20clubs in the production PremierLeague(England) group now have a current documented ground. Official league source read in full: https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/ , DavidCoverdale,26September2026. Only team-to-ground identity is used, not its capacities, anecdotes or unrelated claims.
- Exact production names are bound to the source identities, including ArsenalFC,BrentfordFC,EvertonFC,LiverpoolFC,ChelseaFC,FulhamFC,SunderlandAFC. Everton is bound to HillDickinsonStadium, not the former GoodisonPark. No team is added or league roster changed.
- Catalog now28unique grounds covering29actual teams, with500still awaiting sourced grounds. This supersedes Stage11's partial8/9/520checkpoint. SharedSanSiro staysoneground, and national grounds are not claimedexclusive.
-114/114real-asset/catalogchecks pass including all20Englishclubs exactlyonecurrentdocumentedground and no staleGoodisonfallback. Broadergroundcoverage, authenticstadiumacoustics, mostteamchants and nativeWindowsUI/audioacceptanceremainopen. Candidateisstillinternalandhasnotbeenrebuiltfor theselaterrepairs.


### Stage 13: Italian and German production club grounds; shared-ground decision

- All20Italian and18German production clubs now have source-backed grounds. Italy: official SerieA notice27 published4September2026 for2026/27, read in full: https://images.legaseriea.it/image/private/fl_attachment/prd/atagcfmsdksksrhe3amr.pdf . Germany: official2026/27 club-and-ground list read in full: https://www.bundesliga.com/en/bundesliga/clubs . Only venue identity is used, not unrelated capacities or claims.
- Names are bound to actual production records, including their existing spelling variants. Atalanta uses NewBalanceArena; Hoffenheim uses SNPArena. Roma/Lazio share one RomeOlimpico entry, just as Milan/Inter share one SanSiro. No new team is created.
- Catalog now63uniquegrounds cover65actualteams;464teams still await sources. Earlier partial counts remain historical checkpoints, not full-coverage claims.
- User-approved shared-ground rule: if both participants use the selected ground, choose the designated Home team, currently the human-controlled team. If only the Away-slot team uses it, that sole owner still wins. If neither does, neutral crowd. The resolver is tested for both Milan/Inter orders, sole-away ownership and no ownership. This does not invent Milan/Inter chant recordings; audible pools remain the two corroborated Madrid/Barcelona pools.
- Stadium selection puts participating teams' verified grounds first and groups other verified grounds by actual database competition. This avoids one long list as coverage grows. Sharedgrounds are deduplicated within groups; neutral and Back remain available. Cup-final/rivalry flags still pass to match start.
-125/125portable real-file/catalog checks pass; Windows-target build has0errors. Group data and callbacks are source/build checked, but native WinForms focus, keyboard and screen-reader acceptance remain unexecuted. No claim of native Windows/NVDA/audio verification or finished release.


### Stage 14: full random chant clips and compact stadium first menu

- Latest user instruction supersedes the earlier25-second-start/goal-force behavior: each team clip finishes before another random selection; no fixed order, repeats allowed. Removed the old25second timer and goal-event teamchant start. Playback status is checked on the actual chant channel; playing/paused/unknown/error state cannot be treated as completion. Explicit force cannot cut a clip. Natural completion allows the next random clip when active-play context is next processed. StopAll and volume muting clear the playback gate through existing channel cleanup.
- Goals retain the existing crowd_goal/CrowdGoal route, not a team chant. MissedChance retains crowd_miss/CrowdMiss. Recovered CrowdGoal, CrowdMiss and CrowdCheer are byte-identical to supplied Crowd files, with numeric duplicates411,425,412respectively. This is byte provenance and routing evidence, not a claim of independent audible semantic or licensing verification.
- Compact stadium first menu shows verified participating grounds once, then Chooseanotherstadium. That opens remaining grounds grouped by actual database competition; owned grounds are excluded from that list. Back/Escape uses existing menu history. Neutral remains available. Missing ground evidence is never filled with an invented venue.
-134/134real-file/recorded-command checks, settings66/66, journeys313/313 and Windows-target build0errors. Tests cover active clip refusal, completion, force refusal, status error, next-match cleanup, actual goal crowd route and missedchance route. Native Windows/NVDA/mixing/menu acceptance remains unexecuted. Crowd reduction remains screen-reader-only, never recorded commentator.


### Stage 15: Egypt-labeled recording rejected as national anthem

- User listened to the existing EGY file preview and identified it as a chant. The exact supplied WAV SHA256b76a2253a9f3ecb365c0c708551bbc9cdf99b8f511b82c5380a6258f812b3d94 is now excluded from national-anthem lookup and preview, while all source bytes remain unchanged. No actual chant team is inferred from that judgment.
- Exclusion is recording-specific, not a permanent country ban. Other countries are not classified from the user's Egypt-only identification; their recording identities remain individually unverified. A future verified Egypt replacement can use the same country code without matching the rejected file hash.
-134/134portable checks remain green, including Egypt ceremony/preview refusal and the existing duration/skip/device-error tests using a different file fixture. Passing those other-country playback tests does not authenticate their music. No source asset is deleted or replaced.


### Stage 16: Actual Spanish production clubs

- Added sourced grounds for the remaining 18 Spanish clubs, using current LALIGA club stadium fields. Catalog now81distinctgrounds for83of529productionteams,446stillawaitingevidence. All20Spanishproductionclubs nowhaveanentry; noextrateams introduced.
- Betis uses Estadio La Cartuja, corroborated by the current2026/27homefixture, rather than the same club page's stale Benito Villamarin FAQ. Rayo uses Estadio de Vallecas from the current league club/fixture page, not an unconfirmed temporary Butarque lead. Atletico's current stadium field names Riyadh Air Metropolitano; the older Civitas FAQ is not used. Sociedad lists Anoeta and describes Reale Arena, retained as one entry.
- Each added entry includes its exact fetched league URL. Names do not establish acoustic identity or chant availability. Existing verified Madrid/Barcelona entries are preserved.
-138/138portable checks and Windows-target build0errors after this catalog extension. Native runtime verification remains open.


### Stage 17: National anthem feature canceled by user

- User classifies the supplied national-labeled recordings as national-team chants and explicitly cancels the national-anthem feature itself. This supersedes the earlier single-file rejection and per-country authentication work. This is the user's supplied-set classification, not independent music certification or a claim about recordings elsewhere.
- Removed the national pre-match ceremony and keyboard/wait state, the academy national-anthem entry and callback. Legacy national API entrypoints remain inert for binary/source compatibility: no file lookup, no open command and no waiting. No country mappings are invented for chant playback.
- Preserved all original bytes, approved Champions League intro/academy playback, Gemini menu music and Move/Select v2. No replacement anthem sourcing is needed.
-137/137real-file/recorded-command checks after cancellation; national no-play/no-wait, academy removal and UCL preservation are covered. Native Windows/NVDA checks remain unexecuted.


### Stage 18: Chant-backed grounds only; flat sound academy

- The user changed stadium scope: a ground is useful here only when its team's distinctive chant is available. The all-529-team coverage target and its pending denominator are canceled. Ground-name research alone does not qualify a menu choice. Earlier coverage totals above are historical, not current targets.
- Removed all unsupported ground entries, including the French metadata briefly committed before this correction. Only Santiago Bernabeu / Real Madrid and Spotify Camp Nou / FC Barcelona remain, backed by the corroborated SP_R0109 and SP_R0108 recovered chant pools. Neutral was initially retained here, then explicitly removed in Stage 19. Other team mappings stay unresolved, not inferred from generic crowd sounds. The shared-ground home tie-break remains the design if a supported shared ground is added in future; there is no unsupported shared-ground entry now.
- Academy is one flat list, without category submenus: exactly nine semantic cues and nine goal-beacon demonstrations. Beacon demonstrations cover center/left/right, near/louder, far/softer, out-of-range silence, no-possession silence, and the reversed left/right relationship in the second half. Silent cases are explicitly explained as intentional, not reported as missing samples. Distance/half examples use the real beacon calculation. Preview selection and Back stop prior channels.
- Match effects, crowd and music previews are no longer academy entries, but their underlying game audio is not removed. The approved UCL startup intro, Gemini menu music and exact Move/Select v2 bytes stay intact. National anthem cancellation remains in force.
- 121/121 real-file/recorded-command checks, 392/392 player-journey checks and 66/66 settings checks pass. Windows-target build has zero warnings and errors. Counts changed because obsolete catalog/category checks were replaced with the revised requirements; this is not native Windows/NVDA or audible mixing verification.

- Two offered grounds means two currently corroborated and wired chant-to-team mappings, NOT proof that other teams have no supplied chants. The 643-record chant index contains many other recordings whose modern team/country identities remain unresolved. The user's set-level national-chant classification does not establish individual country mappings. Those bytes are preserved and the mapping investigation remains open; additional eligible production-team grounds can be added after identity and actual available recording checks, not by name-only expansion.


### Stage 19: Remove the invented neutral stadium choice

- User explicitly rejects the neutral stadium option. Removed that selectable item and its announcement. A fresh session has no selected ground, not an invented neutral venue; its display name says no stadium selected. Unknown or stale keys cannot select a team's chants. Generic background crowd fallback remains an audio safety path, not a selectable stadium identity.
- No original audio, chant index, roster or research evidence was deleted. Unsupported StadiumDefinition rows were removed from current catalog source, not just hidden in UI; their venue facts remain in saved evidence and Git history. National ceremony and academy-only entries were removed under the user's separate requests. Game audio outside the academy was retained.
- Earlier 81/83 and later name-coverage counts were researched venue metadata, not verified chant coverage. Current two offered grounds reflect two wired mappings, not proven absence of other chants. Mapping work continues from supplied files and historical slot metadata.


### Stage 20: Three more wired team-chant mappings

- Implemented Manchester United / Old Trafford, Inter Milan / San Siro, and AC Milan / San Siro. Current implemented coverage is five production teams using four offered grounds, not the much larger historical candidate inventory. The new pools contain 19 present recordings: 535-540 / SP_R0100, 592-597 / SP_R0119 and 604-610 / SP_R0121. Existing Madrid and Barcelona routes are retained.
- Evidence standard: the 2012 PESGaming dt01 original-slot map attributes those exact ranges; the 2014 editing reference lists the same ranges; the supplied index labels and actual recovered files match every new entry. The later 2017 transcription is derivative, not independent confirmation. This corroborates historical slot attribution, not independent listening authentication or proof that this archive was never modified. No renamed national-anthem copy is used as country/team evidence.
- Sources: https://www.pesgaming.com/forums/threads/chant-map.92044/ and http://agunbharok.blogspot.com/2014/05/edit-teamstadionwasitturf-lapangandan.html . Venue relationship is confirmed by https://sansirostadium.com/the-stadium/?lang=en and https://www.premierleague.com/en/news/4725666/all-you-need-to-know-about-the-20-premier-league-stadiums/ .
- The engine now resolves explicit team-name/range/label records instead of the old Madrid-or-Barcelona branch. Every candidate still needs both exact range and original label prefix, plus an existing recording. Unknown teams/keys fail without substituting Madrid or Barcelona. Shared San Siro appears once; the designated home club supplies chants when both participants own it, otherwise the sole participating owner does, including the away slot. Full random clip completion and no goal-triggered chant rules remain.
- 136/136 recorded-command/real-file checks pass, including all 19 new file/label checks, exact playback range for each new club, ownership/visitor refusal, and reversed Milan/Inter derby ownership. The research inventory's 100 historical groups is not implemented coverage. Remaining mappings stay open. No native Windows/NVDA or independent audible chant certification is claimed.


### Stage 21: Five further wired mappings, separate from research

- Added and tested Valencia / Mestalla (558-563, SP_R0110), Paris Saint-Germain / Parc des Princes (572-575, SP_R0114), Marseille / CEPAC Velodrome (566-571, SP_R0113), Bayern / Allianz Arena (624-626, SP_R0127), and Atletico / Riyadh Air Metropolitano (642-646, SP_R0172). All 24 new entries have both exact supplied labels and present WAV files. Current implementation is ten production teams / nine offered grounds; historical candidate inventory is not implementation coverage.
- Same original-slot evidence standard and caveats as Stage 20. The exact production team names were checked in the live-recovered production database. Current ground sources are retained on each entry; Atletico uses the current Riyadh Air stadium field, not the older Civitas FAQ. Marseille's current CEPAC name comes from the naming partner's announcement, not the stale Orange listing. No invented neutral option and no per-event slot semantics are introduced.
- 156/156 real-file/recorded-command checks pass after extending each team's owner/visitor, file/label and playback-range tests. Other supplied tracks remain open research. No independent listening authentication or native Windows/NVDA verification is claimed.
