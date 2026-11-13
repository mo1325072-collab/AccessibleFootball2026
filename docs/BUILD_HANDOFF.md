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
- Qwen is removed for this phase. All643chants are retained,277are wired across72teams/66grounds; unidentified recordings remain deferred, not guessed or deleted. Five teams remain blocked by roster evidence gaps. The supplied GoalNet matches a generated fallback, and dedicated original keeper-save provenance remains unverified. See the audit for the remaining scope, not a blanket final-game acceptance.

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
