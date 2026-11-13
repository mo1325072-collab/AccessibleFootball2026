# Windows playtest repair, 2026-11-14

## Changed production paths

- The startup theme now owns an exclusive intro state. No main menu is constructed until playback ends or Enter/Space skips it. All other keys are suppressed during the intro. A UI timer observes completion without blocking Windows messages.
- Removed the old El Clasico and Custom cup final test entries. Exhibition, real tournaments and draft remain.
- PCM WAV effects now open with the volume-capable MCI driver. The previous waveaudio route rejected setaudio and returned before play, silencing the nine semantic cue previews and match cues. Approved audio files are unchanged.
- Match audio context explicitly blocks menu music starts and live-volume restarts until returning to the main menu. A saved volume update cannot revive menu music during play.
- Every recorded commentary caller shares an atomic non-preemption gate. Goals and penalties wait too. Full native/WAV duration protects long phrases; the old30-second cap is gone. Forced player callnames cannot interrupt a phrase.

## Settings update checks

The current application version is2026.11.14. Settings has a manual Check for updates button and a saved startup-check checkbox, off by default. Save Settings commits the checkbox; Cancel restores its old value. Startup checks run after the exclusive intro, never block it and never announce over a match. Only a newer available version produces a startup notice. Manual checks report current, no published game or failure distinctly.

This is a check and optional release-page link, not an automatic downloader/installer. GitHub release policy:

- Public stable release, neither draft nor prerelease.
- Tag `game-vYYYY.MM.DD` (numeric .NET version comparison).
- Uploaded, nonempty asset named `AccessibleFootball2026-Full-Setup.exe`.
- Release and download URLs must belong to this repository on HTTPS GitHub.

The existing `build-inputs-2ef5d009` prerelease is deliberately not a game update. Until a genuine installer release exists, the checker says that no installable game release has been published, not that an unverified build is up-to-date. Network/API failure is never reported as current. Merely adding these source changes does not create a downloadable update.

## Installer

Build-Complete.py now requests an initial Arabic/English language chooser and standard welcome, destination and program-group pages. It uses Inno Setup's official Arabic messages, bundled in Inno Setup6.5.0 or newer. Existing older Inno installations without Arabic.isl need upgrading. No silent replacement of the approved audio, data or user settings is part of these repairs.

For code-only changes the builder can publish the rebuilt single EXE separately, keeping the already installed Assets and player database. This repair does not claim that a downloadable EXE patch or finished installer has already been published.

## Verification boundary

The game cross-builds for Windows. Automated checks cover the actual academy/match audio entrypoints under MCI command-contract failure simulation, concurrent commentary admission, long-clip duration, match music isolation, update filtering, settings persistence and match flow. These checks do not execute Windows MCI sound output, Windows Forms layouts, NVDA or Inno Setup. Native listening and visual/accessibility verification remain required; do not describe this source checkpoint as a completed Windows acceptance test.

Deferred as requested: player-position corrections. No chants were removed.

References: Microsoft MCI setaudio command documentation at https://learn.microsoft.com/en-us/windows/win32/multimedia/setaudio ; official Inno translations at https://jrsoftware.org/files/istrans/ ; language configuration at https://jrsoftware.org/inno-setup/documentation/topic_languagessection.htm .

## Tactics controls, latest correction

Production pre-match and pause-menu tactics now open a native Windows dialog, not a cycling list item. Exactly two sliders (pressing and passing), three drop-down lists (tackling with five choices, playing style with the five user-supplied names, marking with zonal/man-to-man), and an offside-trap checkbox. Apply commits to the actual team model; Cancel changes nothing. There are no depth/tempo sliders or relink action. Internal linked depth/tempo still follow the two requested sliders. Passing maps to the existing pass-scoring/support model, not the Balanced option; shooting, counter, wings and long balls retain their distinct executed effects. The accessible menu adapter used by headless regression tests mirrors these same choices; the production callback opens real Windows controls. Native dialog layout/NVDA verification remains outstanding.

## Match/menu audio lifecycle

Entering pause, interval or the injury-choice menu suspends the commentary queue and current recorded phrase, pauses crowd/chant channels and closes transient match effects. Menu feedback and the screen reader remain available. Resume continues the same phrase with its duration reservation extended for time spent paused. Leaving the match or starting another match closes the playback scope, invalidates pending callbacks, clears queued lines and stops all native channels before menu music is allowed. Late match events cannot reopen a closed commentary scope. The recording library explicitly starts its own playback scope when the user selects a recording.

## Repeated pass commentary

Ordinary forward high-pass launch contexts in the supplied catalog have only recordings04394and04397 eligible, matching the reported two utterances. Immediate cyclic anti-repeat selection alternated them, while dropping routine events during playback could starve completed/intercepted-pass and current-possession commentary. The engine now keeps a bounded fresh snapshot per routine event kind, prefers actual outcomes/current play to generic launches, discards a launch after its pass resolves and expires stale snapshots. A recently used generic launch recording is not forced back into every free gap; brief silence is valid. All catalog recordings remain intact and eligible in appropriate contexts. Critical halftime/fulltime events clear stale routine snapshots. Tests reproduce launch starvation and verify outcome/current-play survival with both launch recordings preserved. This does not claim that the user's particular saved match or a CPU freeze was reproduced.

## Native key release routing

The live field KeyDown handler used SuppressKeyPress=true. Windows Forms documents that this suppresses KeyUp too, while the input handler requires KeyUp to release movement and re-arm S/W/D/etc. This could leave actions latched after their first use. Field controls now consume KeyDown/KeyPress without suppressing KeyUp; PreviewKeyDown marks game keys as input rather than list-navigation commands. Menus retain their own suppression behavior. The source-level cause is confirmed by Microsoft's contract at https://learn.microsoft.com/en-us/dotnet/desktop/winforms/input-keyboard/events . Native Windows/NVDA repeated-tap/hold acceptance remains unverified.

Unidentified background music despite Music=0 is not claimed fixed solely by the menu-channel repair. The remaining possibilities include embedded musical content in retained crowd/stadium/chant recordings, which use the crowd category. No such recording has been identified by ear yet, and approved chants are not muted or deleted by guess.


## Shootout follow-up

Shootouts retain the selected stadium crowd loop rather than stopping it. ShootoutFinished is critical and maps to the existing conditioned FullTime recordings, so it waits for the previous outcome phrase without being dropped by the routine throttle. A retained accessible result screen reports win/loss/draw, match goals separately from shootout goals, recorded scoring sequence and measured statistics. It stays until the player leaves it. Ordinary match completion uses the same retained summary; an early exit is labelled stoppage, not a fabricated result.

Penalty input no longer offers shot/dive choice menus. Up/down continuously change the facing-Y component at the ordinary game's0.9per-second rate, not ball height. Left/right held with W or S target the corresponding far corner; W is normal power and S is lower-power precise placement. Keeper turns use the same arrows with W/S to commit. Held actions cannot auto-submit the following kick without release. Aim feedback is rate limited. Actual shot error, goal-mouth position, flight speed/time and keeper anticipation/dive/reach determine results. Placement reduces shot error and speed; a keeper committed to the wrong corner cannot receive an unconditional weak-shot save bonus. No goal-beacon removal is inferred from the ambiguous voice clause. Native Windows control/audio/NVDA acceptance remains outstanding.


## Cairo International Stadium

Added Cairo International Stadium to the selectable sourced stadium catalog for the actual Egypt national team. It appears directly when Egypt participates and through the production national-team stadium group. CAF identifies the stadium as Egypt's national-team home: https://www.cafonline.com/caf-u-20-africa-cup-of-nations/news/cairo-ismailia-and-alexandria-ready-to-host-u20-afcon-egypt/ . This does not invent a dedicated recording, chant, capacity-driven model or club ownership grant. Existing general crowd acoustics are used; chant eligibility still requires an independently mapped recorded pool.
