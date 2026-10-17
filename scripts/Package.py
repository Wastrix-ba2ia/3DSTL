from pathlib import Path
import zipfile,hashlib,re
root=Path(__file__).resolve().parents[1]
folder=root/'artifacts/3DSTL-0.1.0-win-x64'
output=root/'artifacts/3DSTL-0.1.0-win-x64.zip'
blocked={'associations.json','previous-handler.txt','previous-format-handlers.json','auth.json','pubg-profile.xml','host-trace.txt','pyvenv.cfg'}
files=[]
for path in sorted(folder.rglob('*')):
 if not path.is_file() or '__pycache__' in path.parts or path.suffix in ('.pyc','.pdb'):continue
 if path.name in blocked:raise RuntimeError('Private/config file in package: '+path.name)
 files.append(path)
with zipfile.ZipFile(output,'w',zipfile.ZIP_DEFLATED,compresslevel=5,allowZip64=True) as archive:
 for path in files:archive.write(path,path.relative_to(folder.parent).as_posix())
with zipfile.ZipFile(output) as archive:
 broken=archive.testzip()
 if broken:raise RuntimeError('Corrupt archive member: '+broken)
hash=hashlib.sha256(output.read_bytes()).hexdigest()
(root/'artifacts/SHA256SUMS.txt').write_text(hash+'  '+output.name+'\n',encoding='utf8')
print('Package verified:',len(files),'files,',round(output.stat().st_size/1024**2,1),'MiB')