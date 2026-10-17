import pathlib,shutil,struct
import trimesh
from OCP.BRepPrimAPI import BRepPrimAPI_MakeBox
from OCP.STEPControl import STEPControl_Writer,STEPControl_AsIs
p=pathlib.Path('fixtures');p.mkdir(exist_ok=True)
mesh=trimesh.creation.box(extents=(20,15,10))
for ext in ['stl','obj','ply','off']:mesh.export(str(p/('box.'+ext)))
(p/'tetra.amf').write_text('<amf unit="millimeter"><object id="0"><mesh><vertices>'+''.join('<vertex><coordinates><x>%s</x><y>%s</y><z>%s</z></coordinates></vertex>'%tuple(v) for v in [(0,0,0),(20,0,0),(0,20,0),(0,0,20)])+'</vertices><volume>'+''.join('<triangle><v1>%s</v1><v2>%s</v2><v3>%s</v3></triangle>'%t for t in [(0,2,1),(0,1,3),(0,3,2),(1,2,3)])+'</volume></mesh></object></amf>')
lines=['G90','M82','G92 E0'];e=0
for layer in range(20):
 lines.append('G0 X0 Y0 Z'+str(layer*.2))
 for x,y in [(20,0),(20,15),(0,15),(0,0)]:
  e+=1;lines.append(f'G1 X{x} Y{y} E{e}')
(p/'box.gcode').write_text('\n'.join(lines));shutil.copyfile(p/'box.gcode',p/'box.gco')
w=STEPControl_Writer();w.Transfer(BRepPrimAPI_MakeBox(20,15,10).Shape(),STEPControl_AsIs);w.Write(str(p/'box.step'));shutil.copyfile(p/'box.step',p/'box.stp')
# ASCII PLY and negative-index OBJ exercise alternate parsers.
mesh.export(str(p/'box-ascii.ply'),encoding='ascii')
(p/'negative.obj').write_text('v 0 0 0\nv 20 0 0\nv 0 20 0\nv 0 0 20\nf -4 -2 -3\nf -4 -3 -1\nf -4 -1 -2\nf -3 -2 -1\n')