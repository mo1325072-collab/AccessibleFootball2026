# Project: AccessibleFootball2026 Gameplay Engine Overhaul

## Architecture
- **Language / Framework**: C# 13 / .NET 9.0 Windows Forms (`net9.0-windows`, `win-x64`)
- **Key Subsystems**:
  - `AccessibleFootball2026.App`: WinForms GUI window (`AccessibleFootballWindow`), Form-level keyboard events, NVDA focus management.
  - `AccessibleFootball2026.Core/Input`: `KeyboardInputHandler`, `GameAction`, discrete edge-triggered action queue, continuous arrow & sprint tracking.
  - `AccessibleFootball2026.Core/Simulation`: `MatchEngine`, game loop (50Hz / 20ms ticks), `PassTransitInfo`, player AI, teammate reception intercept, defensive switching, tackling mechanics.
  - `AccessibleFootball2026.Core/Models`: `Ball` (3D physics, linear grass friction deceleration), `Player` (2D pitch coordinates, facing vector, role, fatigue), `Pitch`.
  - `AccessibleFootball2026.Core/Audio`: `SoundEffectsPlayer` (procedural audio synthesis, panning), `SmartAudioCoordinator` (event subscription, priority), `TolkWrapper` (NVDA screen reader speech).

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| F1 | Edge-Triggered Action Queue | Discrete actions (D, W, S, A, Ctrl, Space/C, Esc) fire exactly once per physical keypress; auto-repeat suppressed. | M1 | ORIGINAL_REQUEST R1 |
| F2 | Continuous Movement & Sprint | Directional arrows and Left Shift maintain continuous movement without edge consumption or action interference. | M1 | ORIGINAL_REQUEST R1 |
| F3 | Form KeyDown/KeyUp KeyPreview | Hook Form-level events with KeyPreview = true, suppress ding/echo (`e.SuppressKeyPress = true`), unified VK code handling for English and Arabic layouts. | M1 | ORIGINAL_REQUEST R1 |
| F4 | NVDA Focus Mode Retention | Keep `_displayListBox` visible and focused with live match telemetry to prevent focus dropping to Windows Desktop. | M1 | ORIGINAL_REQUEST R1 |
| F5 | Ball Ground Friction Deceleration | Replace geometric 0.94 frame decay with linear physical deceleration (~2.5 m/s²) so 18-25m passes reach targets. | M2 | ORIGINAL_REQUEST R2 |
| F6 | Pass Transit State Tracking | Introduce `PassTransitInfo` tracking pass type, trajectory, sender, and intended receiver; do not skip receiver in AI update. | M2 | ORIGINAL_REQUEST R2 |
| F7 | Active Receiver Intercept Physics | Intended receiver actively computes lead intercept position and sprints toward incoming ball trajectory. | M2 | ORIGINAL_REQUEST R2 |
| F8 | Instant Ball Reception Handoff | Teammate immediately takes ball possession upon arrival (<= 2.2m) without hesitation; fires trap sound and speech announcement. | M2 | ORIGINAL_REQUEST R2 |
| F9 | Debounced Player Switching | Pressing Ctrl / LCtrl / RCtrl switches to nearest outfield defender once per press; current player excluded; debounced at edge level. | M3 | ORIGINAL_REQUEST R3 |
| F10 | Distinct Standing vs Slide Tackle | S triggers standing tackle (1.8m reach, low risk); W/A/D trigger slide tackle (2.8m reach, slide boost, cooldown, foul probability). | M3 | ORIGINAL_REQUEST R3 |
| F11 | Spoken Tackle Distance Announcements | Out-of-range tackles announce exact distance to ball owner (e.g. "Too far: 4.2m"). | M3 | ORIGINAL_REQUEST R3 |
| F12 | Instant Auditory Action Confirmation | Dedicated audio for shots (fix missing ShotTaken sound), passes, crosses, whistles, and tackles with spatial stereo panning. | M3 | ORIGINAL_REQUEST R4 |
| F13 | Solution Compilation & Zero Errors | Solution compiles with 0 errors and 0 warnings using .NET SDK 9.0 (`dotnet build -c Release`). | M4 | ORIGINAL_REQUEST R5 |
| F14 | Headless Simulation Verification | Automated test suite executes via `--test` and exits with code 0 verifying gameplay mechanics. | M4 | ORIGINAL_REQUEST R5 |
| F15 | Desktop Release Deployment | Publish single-file release executable to `C:\Users\mohamed farag\Desktop\AccessibleFootball2026.exe`. | M4 | ORIGINAL_REQUEST R5 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Input Subsystem & Form Focus Architecture | F1, F2, F3, F4 | none | DONE |
| M2 | Pass Transit & Teammate Reception Physics | F5, F6, F7, F8 | M1 | DONE |
| M3 | Defensive Switching, Tackling & Auditory Feedback | F9, F10, F11, F12 | M1, M2 | IN_PROGRESS |
| M4 | E2E Testing, Build & Desktop Deployment | F13, F14, F15 | M1, M2, M3 | PLANNED |

## Interface Contracts

### Input Subsystem ↔ Simulation Engine (`KeyboardInputHandler` ↔ `MatchEngine`)
- `KeyboardInputHandler.RegisterKeyDown(Keys key)`: Queues discrete action if not already held; sets continuous state for movement/sprint.
- `KeyboardInputHandler.RegisterKeyUp(Keys key)`: Clears held state.
- `KeyboardInputHandler.ConsumeCurrentActions(bool hasPossession)`: Returns discrete actions consumed from queue + active continuous actions.
- Output actions: `GameAction` enum flags (`Pass`, `Shoot`, `ThroughBall`, `Cross`, `StandTackle`, `SlideTackle`, `SwitchPlayer`, `Radar`, `MoveUp`, `MoveDown`, `MoveLeft`, `MoveRight`, `Sprint`).

### Ball Physics ↔ Match Engine (`Ball` ↔ `MatchEngine`)
- `Ball.UpdatePhysics(float deltaTime)`: Decelerates using linear physical friction `Math.Max(0f, speed - Deceleration * deltaTime)` instead of 0.94 multiplier.
- `PassTransitInfo`: Struct containing `Player Sender`, `Player IntendedReceiver`, `Vector2 TargetPoint`, `float EstimatedArrivalTime`, `PassType Type`.
- `MatchEngine.ActivePassTransit`: Set when `ExecutePass` or `ExecuteThroughBall` is invoked. Cleared when ball is caught, out of bounds, or intercepted.

### Match Events ↔ Audio Coordinator (`MatchEngine` ↔ `SmartAudioCoordinator`)
- `MatchEventType.BallTrapped`: Fired when receiver takes control; triggers `SoundEffectsPlayer.PlayKick(pan)` and speech announcement.
- `MatchEventType.ShotTaken`: Explicitly handled in audio coordinator with high-power shot sound effect.
- `MatchEventType.TackleAttempted`: Transmits tackle type (Standing / Slide), outcome (Won, Missed, Foul), and distance.

## Code Layout
- `AccessibleFootball2026.App/`
  - `AccessibleFootballWindow.cs`: Window form, key events, listbox focus.
- `AccessibleFootball2026.Core/Input/`
  - `KeyboardInputHandler.cs`: Edge-triggered action queue, key state tracking.
  - `GameAction.cs`: Action enumeration.
- `AccessibleFootball2026.Core/Simulation/`
  - `MatchEngine.cs`: Core match loop, pass transit, AI receiver intercept, defensive switching, tackling.
- `AccessibleFootball2026.Core/Models/`
  - `Ball.cs`: Physical deceleration, 3D ball trajectory.
  - `Player.cs`: Player state, facing direction, movement.
- `AccessibleFootball2026.Core/Audio/`
  - `SoundEffectsPlayer.cs`: SFX generation (kicks, whistles, tackles).
  - `SmartAudioCoordinator.cs`: Event routing and audio prioritization.
