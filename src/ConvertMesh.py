import sys,pathlib,struct,math,re,base64,xml.etree.ElementTree as ET,zipfile
source=pathlib.Path(sys.argv[1]);dest=pathlib.Path(sys.argv[2]);ext=source.suffix.lower()
def write_stl(triangles):
 with dest.open('wb') as f:
  f.write(b'AuxDash mesh preview'.ljust(80,b'\0'));f.write(struct.pack('<I',len(triangles)))
  for tri in triangles:f.write(struct.pack('<12fH',0,0,0,*[v for p in tri for v in p],0))
if ext in ('.sldprt','.sldasm','.slddrw','.prtdot','.asmdot','.drwdot','.eprt','.easm','.edrw'):
 from EmbeddedCadPreview import extract
 extract(source,str(dest)+'.png')
elif ext in ('.step','.stp'):
 from OCP.STEPControl import STEPControl_Reader
 from OCP.IFSelect import IFSelect_RetDone
 from OCP.BRepMesh import BRepMesh_IncrementalMesh
 from OCP.StlAPI import StlAPI_Writer
 reader=STEPControl_Reader()
 if reader.ReadFile(str(source))!=IFSelect_RetDone:raise ValueError('Cannot read STEP')
 if reader.TransferRoots()==0:raise ValueError('No STEP shapes')
 shape=reader.OneShape();mesh=BRepMesh_IncrementalMesh(shape,0.12,False,0.35,False);mesh.Perform()
 writer=StlAPI_Writer();writer.ASCIIMode=False
 if not writer.Write(shape,str(dest)):raise ValueError('Cannot tessellate STEP')
elif ext=='.amf':
 if zipfile.is_zipfile(source):
  with zipfile.ZipFile(source) as z:
   names=[n for n in z.namelist() if n.lower().endswith('.amf')]
   entry=z.getinfo(names[0])
   if entry.file_size>180*1024*1024:raise ValueError('AMF too large')
   root=ET.fromstring(z.read(entry))
 else:root=ET.parse(source).getroot()
 for e in root.iter():e.tag=e.tag.rsplit('}',1)[-1]
 objects={}
 for obj in root.findall('object'):
  vertices=[tuple(float(v.findtext('coordinates/'+k)) for k in ('x','y','z')) for v in obj.findall('mesh/vertices/vertex')]
  triangles=[]
  for t in obj.findall('mesh/volume/triangle'):triangles.append(tuple(vertices[int(t.findtext(k))] for k in ('v1','v2','v3')))
  objects[obj.attrib['id']]=triangles
 triangles=[];instances=root.findall('constellation/instance')
 if instances:
  import numpy as np
  for instance in instances:
   rx,ry,rz=[math.radians(float(instance.findtext(k,'0'))) for k in ('rx','ry','rz')]
   cx,sx,cy,sy,cz,sz=math.cos(rx),math.sin(rx),math.cos(ry),math.sin(ry),math.cos(rz),math.sin(rz)
   matrix=np.array([[cz,-sz,0],[sz,cz,0],[0,0,1]])@np.array([[cy,0,sy],[0,1,0],[-sy,0,cy]])@np.array([[1,0,0],[0,cx,-sx],[0,sx,cx]])
   delta=np.array([float(instance.findtext(k,'0')) for k in ('deltax','deltay','deltaz')])
   triangles.extend(tuple(tuple(matrix@np.array(p)+delta) for p in t) for t in objects[instance.attrib['objectid']])
 else:
  for t in objects.values():triangles.extend(t)
 write_stl(triangles)
elif ext in ('.gcode','.gco'):
 # Use slicer thumbnails when present. Only ordinary PNG/JPEG thumbnail blocks are decoded.
 text=source.read_text(errors='replace')
 for block in re.finditer(r';\s*thumbnail(?:_JPG|_PNG)?\s+begin[^\n]*\n(.*?)\n;\s*thumbnail(?:_JPG|_PNG)?\s+end',text,re.S|re.I):
  try:
   encoded=''.join(line.lstrip('; ').strip() for line in block.group(1).splitlines());raw=base64.b64decode(encoded,validate=True)
   if len(raw)>16*1024*1024:continue
   from PIL import Image
   import io
   with Image.open(io.BytesIO(raw)) as image:
    image.thumbnail((640,640));image.save(str(dest)+'.png');sys.exit(0)
  except Exception:pass
 x=y=z=e=0.;xyz_absolute=True;e_absolute=True;segments=[]
 for line in text.splitlines():
  line=line.split(';',1)[0].strip().upper()
  m=re.match(r'(?:N\d+\s+)?([GMT]\d+)\b',line)
  if not m:continue
  cmd=m.group(1);values={k:float(v) for k,v in re.findall(r'([XYZE])\s*([-+]?(?:\d*\.\d+|\d+\.?\d*))',line)}
  if cmd=='G90':xyz_absolute=True;continue
  if cmd=='G91':xyz_absolute=False;continue
  if cmd=='M82':e_absolute=True;continue
  if cmd=='M83':e_absolute=False;continue
  if cmd=='G92':
   x=values.get('X',x);y=values.get('Y',y);z=values.get('Z',z);e=values.get('E',e);continue
  if cmd not in ('G0','G00','G1','G01'):continue
  nx=values.get('X',x) if xyz_absolute else x+values.get('X',0);ny=values.get('Y',y) if xyz_absolute else y+values.get('Y',0);nz=values.get('Z',z) if xyz_absolute else z+values.get('Z',0)
  ne=values.get('E',e) if e_absolute else e+values.get('E',0)
  if ne>e+1e-7 and math.hypot(nx-x,ny-y)>.01:segments.append(((x,y,z),(nx,ny,nz)))
  x,y,z,e=nx,ny,nz,ne
 triangles=[]
 stride=max(1,math.ceil(len(segments)/60000))
 for a,b in segments[::stride]:
  dx,dy=b[0]-a[0],b[1]-a[1];length=math.hypot(dx,dy);ox,oy=-dy/length*.23,dx/length*.23
  p=(a[0]+ox,a[1]+oy,a[2]);q=(a[0]-ox,a[1]-oy,a[2]);r=(b[0]-ox,b[1]-oy,b[2]);s=(b[0]+ox,b[1]+oy,b[2]);triangles.extend(((p,q,r),(p,r,s)))
 if not triangles:raise ValueError('No printable linear extrusion paths or embedded thumbnail')
 write_stl(triangles)
else:
 import trimesh
 mesh=trimesh.load(str(source),force='scene',process=False)
 if isinstance(mesh,trimesh.Scene):mesh=mesh.to_geometry()
 if not isinstance(mesh,trimesh.Trimesh) or not len(mesh.faces):raise ValueError('No mesh faces')
 if len(mesh.faces)>1500000:raise ValueError('Mesh too complex')
 mesh.export(str(dest),file_type='stl')