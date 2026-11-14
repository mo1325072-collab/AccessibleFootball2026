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
the full installer if the patch record is missing, unreadable, says assets changed, or does not
name the installed version as its exact `base_version`. A patch is only ever applied to the
baseline it was built from; `"assets_changed": false` alone is never enough, because an install
older than the patch's baseline can carry different audio/data. Every other install takes the
full installer, which can bring any older copy up to date.

## How installation stays safe

- A code patch is extracted into `Updates/pending` beside the game and verified again
  (executable size and SHA256 from the record). Only then is it applied.
- Windows locks the running game's executable, so a process can never swap its own image.
  Applying therefore always happens from a temporary self-copy (the applier): at the next
  launch, before anything else loads, the game hands off to the applier and exits; on
  "install now", the same applier path is used from the main menu. The applier waits until
  the game process has fully exited (up to 60 seconds; if the game is still alive it leaves
  the staged update untouched for the next launch), then swaps in the staged executable and
  relaunches the game with `--after-update-attempt`, so a failed swap can never bounce the
  relaunched game straight back into the applier. The old executable is kept as
  `Updates/AccessibleFootball2026.previous.exe` for rollback.
- Updates never apply during a match; the settings dialog is only reachable from the main menu,
  and the startup auto-check (off by default) only announces, it never downloads.

## Publishing a release (for whoever builds on Windows)

1. Build with `python Build-Complete.py` as usual. Besides the installer it now also writes to
   `build-complete/output`:
   - `AccessibleFootball2026-Full-Setup.exe` + `AccessibleFootball2026-Full-Setup.json`
   - `AccessibleFootball2026-CodePatch.zip` + `AccessibleFootball2026-CodePatch.json`
2. Create a release on the releases repository, e.g. tag `game-v2026.1`, title
   "Accessible Football 2026.1", stable (not draft, not prerelease).
3. Upload the release assets. Include the code patch pair only when audio/data content is
   unchanged since the previous release AND its record's `base_version` names that previous
   version; otherwise upload installer + installer JSON only, and players receive the full
   installer. For 2026.1 specifically: upload ONLY `AccessibleFootball2026-Full-Setup.exe` and
   `AccessibleFootball2026-Full-Setup.json` - the current package pruned 366 unlinked chants and
   reshaped content versus every older install, so no existing copy is asset-compatible with a
   code patch this release, and this build's patch record deliberately carries
   `"base_version": null` so the updater can never offer it. The patch pair returns with the
   next content-unchanged release (`PATCH_BASE_VERSION` set to the previous version).
4. Version numbers come from `VERSION` in `Build-Complete.py`,
   `<Version>` in `AccessibleFootball2026.App/AccessibleFootball2026.App.csproj` and
   `GameUpdateChecker.CurrentVersion`; bump all three together per release
   (`game-v<same version>` as the tag). Set `PATCH_BASE_VERSION` in `Build-Complete.py` to the
   previous release's version: it is written into the patch record as `base_version`, and the
   updater applies the patch only to installs of exactly that version.
5. Release tags use the two-part scheme (`game-v2026.1`, `game-v2026.2`, ...). Three-part
   versions are the retired date-based scheme (2026.11.14 means November 14) and always order
   before the current scheme, so a legacy install is migrated forward and a legacy-scheme tag
   can never pull a current install backwards. The highest-version published stable release
   wins; an older or equal release is never offered as an update, however recently it was
   published, and publish date only breaks a same-version tie.

Note: the first release (2026.1) is the one that brings the updater itself. Copies installed
before it cannot see the releases repository, so 2026.1 is installed once by hand (or by
building from source); every update after that works from inside the game.

## Repository visibility requirement

Anonymous in-game downloads require the releases repository to be **public**; GitHub does not
serve release assets from a private repository without credentials, and shipping credentials
inside the game would expose them. Until the owner explicitly approves making this second
repository public, keep it private and treat in-game update checks as "no published release".
