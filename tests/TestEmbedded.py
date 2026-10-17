from EmbeddedCadPreview import extract
from PIL import Image
import io,pathlib,struct
p=pathlib.Path('fixtures');image=Image.new('RGB',(96,96),'silver');buf=io.BytesIO();image.save(buf,format='PNG');(p/'embedded-cad-test.bin').write_bytes(b'preview-test-header'+buf.getvalue());extract(p/'embedded-cad-test.bin',p/'embedded-cad-test.png')
(p/'no-preview-test.bin').write_bytes(b'not a supported preview')
try:extract(p/'no-preview-test.bin',p/'should-not-exist.png');raise AssertionError('Invalid input accepted')
except ValueError:pass
print('PASS embedded image extraction and unsupported-file rejection; not a genuine SOLIDWORKS compatibility test')