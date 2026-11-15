"""Build the complete game from this repository and its GitHub input release. No Drive."""
from pathlib import Path
import argparse,hashlib,json,os,shutil,subprocess,sys,urllib.request,zipfile,concurrent.futures
ROOT=Path(__file__).resolve().parent
if not (ROOT/'AccessibleFootball2026.sln').exists():ROOT=ROOT.parent
REPO='mo1325072-collab/AccessibleFootball2026'
RELEASES_REPO='mo1325072-collab/Accessible-Football-Releases'
VERSION='2026.1'
RELEASE_TITLE='Accessible Football 2026.1'
# Installed version this build's code patch was built on. The in-game updater applies the patch
# only to that exact baseline (and unchanged assets); every older or different install takes the
# full installer instead. For 2026.1 no existing install is asset-compatible: the current package
# pruned 366 unlinked chants and reshaped content versus 2026.11.14, so this build's patch carries
# base_version null and is never offered; publish installer + installer JSON only for this release.
# Starting with the next content-unchanged release, set this to the previous release's VERSION.
PATCH_BASE_VERSION=None
TAG='build-inputs-2ef5d009'
SHA='689d01ed08e3328171e11947371354e92553702374e065b586191a1e911371aa'
SIZE=38970719
# Manifest is read from a checked-in, content-hashed record, not a release instruction.
def digest(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def get(url,target):
 req=urllib.request.Request(url,headers={'User-Agent':'AccessibleFootball2026-Build','Accept':'application/vnd.github+json'})
 tmp=target.with_suffix(target.suffix+'.tmp')
 with urllib.request.urlopen(req,timeout=120) as r,tmp.open('wb') as w:shutil.copyfileobj(r,w,1024*1024)
 tmp.replace(target)
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--prepare-only',action='store_true');p.add_argument('--stage-only',action='store_true',help='Build and verify the full installer payload without invoking Inno; not an installer');p.add_argument('--code-only',action='store_true',help='Build a code-only patch ZIP without reading or downloading audio');p.add_argument('--iscc');p.add_argument('--dotnet',default='dotnet');p.add_argument('--input-zip',type=Path,help='Optional predownloaded verified input ZIP; normally automatic GitHub download')
 a=p.parse_args();work=ROOT/'build-complete';work.mkdir(exist_ok=True);parts=work/'download';parts.mkdir(exist_ok=True)
 if a.code_only:
  code=work/'code-only';code.mkdir(exist_ok=True)
  subprocess.run([a.dotnet,'publish',str(ROOT/'AccessibleFootball2026.App'),'--configuration','Release','--runtime','win-x64','--self-contained','true','-p:EnableWindowsTargeting=true','--output',str(code)],check=True,cwd=ROOT)
  exe=code/'AccessibleFootball2026.exe'
  if not exe.is_file():raise SystemExit('No rebuilt executable produced')
  patch=work/'AccessibleFootball2026-CodePatch.zip'
  with zipfile.ZipFile(patch,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:z.write(exe,exe.name)
  record={'artifact':patch.name,'bytes':patch.stat().st_size,'sha256':digest(patch),'exe_bytes':exe.stat().st_size,'exe_sha256':digest(exe),'assets_changed':False,'version':VERSION,'native_windows_acceptance':False}
  patch.with_suffix('.json').write_text(json.dumps(record,indent=2))
  print(json.dumps(record,indent=2));print('Close the existing game, back up its EXE, and replace only AccessibleFootball2026.exe in the installed game folder. Keep Assets, database, DLLs and settings. This is not an automatic updater or completed native acceptance test.')
  return

 m={'archive_bytes': 38970719, 'archive_sha256': '689d01ed08e3328171e11947371354e92553702374e065b586191a1e911371aa', 'parts': [{'name': 'AF2026-ExtraAudio.part001.bin', 'bytes': 8388608, 'sha256': 'd1c973b764e6a12739bb5c392a440f8ec6a26ca4c6ffb8ab93d6b8b6ff2e7a29'}, {'name': 'AF2026-ExtraAudio.part002.bin', 'bytes': 8388608, 'sha256': '684dc1c9491bcf6e7fa7d6dfb968d7ce35e0457e6d7b17fef976b8edaa6a511d'}, {'name': 'AF2026-ExtraAudio.part003.bin', 'bytes': 8388608, 'sha256': '926647d2a71500d1eadd4d2b1941f89e43f7dfcee1598ef9697749fa9689afa6'}, {'name': 'AF2026-ExtraAudio.part004.bin', 'bytes': 8388608, 'sha256': '0ca9c16dcc3ed8e23f8f34a44091d369561e56f14cd75b9aa02a719d91fc4d3e'}, {'name': 'AF2026-ExtraAudio.part005.bin', 'bytes': 5416287, 'sha256': 'a290e8b0c8a92591979afe3a2510af6627a0c2f5bc17fbb3419de6b95e07b67e'}]}
 archive=a.input_zip.resolve() if a.input_zip else work/'additional-content.zip'
 if not archive.exists() or archive.stat().st_size!=SIZE or digest(archive)!=SHA:
  if a.input_zip:raise SystemExit('Provided input ZIP hash mismatch')
  release=parts/'release.json';get(f'https://api.github.com/repos/{REPO}/releases/tags/{TAG}',release)
  assets=json.loads(release.read_text())['assets']
  def download(r):
   path=parts/r['name']
   if path.exists() and path.stat().st_size==r['bytes'] and digest(path)==r['sha256']:return
   matches=[x for x in assets if x['name'].endswith(r['name']) and x['size']==r['bytes']]
   if len(matches)!=1:raise RuntimeError('Missing or ambiguous GitHub asset: '+r['name'])
   get(matches[0]['browser_download_url'],path)
   if path.stat().st_size!=r['bytes'] or digest(path)!=r['sha256']:raise RuntimeError('GitHub asset failed hash: '+r['name'])
   print('Verified',r['name'],flush=True)
  with concurrent.futures.ThreadPoolExecutor(max_workers=4) as ex:list(ex.map(download,m['parts']))
  temp=archive.with_suffix('.tmp')
  with temp.open('wb') as w:
   for r in m['parts']:
    with (parts/r['name']).open('rb') as f:shutil.copyfileobj(f,w,1024*1024)
  if temp.stat().st_size!=SIZE or digest(temp)!=SHA:raise SystemExit('Joined input ZIP hash mismatch')
  temp.replace(archive)
 data=work/'verified-input';data.mkdir(exist_ok=True)
 with zipfile.ZipFile(archive) as z:
  plan=json.loads(z.read('content-plan.json'))
  for r in plan['repository_files']+plan['additional_files']:
   target=(data/r['path']).resolve()
   if not target.is_relative_to(data.resolve()):raise SystemExit('Unsafe content path')
   if target.exists() and target.stat().st_size==r['bytes'] and digest(target)==r['sha256']:continue
   target.parent.mkdir(parents=True,exist_ok=True)
   if 'repository_path' in r:
    source=ROOT/r['repository_path']
    if not source.is_file():raise SystemExit('Missing repository content. Clone/download full source, not just code: '+r['repository_path'])
    shutil.copy2(source,target)
   else:
    with z.open(r['path']) as f,target.open('wb') as w:shutil.copyfileobj(f,w,1024*1024)
   if target.stat().st_size!=r['bytes'] or digest(target)!=r['sha256']:raise SystemExit('Content failed hash: '+r['path'])
 print('Verified',len(plan['repository_files'])+len(plan['additional_files']),'audio/support files.',flush=True)
 print('All input files verified. No game executed.',flush=True)
 if a.prepare_only:return
 if os.name!='nt' and not a.stage_only:raise SystemExit('Verified input preparation complete. Run the installer build on Windows, or use --stage-only for payload checks.')
 # Derive linked distribution chants from the same production resolver, not a copied list.
 inventory_path=work/'package-audio-inventory.json'
 subprocess.run([a.dotnet,'run','--project',str(ROOT/'tools'/'PackageAudit'),'-c','Release','--',str(data/'Assets'/'Audio'),str(inventory_path)],check=True,cwd=ROOT)
 inventory=json.loads(inventory_path.read_text())
 linked=set(inventory['linked_chants'])
 game=work/'game'
 # Rebuild only this generated staging directory so stale excluded files cannot leak in.
 if game.exists():shutil.rmtree(game)
 game.mkdir()

 subprocess.run([a.dotnet,'publish',str(ROOT/'AccessibleFootball2026.App'),'--configuration','Release','--runtime','win-x64','--self-contained','true','-p:EnableWindowsTargeting=true','--output',str(game)],check=True,cwd=ROOT)
 excluded=[]
 for name in ('Assets','ThirdParty'):
  if not (data/name).exists():continue
  for source in (data/name).rglob('*'):
   if not source.is_file():continue
   relative=source.relative_to(data).as_posix()
   audio_relative=relative.removeprefix('Assets/Audio/')
   if audio_relative.startswith('SFX/Chants/') and source.suffix.lower() in ('.wav','.mp3') and audio_relative not in linked:
    excluded.append({'path':relative,'bytes':source.stat().st_size});continue
   target=game/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
 shutil.copy2(ROOT/'AccessibleFootball2026.Core'/'Data'/'players_database.json',game/'players_database.json')
 # Verify every retained staged payload against source-of-truth bytes, not just source existence.
 expected=[r for r in plan['repository_files']+plan['additional_files'] if r['path'] not in {e['path'] for e in excluded}]
 for r in expected:
  target=game/r['path']
  if not target.is_file() or target.stat().st_size!=r['bytes'] or digest(target)!=r['sha256']:raise SystemExit('Staged content missing/corrupt: '+r['path'])
 for relative in inventory['required']+inventory['linked_chants']:
  if not (game/'Assets'/'Audio'/relative).is_file():raise SystemExit('Staged runtime sound path missing: '+relative)
 # Re-run the actual resolver at its installed relative layout. No original source files are removed.
 subprocess.run([a.dotnet,'run','--project',str(ROOT/'tools'/'PackageAudit'),'-c','Release','--',str(game/'Assets'/'Audio'),str(work/'staged-audio-inventory.json')],check=True,cwd=ROOT)
 staged_inventory=json.loads((work/'staged-audio-inventory.json').read_text())
 if staged_inventory!=inventory:raise SystemExit('Installed-layout chant/sound resolution changed')
 reduction={'excluded_unlinked_chants':len(excluded),'excluded_bytes':sum(r['bytes'] for r in excluded),'linked_chants_retained':len(linked),'retained_verified_content_files':len(expected),'required_runtime_paths_verified':len(inventory['required']),'all_commentary_retained':True,'native_windows_acceptance':False,'installer_compiled':False}
 (work/'package-verification.json').write_text(json.dumps(reduction,indent=2));print(json.dumps(reduction,indent=2),flush=True)

 # Hash the freshly rebuilt game, never attach the old binary's manifest to changed source.
 files=[{'path':p.relative_to(game).as_posix(),'bytes':p.stat().st_size,'sha256':digest(p)} for p in sorted(game.rglob('*')) if p.is_file() and p.name!='manifest.json']
 try:source=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
 except (OSError,subprocess.CalledProcessError):source='source archive; git commit unavailable'
 (game/'manifest.json').write_text(json.dumps({'source_commit':source,'input_sha256':SHA,'files':files},indent=2))
 if a.stage_only:print('Full payload staged and verified. No installer compiled or native game executed.');return
 iscc=a.iscc or shutil.which('ISCC.exe')
 if not iscc:
  candidates=[Path(os.environ.get(x,''))/'Inno Setup 6'/'ISCC.exe' for x in ('ProgramFiles(x86)','ProgramFiles')]
  iscc=next((str(p) for p in candidates if p.is_file()),None)
 if not iscc:raise SystemExit('Game rebuilt with all assets. Install official Inno Setup6.5.0+ or supply --iscc ISCC.exe.')
 if not (Path(iscc).resolve().parent/'Languages'/'Arabic.isl').is_file():raise SystemExit('Arabic installer messages missing. Install official Inno Setup6.5.0 or newer, then retry.')
 iss=work/'complete.iss';iss.write_text('[Setup]\nAppId=AccessibleFootball2026-Full\nAppName=Accessible Football\nAppVersion='+VERSION+'\nDefaultDirName={localappdata}\\AccessibleFootball2026\nDefaultGroupName=Accessible Football\nPrivilegesRequired=lowest\nOutputDir=output\nOutputBaseFilename=AccessibleFootball2026-Full-Setup\nCompression=lzma2/fast\nSolidCompression=no\nDiskSpanning=no\nDisableDirPage=no\nDisableProgramGroupPage=no\nDisableWelcomePage=no\nShowLanguageDialog=yes\nUsePreviousLanguage=no\n[Languages]\nName: "ar"; MessagesFile: "compiler:Languages\\Arabic.isl"\nName: "en"; MessagesFile: "compiler:Default.isl"\n[Files]\nSource: "'+str(game)+'\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs\n[Tasks]\nName: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"\n[Icons]\nName: "{group}\\Accessible Football"; Filename: "{app}\\AccessibleFootball2026.exe"; WorkingDir: "{app}"\nName: "{autodesktop}\\Accessible Football"; Filename: "{app}\\AccessibleFootball2026.exe"; WorkingDir: "{app}"; Tasks: desktopicon\n',encoding='utf-8')
 subprocess.run([iscc,str(iss)],check=True)
 exe=work/'output'/'AccessibleFootball2026-Full-Setup.exe'
 if not exe.is_file() or list(exe.parent.glob('*.bin')):raise SystemExit('Not a complete one-file installer')
 reduction['installer_compiled']=True;reduction['installer_bytes']=exe.stat().st_size;reduction['installer_sha256']=digest(exe)
 (work/'package-verification.json').write_text(json.dumps(reduction,indent=2))
 print('BUILT',exe,'bytes',exe.stat().st_size,'SHA256',digest(exe))

 # Emit the in-game updater's release assets next to the installer: its hash record, plus the
 # code-only patch (fresh EXE from this exact build) for players whose audio/data is unchanged.
 setup_record={'artifact':exe.name,'bytes':exe.stat().st_size,'sha256':digest(exe),'assets_changed':True,'version':VERSION}
 (work/'output'/'AccessibleFootball2026-Full-Setup.json').write_text(json.dumps(setup_record,indent=2))
 built_exe=game/'AccessibleFootball2026.exe'
 patch=work/'output'/'AccessibleFootball2026-CodePatch.zip'
 with zipfile.ZipFile(patch,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:z.write(built_exe,built_exe.name)
 patch_record={'artifact':patch.name,'bytes':patch.stat().st_size,'sha256':digest(patch),'exe_bytes':built_exe.stat().st_size,'exe_sha256':digest(built_exe),'assets_changed':PATCH_BASE_VERSION is None,'base_version':PATCH_BASE_VERSION,'version':VERSION}
 (work/'output'/'AccessibleFootball2026-CodePatch.json').write_text(json.dumps(patch_record,indent=2))
 print('Release assets ready in build-complete/output: installer, installer JSON, code patch, patch JSON.')
 print('Publish them as release "'+RELEASE_TITLE+'" (tag game-v'+VERSION+') on https://github.com/'+RELEASES_REPO+'/releases so the in-game updater can find and verify them.')
 print('Include the code patch assets only when audio/data content is unchanged since the previous release; otherwise publish installer + installer JSON only.')
 print('Still requires clean offline Windows/NVDA/audio/controls acceptance. No game was launched.')
if __name__=='__main__':main()
