# Original User Request

## Initial Request — 2026-11-04T17:33:34Z

Comprehensive overhaul of the in-match gameplay engine and input subsystem in `AccessibleFootball2026` to make the match 100% playable, responsive, and tactile for visually impaired users using NVDA. Fix edge-triggered action processing, pass reception, player switching, and live match keyboard focus.

Working directory: `C:\Users\mohamed farag\.gemini\antigravity\scratch\AccessibleFootball2026`
Integrity mode: development

## Requirements

### R1. Edge-Triggered Action Processing & Form-Level Key Handling
- Replace pure 50Hz level-sensitive polling with edge-triggered action execution (On-Press / KeyDown) for discrete actions: Short Pass (`D`), Shoot (`W`), Cross/Lob (`S`), Through Ball (`A`), Switch Player (`Ctrl`), Tactical Radar (`Space`/`C`), and Pause (`Esc`).
- Ensure directional movement (Arrow keys) and sprinting (`Left Shift`) remain continuous (while held), while action keys fire exactly once per tap and do not cascade into defensive slide tackles or fouls when released.
- Hook key events directly into `Form.KeyDown` and `Form.KeyUp` with `KeyPreview = true`, supporting both English and Arabic keyboard layouts, while ensuring NVDA remains in Focus Mode without keyboard focus dropping to the desktop.

### R2. Pass Transit & Teammate Reception Physics
- When a pass (`D`) or through ball (`A`) is executed, the intended receiving teammate must actively adjust their movement towards the ball's trajectory.
- Ensure the receiving player is not skipped during physics updates while the ball is in flight, and immediately assumes ball control upon arrival instead of freezing and letting opponent AI snatch it.

### R3. Responsive Defensive Actions & Clean Player Switching
- Ensure pressing `Ctrl` (or `Left Ctrl` / `Right Ctrl`) switches smoothly to the closest outfield defender once per press with immediate debouncing (no rapid 50Hz cycling through the entire squad).
- Tackle command (`S` for standing tackle, `W`/`A`/`D` for slide tackle when defending) must only trigger on distinct key presses, accompanied by distinct sound effects and spoken distance/success announcements.

### R4. Instant Auditory Confirmation
- Every match action must produce immediate audio feedback (e.g., distinct kick sounds for pass/shot/cross, referee whistle, crowd murmur, and concise spoken player names).

### R5. Build, Simulation Test & Desktop Deployment Verification
- The entire solution must compile cleanly with zero errors (`dotnet build -c Release`).
- The automated simulation suite must execute successfully with exit code 0 (`--test`).
- The release executable must be published to `C:\Users\mohamed farag\Desktop\AccessibleFootball2026.exe`.

## Acceptance Criteria

### Input Responsiveness
- [ ] Tapping `D` (Pass) or `W` (Shoot) once executes exactly one action and does not trigger unintended secondary actions upon release.
- [ ] Arrow keys smoothly move and orient the player towards the opponent goal.
- [ ] `Ctrl` switches control cleanly to the nearest defender without cycling multiple players in a single press.

### Match Playability
- [ ] Passes reach the receiving teammate who traps the ball and allows the user to immediately continue play.
- [ ] Game loop does not freeze after shots, passes, or out-of-bounds situations.
- [ ] Window retains input focus throughout the live match without interference from NVDA browse mode.

### Build & Delivery
- [ ] `dotnet build -c Release` succeeds with 0 errors.
- [ ] Simulation `--test` runs to completion with exit code 0.
- [ ] Updated single-file executable is deployed to `C:\Users\mohamed farag\Desktop\AccessibleFootball2026.exe`.


## Follow-up — 2026-11-04T20:04:37Z

Resume execution following server restart. Please check current status of Milestone 2 and continue driving the remaining milestones (M2 completion, M3 Defensive Switching & Audio Feedback, M4 E2E Testing, Build & Desktop Deployment) to completion.


## Follow-up — 2026-11-04T20:34:17Z

CRITICAL USER DIRECTIVE:
Eliminate verbose, unnecessary tutorial instructions and unsolicited hints from screen reader announcements during live matches.
- DO NOT say self-evident instructions like 'اضغط Ctrl للتبديل' or 'اضغط W للتسديد' during play or when out-of-range.
- Keep in-match announcements high-signal, concise, and telegraphic.
- For player switching: announce only the player name and assigned position (e.g. 'محمد عبد المنعم - قلب دفاع').
- For out-of-range tackles: announce only the distance concisely (e.g. 'بعيد: 3.5 م') without tutorial prompts.
Ensure all speech text in MatchEngine, SmartAudioCoordinator, and GamePlan menus respects this concise, zero-clutter rule.


## Follow-up — 2026-11-05T13:47:51Z

# Teamwork Project Prompt — Accessible Football 2026/2027

Working directory: C:\Users\mohamed farag\.gemini\antigravity\scratch\AccessibleFootball2026
Integrity mode: development

## Project Description
Accessible Football 2026/2027 is a native WinForms .NET 9 (`win-x64`, PE Subsystem 2 GUI, zero black console windows) accessible audio soccer simulation designed specifically for blind and visually impaired gamers. The game provides a high-fidelity binaural 3D auditory experience where the pitch is perceived through authentic stadium acoustics, physical ball impacts, referee whistles, tactical earcons, and real broadcast Arabic commentary by Raouf Khlif (PES 2013), completely silencing robotic screen reader speech during active gameplay.

Execute the work strictly milestone-by-milestone, sending regular progress update messages as milestones are achieved.

---

## Requirements

### R1. Authentic Gameplay Controls, Physics & Half-Time Side Switch
- **Controls**:
  - `S`: Short Pass (التمرير الأرضي المباشر).
  - `D`: Cross / Lob (الرفعة / العرضية داخل منطقة الجزاء).
  - `W`: Powerful Shot (التسديد القوي نحو المرمى الحالي).
  - `A`: Through Ball (التمريرة البينية في المساحة).
  - `Ctrl`: Switch Defender (تبديل المدافع بضغطة كنترول عادية مع نقرة صوتية سريعة وخاطفة، وصمت تام لقارئ الشاشة).
  - `Shift`: Sprint (الجري السريع).
  - `Space`: On-Demand Tactical Radar (ينطق عند الطلب فقط: مين معاه الكرة، في أي منطقة، والمسافة لمرمى الخصم الحالي، أو تفاصيل الكرة الثابتة).
- **Fair Tackle Formula**:
  - Compare `Distance(Tackler, Ball)` vs `Distance(Tackler, Opponent)`.
  - If closer to the ball -> Clean Tackle Won (100% ball recovery at user's feet, TackleWon earcon).
  - If closer to opponent body -> Foul called against user (referee whistle + foul earcon).
- **Half-Time Side Switch (تبديل المرميين في الشوط الثاني)**:
  - Match pauses at 45:00 for Half-Time (صافرة نهاية الشوط الأول، إعلان النتيجة).
  - In 1st half: User attacks Right (مرمى الخصم عند X = 105, movement forward with Right arrow).
  - In 2nd half: Teams swap sides! User attacks Left (مرمى الخصم عند X = 0, movement forward with Left arrow, shooting with W targets the Left goal, and Pitch.CalculateZone calculates zones with attackingTowardsRight: false).

### R2. IFAB Tactical Realism: Substitutions, Disciplinary Cards & Injuries
- **Substitutions (Law 3)**:
  - 5 substitutions allowed in regular 90 minutes across a maximum of 3 stoppage opportunities (windows).
  - Multiple substitutions made during the same stoppage count as 1 opportunity.
  - Substitutions during half-time do NOT consume a stoppage opportunity.
  - Pausing the match to adjust formations or change tactics/player positions on pitch never consumes a substitution window (tactical freedom like a coach giving touchline instructions).
  - In Extra Time: +1 additional substitution (6th sub) and +1 additional stoppage window (4th window).
- **Disciplinary Sanctions (Law 12)**:
  - Yellow Card (Caution) for reckless challenges or stopping promising attacks (SPA).
  - Second Yellow Card -> Red Card and dismissal.
  - Direct Red Card for violent conduct / serious foul play (tackles from behind) or denying an obvious goal-scoring opportunity (DOGSO).
  - Sent-off players leave the pitch; the team plays with 10 players, opening real tactical space.
- **Injuries System**:
  - Minor Knocks: Temporary 20% stamina/speed reduction from hard tackles.
  - Severe Injuries: Forced substitution required. If all 5 subs are exhausted, team must complete the match with 10 players.

### R3. Authentic Audio Overhaul & Complete Screen Reader Match Silence
- **Silence Live Match Telemetry**: Zero automatic robotic NVDA speech while running with arrow keys during active match play. The screen reader speaks ONLY in menus or on explicit Space radar query.
- **PES 2013 Audio Asset Integration**:
  - Extract Raouf Khlif broadcast commentary recordings from `D:\games\PES 2013\img\dt00_a.img` (17,323 genuine studio audio recordings, voice only) and bind them to all match events (goals, shots, fouls, yellow/red cards, injuries, tackles, corners, half-time, full-time).
  - Extract authentic stadium acoustics, reactive crowd roars, ball kick impacts, woodwork hits, and referee whistles from `D:\games\PES 2013\img\dt01.img`.
  - Remove all synthetic crowd loops and unrealistic grass running footstep sounds.
  - Generate 8 distinct musical/tonal Earcons (Tackle Won, Ball Lost, Foul, Penalty, Corner, Goal Kick, Throw-in, Player Switch).

### R4. Accessible Menus, User Guide, Academy & Bilingual Settings
- Remove all condescending tutorial strings ("اضغط كذا للمتابعة") from menus and headers.
- Standalone "دليل المستخدم (User Guide)" screen in the main menu.
- Standalone "تعلم أصوات اللعبة (Learn Game Sounds)" academy screen to preview each earcon and sound with explanation.
- Professional Settings:
  - Sliders for volume levels with live audio preview.
  - ComboBoxes for difficulty, match duration, and commentary.
  - Checkboxes for Extra Time and Penalty Shootouts.
  - Save to disk and Reset to Defaults buttons.
  - Bilingual UI switcher: Arabic / English.

### R5. Comprehensive Official 2026/2027 Database
- 7 Full Domestic Leagues (Egyptian Premier League, Saudi Pro League, English Premier League, La Liga, Serie A, Bundesliga, Ligue 1).
- Continental Club Competitions (UEFA Champions League, Europa League, CAF Champions League, CAF Confederation Cup).
- Free Standalone "All FIFA-Ranked National Teams" category sorted by continent for direct exhibition matches.
- Major National Tournaments (FIFA World Cup, UEFA Euro, CAF Africa Cup of Nations, AFC Asian Cup, Copa America).
- Tamper-proof, locked official squads compiled in code from verified sources (Transfermarkt / EA FC 26 season 2026/2027).

---

## Acceptance Criteria

### Gameplay & Rules
- [ ] In the second half, user team attacks Left towards X = 0, Left arrow moves toward opponent goal, W shoots toward Left goal, and zones are inverted.
- [ ] Keybindings: S = Pass, D = Cross, W = Shoot, A = Through ball, Ctrl = Switch defender with subtle earcon, Shift = Sprint, Space = Radar.
- [ ] Substitutions enforce max 5 players across max 3 stoppage opportunities in 90 mins; half-time subs don't consume a window; tactical changes without subs don't consume a window; 6th sub and 4th window granted in Extra Time.
- [ ] Yellow/red cards trigger according to foul severity and DOGSO; red-carded player is removed from pitch and team continues with 10 players.
- [ ] Severe injuries force substitution or 10-man play if subs are exhausted.

### Audio & Atmosphere
- [ ] NVDA is 100% silent during active match running; only Space speaks tactical radar.
- [ ] Raouf Khlif commentary from PES 2013 plays dynamically for all game events.
- [ ] Stadium crowd acoustics and ball kicks from PES 2013 are used; footsteps on grass sound is completely deleted.
- [ ] 8 distinct tonal earcons are wired and audible.

### Menus, Database & Packaging
- [ ] Standalone User Guide and Learn Game Sounds menus are functional.
- [ ] Settings menu includes live-preview sliders, Save/Reset, and Arabic/English language toggle.
- [ ] Official 2026/2027 database contains 7 domestic leagues, continental cups, and FIFA national teams.
- [ ] 100% passing automated test suite (dotnet test).
- [ ] Release single-file executable AccessibleFootball2026.exe built as native WinExe (Subsystem 2 GUI, zero console window) and published to Desktop.


## Follow-up — 2026-11-05T14:26:52Z

[User Direct Instruction - Priority]:
The user explicitly specified:
"ماتنساش escape للإيقاف والانتقال لقائمة المباراة"
Ensure that pressing the `Escape` key during an active match pauses the match immediately and transitions to the in-match Pause/Game Plan Menu (قائمة المباراة / قائمة الإيقاف المؤقت), where the user can adjust tactics, view match stats, review lineup, or make substitutions (subject to the 5 subs in 3 windows rule). Make sure this is bound, tested, and documented across InputHandler, Window, and User Guide.


## Follow-up — 2026-11-05T15:15:12Z

[User Direct Instruction - Critical Audio & Aiming Gameplay Mechanic]:
The user explicitly specified:
"ماتنساش تخلي السهمين الأعلى والأسفل في المباراة يغيروا زاوية اللاعب بحيث يكون مواجه للمرمى ويكون في صوت للمرمى بس مايكونش مزعج بيرتفع تدريجيا لما اللاعب بتاعي يكون قريب من المرنى ومن خلال الزاوية بتاعة الصوت أقدر أقرر لو هعدل اتجاهي وأشوط أو هعمل كروس"

Requirements to integrate immediately into the Simulation and Audio Engine:
1. **Up/Down Arrow Aiming Angles**:
   - Pressing Up and Down arrow keys dynamically modifies the player's aiming angle/body orientation towards the goal or target (adjusting Y angle/direction), allowing fine directional control before shooting (W) or crossing (D).
2. **Subtle, Dynamic 3D Spatial Goal Beacon (منارة صوت المرمى ثلاثية الأبعاد)**:
   - Provide a gentle, non-intrusive, harmonic acoustic beacon for the opponent's goal (e.g. a soft, warm resonant chime or subtle pulse - NEVER a harsh or annoying beep).
   - Volume dynamics: Gradually rises in volume as the user player approaches the opponent goal (silent when far in own half, subtle and quiet in midfield, swelling smoothly as the player enters the attacking third and penalty box).
   - 3D Stereo Panning: Dynamically calculated based on the angle and Y-offset between the player and the goal center! If the player is on the wing, the beacon pans to the opposite ear. When centered, the beacon is perfectly balanced in both ears, signaling a clear direct shot on goal.
   - Half-time adaptation: The beacon targets the active opponent goal (X=105, Y=Width/2 in 1st half; X=0, Y=Width/2 in 2nd half).

Ensure this is bound, tested, and added to the audio engine and user guide.


## Follow-up — 2026-11-05T16:06:16Z

[User Direct Instruction - Priority for Milestone 3 Audio & Commentary]:
The user explicitly specified:
"خلي اختبارات المرحلة الثالثة لما تبدأ تتأكد من الربط الجيد لالتأثيرات الصوتية وملفات التعليق بأحداث اللعبة عشان أستفيد من كل تأثير صوتي وكل جملة تعليق ولو لقيت حاجة في ملف التعليقمش موجودة عندك في أحداث اللعبة قول لي عليها وضيفها في أحداث اللعبة"

Requirements to integrate immediately into Milestone 3:
1. **Exhaustive Commentary & SFX Event Binding Verification**:
   - Milestone 3 automated and behavioral test suite must explicitly verify that every match event (Goal, Miss, Shot, Cross, Pass, Tackle, Foul, Yellow Card, Red Card, Injury, Offside, Penalty, Corner, Throw-in, Goal Kick, Halftime, Fulltime, Save, Woodwork hit) is cleanly and deterministically bound to real audio SFX and Raouf Khlif commentary clips.
2. **Commentary Audio Cataloging & Game Event Expansion**:
   - During extraction and classification of Raouf Khlif commentary from `dt00_a.img`, inspect all audio categories and voice lines.
   - If there are commentary voice lines for football scenarios not currently represented in `MatchEngine` (e.g., Goalkeeper heroic saves/fingertip deflections, Woodwork post/bar hits, Stoppage/Injury time announcements, Hat-tricks, Comebacks, Last-minute dramatic goals), inform the user about them and implement the corresponding `MatchEvent` and simulation triggers in `MatchEngine` so that no commentary category is wasted.


## Follow-up — 2026-11-05T18:26:28Z

[User Direct Instruction - Priority Earcon Addition]:
The user explicitly specified:
"في نغمة نسيت أقول لك عليها هي نغمة فشل الاستخلاص بسبب إن اللاعب بعيد عن مسار الكرة"

Requirements to integrate immediately into Milestone 3 Audio Engine:
1. **New 9th Earcon: TackleOutOfReach / TackleMissed (نغمة فشل الاستخلاص لبُعد المسافة)**:
   - Generate and add a dedicated subtle, intuitive tonal earcon (e.g. a soft swift air whoosh / gentle descending tone) to `Assets/Audio/SFX/Earcons/TackleMissed.wav`.
2. **Simulation & Audio Trigger Wiring**:
   - In `MatchEngine.cs` and `SmartAudioCoordinator.cs`, whenever the user attempts a tackle (standing or slide) but the tackler is beyond the reach threshold (`distance > reachThreshold`), play this `TackleMissed` earcon immediately instead of silence or speech.
   - Provides instant spatial feedback that the player tackled empty air because they were too far from the ball.
3. **Integration**:
   - Include in `Milestone3AudioOverhaulTests.cs` and document in Sound Academy / User Guide.


## Follow-up — 2026-11-05T19:40:07Z

[User Direct Instruction - Priority Menu Cleanliness & Narrative Purge]:
The user explicitly specified:
"مش بس جمل اضغط كذا للمتابعة كمان الشرح التفصيلي الممل زي وضع 4 فرق أنت تختار وهكذا إلى آخر الشرح"

Requirements to integrate immediately into Milestone 4:
1. **Purge Long-Winded Descriptive Narratives & Mode Tutorials from Menu Items**:
   - In all menus (`AccessibleMenuSystem.cs`, `AccessibleFootballWindow.cs`, `TournamentMenuBuilder.cs`, `GamePlanMenuBuilder.cs`, etc.), completely purge verbose explanatory text attached to menu item titles (e.g., remove strings like "وضع 4 فرق أنت تختار فريقك وتلعب...", descriptive tutorials of modes, etc.).
2. **Crisp, Professional Functional Titles**:
   - Menu items must be strictly concise and direct (e.g. "مباراة ودية", "دوري أبطال أوروبا", "كأس العالم", "دليل المستخدم", "أكاديمية الأصوات", "إعدادات اللعبة والصوت", "خروج").
3. **Decouple Explanations to User Guide Only**:
   - Any detailed explanation of what a tournament or mode does belongs exclusively in the standalone "User Guide" (دليل المستخدم) screen for players who want to read about it, NEVER spoken when merely navigating through menus.
4. **Test Assertions**:
   - Update assertions in Test 15A of `Milestone4MenuAcademySettingsTests.cs` to verify that menu item titles do not contain descriptive tutorial narratives.
