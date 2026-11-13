# AccessibleFootball2026: exact build handoff

## Status, 13 November 2026

The current repaired game source is published. The full audio/data/runtime payload is privately preserved and round-trip hash verified. A complete single EXE has NOT been built or tested. These are builder inputs, not a ready game release.

Runtime source commit: https://github.com/mo1325072-collab/AccessibleFootball2026/commit/2ef5d0091c2e4f67aca6a2edb3d4af932330db16

Complete builder inputs are stored in a private owner Drive folder. Obtain that folder from the project owner.

The folder is restricted to the project owner. Its links are NOT public. The other coding tool must use the owner's authorized Google Drive session/integration. If that tool lacks owner Drive access, authentication is required; do not bypass it, publish the files, or ask the owner to re-upload 70 parts. Download the folder in one operation through authenticated Google Drive, or have the authorized tool fetch the file IDs in parts-manifest.json automatically. Extract Drive's outer ZIP if it supplies one. No per-part user action is needed.

## Inputs and verification

- 70 numbered binary parts, total 1,755,013,306 bytes, all uploaded privately and downloaded again for hash verification.
- Joined ZIP SHA256: 135b02c97c20a53059a6def66f4287f9291d35b0b02d650f62481eea5ec0f90b
- 1,821 manifest-listed game payload files; 1,814 audio/support files under Assets/Audio. manifest.json is the additional integrity record.
- Source snapshot and BuildKit are small separate archives in the same private folder. The snapshot omits original SFX duplicates because all game-ready audio is already in the complete payload. GitHub retains the supplied SFX source archive tree.
- Do not replace this payload with a GitHub source ZIP or a framework-dependent publish folder. That would omit required audio or runtime files.

## Exact instructions for the other coding tool

1. Get the private folder with the owner's authorized Drive access. Unzip AF2026-BuildKit.zip. Use Python 3.9 or later and a Windows build machine with the official Inno Setup compiler installed. Do not send passwords or access tokens in chat.
2. Run `python assemble.py <downloaded-parts-folder> <output-folder>`. It verifies each part, joins them in manifest order, verifies the complete ZIP SHA256, then extracts and verifies every payload file. It never starts the game. Stop on any mismatch. You can alternatively use the PowerShell wrapper below, which performs these checks first.
3. Preferred next packaging route: build a single Inno Setup installer containing the verified, already self-contained game directory. This avoids embedding 1.75GB through the C# compiler. This route is supplied for the next builder and is NOT claimed tested here. Run in Windows PowerShell: `./Build-OneExe.ps1 -Iscc 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' -PartsDir '<downloaded-parts-folder>'`. Supply the actual installed compiler path if different. Expected result is `output/AccessibleFootball2026-Full-Setup.exe` with no external .bin files. It installs all bundled content automatically under the current user's LocalAppData, without requiring external database or audio folders. It is an installer, not a zero-extraction portable EXE. No installer has yet been produced.
4. If rebuilding game code, clone the repository, check out the exact runtime commit above, and publish `AccessibleFootball2026.App` for `win-x64`, Release, `--self-contained true`, with .NET 8. Preserve the complete verified Assets/Audio and bundled database from the payload. Recreate all manifest hashes after any rebuild and rerun the regression harnesses. Never reuse the old manifest for changed binaries.
5. Acceptance before delivery: install the resulting ONE EXE on a clean Windows x64 machine without .NET installed, with internet disabled and no source tree/audio folders present. Verify installed files against the manifest before launching. Test NVDA and SAPI speech, menu music and approved Move/Select sounds, MCI commentary, team/database loading, focused and held/combo keys, halftime goal direction, heading-relative beacon, and VAR pause/announcements/results. No native Windows/NVDA/MCI execution has been performed in this handoff. Test installer accessibility, disk-space failure and uninstall, too. Return the final EXE's size and SHA256 and upload that exact EXE privately. Do not label this source/payload kit a finished EXE.

## Failed alternative retained for diagnosis

FullGameLauncher contains the actual C# self-extracting launcher source attempted here. It embeds FullGame.zip, automatically extracts to LocalAppData, verifies payload hashes and launches the game. Its command was `dotnet publish FullGameLauncher -c Release -r win-x64 --self-contained true -p:EnableWindowsTargeting=true -p:GamePayload=<full-verified-ZIP> -o <output-directory>`. The real compiler result was MSB6006: csc.dll exited with code137 while embedding the 1.75GB resource. No usable all-in-one EXE resulted. Do not repeatedly retry this memory-heavy route unchanged. The Inno route above is a different, untested packaging proposal, not a completed build.

## Scope and remaining limitations

Qwen/model probing is removed for this phase; tactical advice is coded. Beacon direction uses player heading, quiet midfield and gradual emergence around40m. Input fixes cover held aim and independent left/right Ctrl/Shift. VAR includes announced review, paused play and speech-completion gating; the simulation has no invented video/handball evidence. Goal, offside, penalty and direct-red paths are wired; mistaken identity and clearly wrong second-caution corrections have explicit observed-incident APIs, not random fabricated mistakes. 1,890 default portable assertions and131 separate full-corpus commentary checks passed. These are not a substitute for Windows acceptance.

All643 chant recordings are retained;277 are wired across72teams/66grounds. Remaining identities are deferred, not deleted or guessed. Five teams remain blocked by actual roster evidence gaps. Supplied GoalNet audio matches a generated fallback; a dedicated original keeper-save recording remains unverified. Approved music/menu sounds and raw original database bytes are preserved. Source-data gaps are documented in the repository audit.

## Packaging references

- https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/
- https://jrsoftware.org/ishelp/topic_setup_diskspanning.htm
- https://jrsoftware.org/ishelp/topic_filessection.htm
- https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm

DiskSpanning=no retains compressed files inside Setup.exe; the documented format ceiling for that option is4,200,000,000compressed bytes. The next builder must still check actual compiler output. Do not infer a successfully built installer from this format allowance.

## BuildKit source files

### assemble.py

```python
"""Join owner-authorized downloaded Drive parts, then verify all files. Never run the game."""
from pathlib import Path
import hashlib,json,sys,zipfile,shutil
kit=Path(__file__).resolve().parent
parts=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else kit
out=Path(sys.argv[2]).resolve() if len(sys.argv)>2 else kit/'verified'
m=json.loads((kit/'parts-manifest.json').read_text())
out.mkdir(parents=True,exist_ok=True)
archive=out/m['archive_name'];temp=archive.with_suffix('.tmp')
def digest(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
with temp.open('wb') as w:
 for r in m['parts']:
  found=list(parts.rglob(r['name']))
  if len(found)!=1:raise SystemExit('Need exactly one part: '+r['name'])
  p=found[0]
  if p.stat().st_size!=r['bytes'] or digest(p)!=r['sha256']:raise SystemExit('Part hash mismatch: '+r['name'])
  with p.open('rb') as f:shutil.copyfileobj(f,w,1024*1024)
if temp.stat().st_size!=m['archive_bytes'] or digest(temp)!=m['archive_sha256']:raise SystemExit('Joined archive hash mismatch')
temp.replace(archive)
with zipfile.ZipFile(archive) as z:
 manifest=json.loads(z.read('AccessibleFootball2026/manifest.json'))
 if manifest['source_commit']!='2ef5d0091c2e4f67aca6a2edb3d4af932330db16':raise SystemExit('Wrong source version')
 for r in manifest['files']:
  name='AccessibleFootball2026/'+r['path'];target=(out/name).resolve()
  if not target.is_relative_to(out):raise SystemExit('Unsafe archive path')
  h=hashlib.sha256();size=0;target.parent.mkdir(parents=True,exist_ok=True)
  with z.open(name) as f,target.open('wb') as w:
   for b in iter(lambda:f.read(1024*1024),b''):h.update(b);size+=len(b);w.write(b)
  if size!=r['bytes'] or h.hexdigest()!=r['sha256']:raise SystemExit('Payload hash mismatch: '+r['path'])
 (out/'AccessibleFootball2026'/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('VERIFIED: 70 parts, full ZIP and',len(manifest['files']),'payload files. No program executed.')
print('Installer source directory:',out/'AccessibleFootball2026')
```

### Build-OneExe.ps1

```powershell
param([Parameter(Mandatory=$true)][string]$Iscc,[string]$PartsDir=$PSScriptRoot)
$ErrorActionPreference='Stop'
if (!(Test-Path -LiteralPath $Iscc)) { throw 'Supply the installed Inno Setup ISCC.exe path.' }
& python "$PSScriptRoot\assemble.py" $PartsDir "$PSScriptRoot\verified"
if ($LASTEXITCODE -ne 0) { throw 'Assembly/hash verification failed. Stop.' }
$GameDir=(Resolve-Path "$PSScriptRoot\verified\AccessibleFootball2026").Path
& $Iscc "/DGameDir=$GameDir" "$PSScriptRoot\FullGame.iss"
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed. Do not claim a complete EXE.' }
$Exe=Join-Path $PSScriptRoot 'output\AccessibleFootball2026-Full-Setup.exe'
if (!(Test-Path $Exe)) { throw 'Expected setup executable is missing.' }
if (Get-ChildItem "$PSScriptRoot\output" -Filter '*.bin') { throw 'Not a single-file installer: unexpected external slices.' }
Get-FileHash -Algorithm SHA256 $Exe
Write-Host 'Compilation finished. Still requires clean Windows/NVDA/offline acceptance. No game executed.'
```

### FullGame.iss

```ini
#ifndef GameDir
  #error Supply /DGameDir=the_verified_game_directory
#endif
[Setup]
AppId=AccessibleFootball2026-Candidate-2ef5d009
AppName=Accessible Football 2026 (Test Candidate)
AppVersion=2026.11.13
DefaultDirName={localappdata}\AccessibleFootball2026\Candidate-2ef5d009
DefaultGroupName=Accessible Football 2026
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename=AccessibleFootball2026-Full-Setup
Compression=lzma2/fast
SolidCompression=no
DiskSpanning=no
DisableDirPage=yes
DisableProgramGroupPage=yes
Uninstallable=yes
[Files]
Source: "{#GameDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\Accessible Football 2026"; Filename: "{app}\AccessibleFootball2026.exe"; WorkingDir: "{app}"
[Run]
Filename: "{app}\AccessibleFootball2026.exe"; WorkingDir: "{app}"; Description: "Start Accessible Football 2026"; Flags: nowait postinstall skipifsilent unchecked
```

### FullGameLauncher/FullGameLauncher.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>WinExe</OutputType><TargetFramework>net8.0-windows</TargetFramework><UseWindowsForms>true</UseWindowsForms><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><PublishSingleFile>true</PublishSingleFile><IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract><AssemblyName>AccessibleFootball2026-FullGame</AssemblyName></PropertyGroup>
<ItemGroup><EmbeddedResource Include="$(GamePayload)" LogicalName="FullGame.zip" /></ItemGroup>
<Target Name="RequirePayload" BeforeTargets="BeforeBuild"><Error Condition="!Exists('$(GamePayload)')" Text="GamePayload must name the verified complete game ZIP." /></Target>
</Project>
```

### FullGameLauncher/Program.cs

```csharp
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;
internal static class Program
{
    [STAThread] static int Main()
    {
        try
        {
            using var payload=Assembly.GetExecutingAssembly().GetManifestResourceStream("FullGame.zip") ?? throw new IOException("Bundled game payload missing.");
            using var zip=new ZipArchive(payload,ZipArchiveMode.Read);
            var item=zip.Entries.Single(e=>e.FullName.EndsWith("/manifest.json",StringComparison.Ordinal));
            using var stream=item.Open();using var manifest=JsonDocument.Parse(stream);
            string commit=manifest.RootElement.GetProperty("source_commit").GetString()!;
            if(commit.Length!=40 || commit.Any(c=>!Uri.IsHexDigit(c)))throw new IOException("Invalid game version.");
            string cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AccessibleFootball2026","Versions",commit);
            Directory.CreateDirectory(cache);
            using var gate=new FileStream(Path.Combine(cache,".extract.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            string root=Path.Combine(cache,"AccessibleFootball2026");
            foreach(var file in manifest.RootElement.GetProperty("files").EnumerateArray())
            {
                string relative=file.GetProperty("path").GetString()!;
                string target=Path.GetFullPath(Path.Combine(root,relative));
                if(!target.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Unsafe bundled path.");
                string expected=file.GetProperty("sha256").GetString()!;
                bool Valid(){if(!File.Exists(target))return false;using var f=File.OpenRead(target);return Convert.ToHexString(SHA256.HashData(f)).Equals(expected,StringComparison.OrdinalIgnoreCase);}
                if(Valid())continue;
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var entry=zip.GetEntry("AccessibleFootball2026/"+relative)??throw new IOException("Missing bundled asset: "+relative);
                string temp=target+".tmp";using(var input=entry.Open())using(var output=File.Create(temp))input.CopyTo(output);
                File.Move(temp,target,true);if(!Valid())throw new IOException("Bundled asset failed verification: "+relative);
            }
            string exe=Path.Combine(root,"AccessibleFootball2026.exe");
            Process.Start(new ProcessStartInfo(exe){WorkingDirectory=root,UseShellExecute=true});return 0;
        }
        catch(Exception error){MessageBox.Show("تعذر تجهيز اللعبة الكاملة. تحقق من المساحة المتاحة وأعد التشغيل.\nCould not prepare the full game. Check free disk space and retry.\n"+error.Message,"AccessibleFootball2026");return 1;}
    }
}
```
