# In-game updates: download and install inside the game

Since version 2026.1 the game can find, download and install official updates itself,
from the dedicated releases repository:

https://github.com/mo1325072-collab/Accessible-Football-Releases

The source/code repository `AccessibleFootball2026` stays exactly as it is; it is not a
release channel and its `build-inputs-*` prerelease remains build inputs, never a game update.

## What the player experiences

- Settings → "التحقق من وجود تحديثات" / "Check for updates" now offers to download and install
  a published update directly, instead of only opening a browser page.
- Nothing downloads or installs silently. The player confirms the download (with its size in MB)
  and confirms the install separately.
- Downloads resume after interruption: a cancelled or failed download continues where it stopped
  on the next attempt, which matters on a limited data bundle.
- Every download is verified against its published SHA256 and size record before anything is
  installed. A failed verification installs nothing and keeps the current version.
- The game version is spoken and shown in the confirmation before any byte is downloaded.

## Two update kinds

1. **Code patch** (`AccessibleFootball2026-CodePatch.zip`, tens of MB): only the fresh
   `AccessibleFootball2026.exe`. Used when the release's patch record says
   `"assets_changed": false`. Audio, chants, commentary, database and settings stay in place.
   This is the bandwidth-saving path for ordinary fixes.
2. **Full installer** (`AccessibleFootball2026-Full-Setup.exe`, the complete Inno Setup wizard,
   Arabic/English): used whenever game content changed. It installs over the existing copy.
   Settings live in `%AppData%` outside the install folder and are preserved either way.

The updater always prefers the code patch when a release ships a valid one, and falls back to
the full installer if the patch record is missing, unreadable or says assets changed.

## How installation stays safe

- A code patch is extracted into `Updates/pending` beside the game and verified again
  (executable size and SHA256 from the record). Only then is it applied.
- Applying replaces only `AccessibleFootball2026.exe`, while the game is not running:
  automatically at the next launch before anything else starts, or immediately through a
  temporary self-copy that waits for the game to exit, swaps the file, and relaunches.
  The old executable is kept as `Updates/AccessibleFootball2026.previous.exe` for rollback.
- Updates never apply during a match; the settings dialog is only reachable from the main menu,
  and the startup auto-check (off by default) only announces, it never downloads.

## Publishing a release (for whoever builds on Windows)

1. Build with `python Build-Complete.py` as usual. Besides the installer it now also writes to
   `build-complete/output`:
   - `AccessibleFootball2026-Full-Setup.exe` + `AccessibleFootball2026-Full-Setup.json`
   - `AccessibleFootball2026-CodePatch.zip` + `AccessibleFootball2026-CodePatch.json`
2. Create a release on the releases repository, e.g. tag `game-v2026.1`, title
   "Accessible Football 2026.1", stable (not draft, not prerelease).
3. Upload all four files as release assets. Include the code patch pair only when audio/data
   content is unchanged since the previous release; otherwise upload installer + installer JSON
   only, and players receive the full installer.
4. Version numbers come from `VERSION` in `Build-Complete.py`,
   `<Version>` in `AccessibleFootball2026.App/AccessibleFootball2026.App.csproj` and
   `GameUpdateChecker.CurrentVersion`; bump all three together per release
   (`game-v<same version>` as the tag).
5. The newest published stable release wins. Keep one active release line; older releases can
   stay published, the game picks the newest by publish date.

Note: the first release (2026.1) is the one that brings the updater itself. Copies installed
before it cannot see the releases repository, so 2026.1 is installed once by hand (or by
building from source); every update after that works from inside the game.

## Repository visibility requirement

Anonymous in-game downloads require the releases repository to be **public**; GitHub does not
serve release assets from a private repository without credentials, and shipping credentials
inside the game would expose them. Until the owner explicitly approves making this second
repository public, keep it private and treat in-game update checks as "no published release".
