import io,struct,re,pathlib
from PIL import Image
Image.MAX_IMAGE_PIXELS=16000000
def extract(source,dest):
 def save(data):
  try:
   with Image.open(io.BytesIO(data)) as image:
    if image.width*image.height>16000000:return False
    image.load();image.thumbnail((640,640));image.convert('RGBA').save(dest);return True
  except Exception:return False
 def inspect(data):
  if len(data)>32*1024*1024:return False
  if save(data):return True
  for signature in (b'\x89PNG\r\n\x1a\n',b'\xff\xd8\xff',b'BM'):
   start=0
   for attempt in range(12):
    offset=data.find(signature,start)
    if offset<0:break
    if save(data[offset:]):return True
    start=offset+len(signature)
  # Clipboard DIB stream / SummaryInformation thumbnail (CF_DIB).
  for offset in range(min(32,len(data)-40)):
   try:
    size=struct.unpack_from('<I',data,offset)[0]
    if size not in (40,108,124):continue
    width,height,planes,bits,compression=struct.unpack_from('<iiHHI',data,offset+4)
    if width<=0 or abs(height)<=0 or width*abs(height)>16000000 or planes!=1 or bits not in (1,4,8,16,24,32):continue
    colors=struct.unpack_from('<I',data,offset+32)[0];palette=(colors or (1<<bits)) if bits<=8 else 0
    pixel_offset=14+size+palette*4+(12 if compression==3 and size==40 else 0)
    body=data[offset:];header=b'BM'+struct.pack('<IHHI',14+len(body),0,0,pixel_offset)
    if save(header+body):return True
   except Exception:pass
  return False
 import olefile
 if olefile.isOleFile(str(source)):
  with olefile.OleFileIO(str(source)) as ole:
   for name in ole.listdir():
    if any(word in '/'.join(name).lower() for word in ('preview','thumbnail','summaryinformation')) and ole.get_size(name)<32*1024*1024:
     if inspect(ole.openstream(name).read()):return
     if name[-1]=='\x05SummaryInformation':
      for value in ole.getproperties(name).values():
       if isinstance(value,bytes) and inspect(value):return
 else:
  # Some eDrawings exports carry directly embedded bitmap blocks.
  with open(source,'rb') as stream:
   if inspect(stream.read(32*1024*1024)):return
 raise ValueError('No supported embedded preview; proprietary geometry is not rendered')