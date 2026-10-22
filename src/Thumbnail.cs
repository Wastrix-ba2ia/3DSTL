using System;using System.IO;using System.Drawing;using System.Runtime.InteropServices;using System.Runtime.InteropServices.ComTypes;using System.Threading;using System.IO.Compression;using System.Linq;
[assembly:ComVisible(false)]
[ComImport,Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IStreamInit{void Initialize(IStream stream,uint mode);}
[ComImport,Guid("e357fccd-a995-4576-b01f-234630154e96"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]public interface IThumb{void GetThumbnail(uint size,out IntPtr bitmap,out uint alpha);}
[ComVisible(true),Guid("B7E33521-1381-4ED2-9C3F-989B9CF937E8"),ClassInterface(ClassInterfaceType.None)]public class Thumbnail:IStreamInit,IThumb {
 protected virtual string Extension {get{return ".3mf";}} string input;
 ~Thumbnail(){try{if(input!=null)File.Delete(input);}catch{}}
 public static Bitmap RenderOrEmbedded(string file)=>PreviewCache.Get(file,()=>Render3mfUncached(file)); private static Bitmap Render3mfUncached(string file){try{return ModelRender.Render(file);}catch{using(var archive=ZipFile.OpenRead(file)){var entries=archive.Entries.Where(e=>e.Length>0&&e.Length<16*1024*1024&&e.FullName.EndsWith(".png",StringComparison.OrdinalIgnoreCase)&&(e.FullName.IndexOf("thumbnail",StringComparison.OrdinalIgnoreCase)>=0||e.FullName.IndexOf("plate_",StringComparison.OrdinalIgnoreCase)>=0)).OrderBy(e=>e.FullName.EndsWith("thumbnail_3mf.png",StringComparison.OrdinalIgnoreCase)?0:e.FullName.Equals("Metadata/plate_1.png",StringComparison.OrdinalIgnoreCase)?1:2);foreach(var entry in entries){try{using(var stream=entry.Open())using(var image=Image.FromStream(stream)){var bmp=new Bitmap(512,512);using(var g=Graphics.FromImage(bmp)){g.Clear(Color.FromArgb(225,225,223));g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;float scale=Math.Min(480f/image.Width,480f/image.Height);g.DrawImage(image,(512-image.Width*scale)/2,(512-image.Height*scale)/2,image.Width*scale,image.Height*scale);}return bmp;}}catch{}}}throw;}}
 public void Initialize(IStream stream,uint mode){
  if(input!=null)throw new COMException("Already initialized");
  System.Runtime.InteropServices.ComTypes.STATSTG stat;stream.Stat(out stat,1);
  long limit=Extension==".stl"?LargeStl.InputLimit:(Extension==".3mf"||Extension==".obj")?512L*1024*1024:128L*1024*1024;
  if(stat.cbSize<=0||stat.cbSize>limit)throw new InvalidDataException("Input too large");
  string temp=Path.Combine(Path.GetTempPath(),"3DSTL-"+Guid.NewGuid()+Extension);
  var read=Marshal.AllocCoTaskMem(4);
  try{using(var output=File.Create(temp)){var buffer=new byte[65536];long total=0;while(true){stream.Read(buffer,buffer.Length,read);int count=Marshal.ReadInt32(read);if(count<=0)break;total+=count;if(total>limit)throw new InvalidDataException("Input too large");output.Write(buffer,0,count);}if(total==0)throw new InvalidDataException("Empty input");}input=temp;}
  catch{try{File.Delete(temp);}catch{}throw;}finally{Marshal.FreeCoTaskMem(read);}
 }

 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr handle);
 public void GetThumbnail(uint size,out IntPtr bitmap,out uint alpha){
  if(input==null)throw new COMException("Not initialized");
  IntPtr result=IntPtr.Zero;Exception error=null;object gate=new object();bool abandoned=false;
  int side=(int)Math.Max(32,Math.Min(size,512));bool large=LargeStl.IsLarge(input)||Extension==".3mf"||Extension==".obj";
  var thread=new Thread(()=>{
   try{
    if(large)Thread.CurrentThread.Priority=ThreadPriority.Lowest;
    using(var image=Extension==".3mf"?RenderOrEmbedded(input):MeshFormats.Render(input))using(var scaled=new Bitmap(side,side)){
     using(var g=Graphics.FromImage(scaled)){g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.DrawImage(image,0,0,side,side);}
     lock(gate){if(!abandoned)result=scaled.GetHbitmap();}
    }
   }catch(Exception e){error=e;}
  }){IsBackground=true};
  thread.SetApartmentState(ApartmentState.STA);thread.Start();
  if(!thread.Join(large?180000:30000)){
   lock(gate){abandoned=true;if(result!=IntPtr.Zero){DeleteObject(result);result=IntPtr.Zero;}}
   throw new COMException("Render timed out");
  }
  if(error!=null)throw new COMException("Model preview could not be rendered",unchecked((int)0x80004005));
  bitmap=result;alpha=1;
 }

}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000001"),ClassInterface(ClassInterfaceType.None)]public class Thumb_stl:Thumbnail {protected override string Extension {get{return ".stl";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000002"),ClassInterface(ClassInterfaceType.None)]public class Thumb_obj:Thumbnail {protected override string Extension {get{return ".obj";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000003"),ClassInterface(ClassInterfaceType.None)]public class Thumb_ply:Thumbnail {protected override string Extension {get{return ".ply";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000004"),ClassInterface(ClassInterfaceType.None)]public class Thumb_off:Thumbnail {protected override string Extension {get{return ".off";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000005"),ClassInterface(ClassInterfaceType.None)]public class Thumb_amf:Thumbnail {protected override string Extension {get{return ".amf";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000006"),ClassInterface(ClassInterfaceType.None)]public class Thumb_gcode:Thumbnail {protected override string Extension {get{return ".gcode";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000007"),ClassInterface(ClassInterfaceType.None)]public class Thumb_gco:Thumbnail {protected override string Extension {get{return ".gco";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000008"),ClassInterface(ClassInterfaceType.None)]public class Thumb_step:Thumbnail {protected override string Extension {get{return ".step";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000009"),ClassInterface(ClassInterfaceType.None)]public class Thumb_stp:Thumbnail {protected override string Extension {get{return ".stp";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000010"),ClassInterface(ClassInterfaceType.None)]public class Thumb_sldprt:Thumbnail {protected override string Extension {get{return ".sldprt";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000011"),ClassInterface(ClassInterfaceType.None)]public class Thumb_sldasm:Thumbnail {protected override string Extension {get{return ".sldasm";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000012"),ClassInterface(ClassInterfaceType.None)]public class Thumb_slddrw:Thumbnail {protected override string Extension {get{return ".slddrw";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000013"),ClassInterface(ClassInterfaceType.None)]public class Thumb_prtdot:Thumbnail {protected override string Extension {get{return ".prtdot";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000014"),ClassInterface(ClassInterfaceType.None)]public class Thumb_asmdot:Thumbnail {protected override string Extension {get{return ".asmdot";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000015"),ClassInterface(ClassInterfaceType.None)]public class Thumb_drwdot:Thumbnail {protected override string Extension {get{return ".drwdot";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000016"),ClassInterface(ClassInterfaceType.None)]public class Thumb_eprt:Thumbnail {protected override string Extension {get{return ".eprt";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000017"),ClassInterface(ClassInterfaceType.None)]public class Thumb_easm:Thumbnail {protected override string Extension {get{return ".easm";}}}
[ComVisible(true),Guid("A37F1000-17A0-4D18-BA10-000000000018"),ClassInterface(ClassInterfaceType.None)]public class Thumb_edrw:Thumbnail {protected override string Extension {get{return ".edrw";}}}