# Build the complete game from GitHub

All required game content is now on this repository and its build-input attachments. No Google Drive is needed. This replaces the earlier split handoff. These are source/build inputs, not a finished single-EXE release.

## One command

Get the complete repository, including PES2013_SFX, then run this from its root on Windows:

```powershell
python Build-Complete.py
```

Build prerequisites: Python3.9+, .NET8SDK, and the official Inno Setup compiler. The script locates ISCC.exe in PATH or the usual Inno Setup6installation folders. If necessary, supply the real location:

```powershell
python Build-Complete.py --iscc 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
```

The script automatically downloads the five small extra-content pieces from this repository's `build-inputs-2ef5d009` input release, joins and SHA256-verifies them, maps and verifies870existing repository files plus944extra files, rebuilds current source as a self-contained win-x64game, copies every required audio/data file, generates fresh output hashes, and asks Inno Setup to build one installer EXE. No user should download or join individual pieces by hand. It never launches the game.

Expected output after a successful Windows build:

`build-complete/output/AccessibleFootball2026-Full-Setup.exe`

The installer includes all game content and automatically installs it under LocalAppData. Game players do not need Python, the SDK, Inno, source folders, Drive, an external database or manual audio-folder selection. Build prerequisites are only for the person/tool compiling it. This is a single installer with extraction, not a no-extraction portable EXE.

## Completeness and status

- Original repaired runtime checkpoint: `2ef5d0091c2e4f67aca6a2edb3d4af932330db16`. Later commits add build support without replacing the runtime.
-870original audio/support files already exist in `PES2013_SFX`. The extra archive adds944files, including full commentary, approved menu music/Move/Select and current catalogs. All1,814content files have expected SHA256values in the authenticated extra archive's content plan.
- Extra archive:38,970,719bytes. SHA256:`689d01ed08e3328171e11947371354e92553702374e065b586191a1e911371aa`.
- The local content preparation path has verified all1,814files. The user has chosen to leave downloading and building to his other coding tool. Inno compilation and clean Windows/NVDA/audio acceptance have NOT been completed here. Failures must stop the build, not produce a falsely labeled complete EXE.
- Before delivery, test the resulting installer offline on clean Windows x64 without .NET installed or source/audio folders present. Check NVDA/SAPI, menu audio, commentary, focused/held/combo controls, halftime beacon direction and VAR pause/speech/results. Verify installed files against its freshly generated manifest.
- Qwen is removed for this phase. All643chants are retained,277are wired across72teams/66grounds; unidentified recordings remain deferred, not guessed or deleted. Medina remains blocked by missing verified keeper evidence; other source squads remain incomplete. The supplied GoalNet matches a generated fallback, and dedicated original keeper-save provenance remains unverified. See the audit for the remaining scope, not a blanket final-game acceptance.

## Notes for the coding tool

Use repository bytes and attached GitHub content only. Do not use the obsolete Drive workflow, substitute a source-only publish folder, ask the owner to re-upload audio, or repeat the failed C# giant-resource embedding attempt. Rebuild the current source, not the old bundled binary. If the owner wants later code edits, update this source normally and keep all audio/data packaging and checks in place. Preserve the approved sounds and accessibility behavior.

For content-only verification, without compiling or launching the game:

```powershell
python Build-Complete.py --prepare-only
```

The GitHub API lists the input assets; the script verifies sizes, hashes and exact expected suffixes rather than relying on Drive credentials or manually invented per-part download links. Archive tampering or missing files causes a hard failure.

Packaging references:
- https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/
- https://jrsoftware.org/ishelp/topic_setup_diskspanning.htm
- https://jrsoftware.org/ishelp/topic_filessection.htm
- https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm

## November14playtest source repairs

See `PLAYTEST_REPAIR_2026-11-14.md` for the audio/intro fixes, update-check release policy, Arabic/English setup changes and native verification limits. Use Inno Setup6.5.0+ for its bundled official Arabic translation. No ready game patch is implied by the build-input release.

For a code-only patch with no audio download or re-packaging, the builder can run `python Build-Complete.py --code-only`. This produces `build-complete/AccessibleFootball2026-CodePatch.zip` plus a SHA256/size JSON record. It contains only the freshly rebuilt EXE, leaving existing Assets, database, DLLs and settings in place. Close the game and back up the old EXE before replacement. This does not publish the patch or test native Windows output.

## Package repair and smaller chant payload, November 14
The normal full build now stages into a fresh generated `build-complete/game` directory and verifies every retained input file's size/hash after copying. It derives chant inclusion from the actual production chant resolver and original index, not a second manually maintained list. Only unlinked files inside SFX/Chants are excluded from distribution. Original repository/download assets and every commentary recording remain retained. Linked Egypt recording and all other working chant pools stay included.

Current measured payload reduction: 366 unlinked chant WAVs excluded, 805,415,588 uncompressed bytes saved; 277 linked chant recordings retained. This is payload size, not a measured final installer compression saving. The builder verifies 1,448 retained content files, including nine semantic cues, exact approved Move/Select, whistle and kick paths, GoalBeacon, catalogs and intentional lineup music. A missing required sound or linked pool fails the build. Startup also checks mandatory packaged paths before initialization can create any fallback.

`python Build-Complete.py --stage-only` rebuilds and verifies the full installed-relative payload on a supported build host without invoking Inno. It is a diagnostic build mode, not an installer or substitute for the normal full command. `package-verification.json` records the exclusion count, bytes and verification boundary. Staging resolution and native command contract checks do not prove installed Windows audible playback. The normal Windows command remains `python Build-Complete.py`.

Startup hides menus while its theme is active; natural completion opens the main menu and Enter/Space skips early. Intentional pre-match lineup music is separate and remains skippable/naturally completing. No unsupported continuous musical crowd bed is reintroduced.

Staged payload verification also ran after relocation to a separate installed-style folder. The nine semantic previews, Move/Select, pass/shot/cross/whistle and GoalBeacon resolved from that folder and reached their recorded command routes. Removing TackleWon from the relocated package caused a hard pre-initialization failure without regenerating it. Existing pause/exit/commentary and live-match menu-music contract tests remain passing. These checks do not replace compiling/running Inno or listening on Windows.

## Startup completion clarification
Latest user clarification supersedes the earlier explicit-key-only interpretation: while the startup theme is active no menu is exposed; natural completion automatically opens the main menu. Enter or Space skips early, with the existing release latch preventing held-key activation of the newly opened menu. A failed intro start also goes to the menu rather than leaving an indefinite silent gate. Pre-match opening music already completes naturally into gameplay and remains early-skippable. Replay timing and Enter confirmation are unchanged.
