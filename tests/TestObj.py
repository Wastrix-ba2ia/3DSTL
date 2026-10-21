"""Run with the bundled cad-runtime/python.exe; temporary models stay local."""
from pathlib import Path
import struct
import subprocess
import sys
import tempfile

converter = Path(__file__).resolve().parents[1] / 'src' / 'ConvertMesh.py'
with tempfile.TemporaryDirectory(prefix='3DSTL-obj-test-') as folder:
    root = Path(folder)
    cases = {
        'negative.obj': ('v 0 0 0\nv 1 0 0\nv 1 1 0\nv 0 1 0\nf -4 -3 -2 -1\n', 2),
        'missing-material.obj': ('mtllib missing.mtl\nv 0 0 0\nv 1 0 0\nv 0 1 0\nusemtl absent\nf 1 2 3\n', 1),
    }
    for name, (content, count) in cases.items():
        source = root / name
        source.write_text(content)
        output = source.with_suffix('.stl')
        subprocess.run([sys.executable, str(converter), str(source), str(output)], check=True)
        with output.open('rb') as stream:
            stream.seek(80)
            assert struct.unpack('<I', stream.read(4))[0] == count
        assert output.stat().st_size == 84 + 50 * count
        print('PASS', name)
    source = root / 'large.obj'
    with source.open('w') as stream:
        stream.write('v 0 0 0\nv 1 0 0\nv 0 1 0\n')
        for _ in range(1500001):
            stream.write('f 1 2 3\n')
    output = root / 'large.stl'
    subprocess.run([sys.executable, str(converter), str(source), str(output)], check=True, timeout=150)
    with output.open('rb') as stream:
        stream.seek(80)
        assert struct.unpack('<I', stream.read(4))[0] == 1500001
    assert output.stat().st_size == 84 + 50 * 1500001
    print('PASS OBJ over 1.5 million triangles is exported completely')
