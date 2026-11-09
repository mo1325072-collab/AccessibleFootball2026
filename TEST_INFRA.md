# E2E Test Infra: AccessibleFootball2026

## Test Philosophy
- Opaque-box, requirement-driven.
- Automated simulation suite verification (`--test`), headless simulation loop, event tracing.
- Covers R1 through R5: input edge-triggering, pass reception, defensive switching & tackling, audio confirmation, and clean build/deployment.

## Feature Inventory Mapping
| # | Feature | Source | Tier 1 | Tier 2 | Tier 3 |
|---|---------|--------|:------:|:------:|:------:|
| F1 | Edge-Triggered Action Queue | ORIGINAL_REQUEST R1 | 5 | 5 | ✓ |
| F2 | Continuous Movement & Sprint | ORIGINAL_REQUEST R1 | 5 | 5 | ✓ |
| F3 | Form KeyDown/KeyUp KeyPreview | ORIGINAL_REQUEST R1 | 5 | 5 | ✓ |
| F4 | NVDA Focus Mode Retention | ORIGINAL_REQUEST R1 | 5 | 5 | ✓ |
| F5 | Ball Ground Friction Deceleration | ORIGINAL_REQUEST R2 | 5 | 5 | ✓ |
| F6 | Pass Transit State Tracking | ORIGINAL_REQUEST R2 | 5 | 5 | ✓ |
| F7 | Active Receiver Intercept Physics | ORIGINAL_REQUEST R2 | 5 | 5 | ✓ |
| F8 | Instant Ball Reception Handoff | ORIGINAL_REQUEST R2 | 5 | 5 | ✓ |
| F9 | Debounced Player Switching | ORIGINAL_REQUEST R3 | 5 | 5 | ✓ |
| F10 | Distinct Standing vs Slide Tackle | ORIGINAL_REQUEST R3 | 5 | 5 | ✓ |
| F11 | Spoken Tackle Distance Announcements | ORIGINAL_REQUEST R3 | 5 | 5 | ✓ |
| F12 | Instant Auditory Action Confirmation | ORIGINAL_REQUEST R4 | 5 | 5 | ✓ |
| F13 | Solution Compilation & Zero Errors | ORIGINAL_REQUEST R5 | 5 | 5 | ✓ |
| F14 | Headless Simulation Verification | ORIGINAL_REQUEST R5 | 5 | 5 | ✓ |
| F15 | Desktop Release Deployment | ORIGINAL_REQUEST R5 | 5 | 5 | ✓ |

## Test Architecture
- Test runner: `AccessibleFootball2026.exe --test`
- Build verification: `$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"; dotnet build -c Release`
- Output verification: exit code 0, check log for draft, audio, simulation checks.
