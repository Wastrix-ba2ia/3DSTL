using System;using System.IO;using System.IO.Compression;using System.Linq;using System.Xml;using System.Xml.Linq;using System.Collections.Generic;using System.Globalization;using System.Drawing;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Media3D;using System.Windows.Media.Imaging;
public static class ModelRender {
 static double Num(string s){return double.Parse(s,CultureInfo.InvariantCulture);}static string Attr(XElement e,string n){var a=e.Attributes().FirstOrDefault(x=>x.Name.LocalName==n);return a==null?null:a.Value;}
 static Matrix3D Matrix(string text){if(string.IsNullOrWhiteSpace(text))return Matrix3D.Identity;var a=text.Split(new[]{' ','\t','\n'},StringSplitOptions.RemoveEmptyEntries).Select(Num).ToArray();if(a.Length!=12)throw new InvalidDataException("Invalid transform");return new Matrix3D(a[0],a[1],a[2],0,a[3],a[4],a[5],0,a[6],a[7],a[8],0,a[9],a[10],a[11],1);}

 // Conservative shape-only upright guess. Sample count is independent of file size.
 public static Matrix3D AutoOrientation(Model3DGroup group){
  if(group.Children.Count!=1||group.Children[0] is not GeometryModel3D shape||shape.Geometry is not MeshGeometry3D mesh||mesh.Positions.Count<12)return Matrix3D.Identity;
  var points=new List<Point3D>();int step=Math.Max(1,(mesh.Positions.Count+8191)/8192);
  for(int i=0;i<mesh.Positions.Count;i+=step)points.Add(group.Transform.Transform(shape.Transform.Transform(mesh.Positions[i])));
  var mean=new Point3D(points.Average(p=>p.X),points.Average(p=>p.Y),points.Average(p=>p.Z));
  double xx=0,xy=0,xz=0,yy=0,yz=0,zz=0;
  foreach(var p in points){var d=p-mean;xx+=d.X*d.X;xy+=d.X*d.Y;xz+=d.X*d.Z;yy+=d.Y*d.Y;yz+=d.Y*d.Z;zz+=d.Z*d.Z;}
  Vector3D Multiply(Vector3D a)=>new Vector3D(xx*a.X+xy*a.Y+xz*a.Z,xy*a.X+yy*a.Y+yz*a.Z,xz*a.X+yz*a.Y+zz*a.Z);
  var axis=xx>=yy&&xx>=zz?new Vector3D(1,0,0):yy>=zz?new Vector3D(0,1,0):new Vector3D(0,0,1);
  for(int i=0;i<24;i++){var next=Multiply(axis);if(next.Length<1e-15)return Matrix3D.Identity;next.Normalize();axis=next;}
  double major=Vector3D.DotProduct(axis,Multiply(axis));
  // Round heads, broad masks and ambiguous assemblies retain their original orientation.
  if(!(major>2.5*(xx+yy+zz-major)))return Matrix3D.Identity;
  double min=double.PositiveInfinity,max=double.NegativeInfinity;
  foreach(var p in points){double t=Vector3D.DotProduct(p-mean,axis);min=Math.Min(min,t);max=Math.Max(max,t);}
  double low=0,high=0;int nl=0,nh=0;double band=(max-min)*.22;
  var lower=new List<Vector3D>();var upper=new List<Vector3D>();
  foreach(var p in points){var d=p-mean;double t=Vector3D.DotProduct(d,axis);var radial=d-axis*t;if(t<min+band)lower.Add(radial);if(t>max-band)upper.Add(radial);}
  double Spread(List<Vector3D> values){if(values.Count<3)return 0;var c=new Vector3D(values.Average(v=>v.X),values.Average(v=>v.Y),values.Average(v=>v.Z));return values.Average(v=>(v-c).LengthSquared);}
  low=Spread(lower);high=Spread(upper);nl=lower.Count;nh=upper.Count;
  // Only a clear taper identifies which end belongs at the top; symmetric rods are ambiguous.
  if(nl<3||nh<3||Math.Max(low,high)<1e-15||Math.Max(low,high)<1.8*Math.Min(low,high))return Matrix3D.Identity;
  if(high>low)axis=-axis;
  var up=new Vector3D(0,0,1);double dot=Math.Clamp(Vector3D.DotProduct(axis,up),-1,1);
  if(dot>.995)return Matrix3D.Identity;
  var rotationAxis=Vector3D.CrossProduct(axis,up);if(rotationAxis.Length<1e-8)rotationAxis=new Vector3D(1,0,0);else rotationAxis.Normalize();
  var matrix=Matrix3D.Identity;matrix.RotateAt(new Quaternion(rotationAxis,Math.Acos(dot)*180/Math.PI),mean);return matrix;
 }
 // Retain only compact component metadata; never materialize huge mesh XML trees.
 static XElement ReadMetadata(XmlReader reader,Dictionary<XElement,MeshGeometry3D> meshes,int depth){
  if(depth>64)throw new InvalidDataException("XML nesting too deep");
  var element=new XElement(XName.Get(reader.LocalName,reader.NamespaceURI));
  if(reader.HasAttributes){while(reader.MoveToNextAttribute()){
   XName name=reader.Prefix=="xmlns"?XNamespace.Xmlns+reader.LocalName:reader.Name=="xmlns"?XName.Get("xmlns"):XName.Get(reader.LocalName,reader.NamespaceURI);
   element.SetAttributeValue(name,reader.Value);
  }reader.MoveToElement();}
  if(reader.LocalName=="mesh"){meshes[element]=ReadMesh(reader);return element;}
  bool empty=reader.IsEmptyElement;reader.Read();if(empty)return element;
  while(reader.NodeType!=XmlNodeType.EndElement){
   if(reader.EOF)throw new InvalidDataException("Truncated model XML");
   if(reader.NodeType==XmlNodeType.Element)element.Add(ReadMetadata(reader,meshes,depth+1));else reader.Read();
  }reader.Read();return element;
 }
 static MeshGeometry3D ReadMesh(XmlReader reader){
  var vertices=new List<Point3D>();var indices=new Int32Collection();long faces=0;
  string spool=null;BinaryWriter writer=null;
  Point3D Vertex(int index){if(index<0||index>=vertices.Count)throw new InvalidDataException("Invalid vertex index");return vertices[index];}
  void WriteFace(int a,int b,int c){foreach(int index in new[]{a,b,c}){var p=Vertex(index);writer.Write(p.X);writer.Write(p.Y);writer.Write(p.Z);}}
  try{
   using(var sub=reader.ReadSubtree())while(sub.Read())if(sub.NodeType==XmlNodeType.Element){
    if(sub.LocalName=="vertex"){
     if(vertices.Count>=12000000)throw new InvalidDataException("Too many model vertices");
     var p=new Point3D(Num(sub.GetAttribute("x")),Num(sub.GetAttribute("y")),Num(sub.GetAttribute("z")));
     if(!double.IsFinite(p.X)||!double.IsFinite(p.Y)||!double.IsFinite(p.Z))throw new InvalidDataException("Invalid model vertex");vertices.Add(p);
    }else if(sub.LocalName=="triangle"){
     int a=int.Parse(sub.GetAttribute("v1"),CultureInfo.InvariantCulture),b=int.Parse(sub.GetAttribute("v2"),CultureInfo.InvariantCulture),c=int.Parse(sub.GetAttribute("v3"),CultureInfo.InvariantCulture);
     Vertex(a);Vertex(b);Vertex(c);faces++;if(faces>20000000)throw new InvalidDataException("Too many model faces");
     if(faces==1500001){spool=Path.GetTempFileName();writer=new BinaryWriter(File.Open(spool,FileMode.Create,FileAccess.Write,FileShare.None));for(int i=0;i<indices.Count;i+=3)WriteFace(indices[i],indices[i+1],indices[i+2]);indices.Clear();}
     if(writer!=null)WriteFace(a,b,c);else{indices.Add(a);indices.Add(b);indices.Add(c);}
    }
   }
   reader.Read();if(faces==0)throw new InvalidDataException("Empty model mesh");
   if(writer==null)return new MeshGeometry3D{Positions=new Point3DCollection(vertices),TriangleIndices=indices};
   writer.Dispose();writer=null;vertices.Clear();vertices.TrimExcess();
   return LargeStl.Reduce(()=>ReadSpool(spool),64);
  }finally{writer?.Dispose();if(spool!=null)try{File.Delete(spool);}catch{}}
 }
 static IEnumerable<Point3D> ReadSpool(string file){using var reader=new BinaryReader(File.OpenRead(file));while(reader.BaseStream.Position<reader.BaseStream.Length)yield return new Point3D(reader.ReadDouble(),reader.ReadDouble(),reader.ReadDouble());}
 public static Bitmap Render(string file){using(var zip=ZipFile.OpenRead(file)){var docs=new Dictionary<string,XDocument>(StringComparer.OrdinalIgnoreCase);var meshes=new Dictionary<XElement,MeshGeometry3D>();long expanded=0;foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".model",StringComparison.OrdinalIgnoreCase))){expanded+=entry.Length;if(entry.Length>1024L*1024*1024||expanded>2L*1024*1024*1024)throw new InvalidDataException("Model archive too large");using(var stream=entry.Open())using(var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=1024L*1024*1024})){reader.MoveToContent();docs[entry.FullName.TrimStart('/')]=new XDocument(ReadMetadata(reader,meshes,0));}}string main=docs.Keys.FirstOrDefault(k=>k.Equals("3D/3dmodel.model",StringComparison.OrdinalIgnoreCase))??docs.Keys.First();var group=new Model3DGroup();int triangles=0;Action<string,string,Matrix3D,int> add=null;add=(part,id,transform,depth)=>{if(depth>20)throw new InvalidDataException("Component nesting too deep");XDocument doc;if(!docs.TryGetValue(part,out doc))throw new InvalidDataException("Missing component");var obj=doc.Descendants().FirstOrDefault(e=>e.Name.LocalName=="object"&&Attr(e,"id")==id);if(obj==null)throw new InvalidDataException("Missing object");var mesh=obj.Elements().FirstOrDefault(e=>e.Name.LocalName=="mesh");if(mesh!=null){var source=meshes[mesh];var positions=new Point3DCollection(source.Positions.Count);foreach(var v in source.Positions)positions.Add(transform.Transform(v));var indices=source.TriangleIndices;triangles+=indices.Count/3;if(triangles>4000000)throw new InvalidDataException("Too many displayed instances");var material=new MaterialGroup();material.Children.Add(new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Color.FromRgb(184,180,169))));material.Children.Add(new SpecularMaterial(new SolidColorBrush(System.Windows.Media.Color.FromRgb(100,96,85)),32));var geo=new GeometryModel3D(new MeshGeometry3D{Positions=positions,TriangleIndices=indices},material){BackMaterial=material};group.Children.Add(geo);}foreach(var component in obj.Descendants().Where(e=>e.Name.LocalName=="component")){string target=Attr(component,"path");if(target==null)target=part;else if(target.StartsWith("/"))target=target.TrimStart('/');else target=new Uri(new Uri("http://model/"+part),target).AbsolutePath.TrimStart('/');var m=Matrix(Attr(component,"transform"));m.Append(transform);add(target,Attr(component,"objectid"),m,depth+1);}};foreach(var item in docs[main].Descendants().Where(e=>e.Name.LocalName=="item")){if(Attr(item,"printable")=="0")continue;add(main,Attr(item,"objectid"),Matrix(Attr(item,"transform")),0);}if(group.Children.Count==0)throw new InvalidDataException("No mesh found");return RenderGroup(group);}} public static Bitmap RenderGroup(Model3DGroup group){var oriented=new Model3DGroup();foreach(var child in group.Children)oriented.Children.Add(child);var orientation=group.Transform.Value;orientation.Append(AutoOrientation(group));oriented.Transform=new MatrixTransform3D(orientation);group=oriented;var bounds=group.Bounds;var center=new Point3D(bounds.X+bounds.SizeX/2,bounds.Y+bounds.SizeY/2,bounds.Z+bounds.SizeZ/2);double radius=Math.Sqrt(bounds.SizeX*bounds.SizeX+bounds.SizeY*bounds.SizeY+bounds.SizeZ*bounds.SizeZ)/2;if(radius<=0)throw new InvalidDataException("Empty mesh");var direction=new Vector3D(1.2,-1.8,1.0);direction.Normalize();var world=new Model3DGroup();world.Children.Add(group);world.Children.Add(new AmbientLight(System.Windows.Media.Color.FromRgb(72,72,72)));world.Children.Add(new DirectionalLight(System.Windows.Media.Color.FromRgb(220,216,205),new Vector3D(-1,1,-2)));world.Children.Add(new DirectionalLight(System.Windows.Media.Color.FromRgb(70,81,95),new Vector3D(1,-1,-.3)));var viewport=new Viewport3D{Camera=new OrthographicCamera(center+direction*radius*4,-direction,new Vector3D(0,0,1),radius*2.35)};viewport.Children.Add(new ModelVisual3D{Content=world});var host=new Grid{Background=new SolidColorBrush(System.Windows.Media.Color.FromRgb(225,225,223)),Width=640,Height=640};var right=Vector3D.CrossProduct(-direction,new Vector3D(0,0,1));right.Normalize();var up=Vector3D.CrossProduct(right,-direction);up.Normalize();double low=double.PositiveInfinity;foreach(GeometryModel3D shape in group.Children){var mesh=(MeshGeometry3D)shape.Geometry;foreach(var v in mesh.Positions)low=Math.Min(low,Vector3D.DotProduct(group.Transform.Transform(shape.Transform.Transform(v))-center,up));}double bottom=320-low*640/(radius*2.35);var shadow=new System.Windows.Shapes.Ellipse{Width=radius>0?300:300,Height=55,Fill=new SolidColorBrush(System.Windows.Media.Color.FromArgb(90,30,30,30)),VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,Math.Max(5,640-bottom-30)),Effect=new System.Windows.Media.Effects.BlurEffect{Radius=24}};host.Children.Add(shadow);host.Children.Add(viewport);host.Measure(new System.Windows.Size(640,640));host.Arrange(new Rect(0,0,640,640));host.UpdateLayout();var image=new RenderTargetBitmap(640,640,96,96,PixelFormats.Pbgra32);image.Render(host);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var mem=new MemoryStream()){encoder.Save(mem);mem.Position=0;using(var bmp=new Bitmap(mem))return new Bitmap(bmp);}}
}