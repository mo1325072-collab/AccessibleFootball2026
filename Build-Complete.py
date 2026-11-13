"""Build the complete game from this repository and its GitHub input release. No Drive."""
from pathlib import Path
import argparse,hashlib,json,os,shutil,subprocess,sys,urllib.request,zipfile,concurrent.futures
ROOT=Path(__file__).resolve().parent
if not (ROOT/'AccessibleFootball2026.sln').exists():ROOT=ROOT.parent
REPO='mo1325072-collab/AccessibleFootball2026'
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
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--prepare-only',action='store_true');p.add_argument('--iscc');p.add_argument('--dotnet',default='dotnet');p.add_argument('--input-zip',type=Path,help='Optional predownloaded verified input ZIP; normally automatic GitHub download')
 a=p.parse_args();work=ROOT/'build-complete';work.mkdir(exist_ok=True);parts=work/'download';parts.mkdir(exist_ok=True)
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
 if os.name!='nt':raise SystemExit('Verified input preparation complete. Run the installer build on Windows.')
 game=work/'game';game.mkdir(exist_ok=True)
 subprocess.run([a.dotnet,'publish',str(ROOT/'AccessibleFootball2026.App'),'--configuration','Release','--runtime','win-x64','--self-contained','true','--output',str(game)],check=True,cwd=ROOT)
 for name in ('Assets','ThirdParty'):
  if (data/name).exists():shutil.copytree(data/name,game/name,dirs_exist_ok=True)
 shutil.copy2(ROOT/'AccessibleFootball2026.Core'/'Data'/'players_database.json',game/'players_database.json')
 # Hash the freshly rebuilt game, never attach the old binary's manifest to changed source.
 files=[{'path':p.relative_to(game).as_posix(),'bytes':p.stat().st_size,'sha256':digest(p)} for p in sorted(game.rglob('*')) if p.is_file() and p.name!='manifest.json']
 try:source=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
 except (OSError,subprocess.CalledProcessError):source='source archive; git commit unavailable'
 (game/'manifest.json').write_text(json.dumps({'source_commit':source,'input_sha256':SHA,'files':files},indent=2))
 iscc=a.iscc or shutil.which('ISCC.exe')
 if not iscc:
  candidates=[Path(os.environ.get(x,''))/'Inno Setup 6'/'ISCC.exe' for x in ('ProgramFiles(x86)','ProgramFiles')]
  iscc=next((str(p) for p in candidates if p.is_file()),None)
 if not iscc:raise SystemExit('Game rebuilt with all assets. Install official Inno Setup or supply --iscc ISCC.exe.')
 iss=work/'complete.iss';iss.write_text('[Setup]\nAppId=AccessibleFootball2026-Full\nAppName=Accessible Football 2026\nAppVersion=2026.11.13\nDefaultDirName={localappdata}\\AccessibleFootball2026\nDefaultGroupName=Accessible Football 2026\nPrivilegesRequired=lowest\nOutputDir=output\nOutputBaseFilename=AccessibleFootball2026-Full-Setup\nCompression=lzma2/fast\nSolidCompression=no\nDiskSpanning=no\nDisableDirPage=yes\nDisableProgramGroupPage=yes\n[Files]\nSource: "'+str(game)+'\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs\n[Icons]\nName: "{group}\\Accessible Football 2026"; Filename: "{app}\\AccessibleFootball2026.exe"; WorkingDir: "{app}"\n',encoding='utf-8')
 subprocess.run([iscc,str(iss)],check=True)
 exe=work/'output'/'AccessibleFootball2026-Full-Setup.exe'
 if not exe.is_file() or list(exe.parent.glob('*.bin')):raise SystemExit('Not a complete one-file installer')
 print('BUILT',exe,'bytes',exe.stat().st_size,'SHA256',digest(exe))
 print('Still requires clean offline Windows/NVDA/audio/controls acceptance. No game was launched.')
if __name__=='__main__':main()
