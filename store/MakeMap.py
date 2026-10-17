from pathlib import Path
root=Path(__file__).resolve().parents[1];folder=root/'artifacts/store-payload'
files=[p for p in sorted(folder.rglob('*')) if p.is_file() and p.suffix not in ('.pdb','.pyc','.lib') and '__pycache__' not in p.parts]
(root/'artifacts/store-map.txt').write_text('[Files]\n'+'\n'.join('"'+str(p)+'" "'+str(p.relative_to(folder))+'"' for p in files),encoding='utf8')