using System;using System.IO;using System.Diagnostics;using System.Globalization;using System.Drawing;using System.Windows.Media;using System.Windows.Media.Media3D;
public static class MeshFormats {
 public static string Root=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(MeshFormats).Assembly.Location),".."));
 public static Bitmap Render(string path)=>PreviewCache.Get(path,()=>RenderUncached(path));
 static Bitmap RenderUncached(string path){if(Path.GetExtension(path).Equals(".stl",StringComparison.OrdinalIgnoreCase))return RenderStl(path);string output=Path.Combine(Path.GetTempPath(),"AuxMesh-"+Guid.NewGuid()+".stl");try{var start=new ProcessStartInfo(Path.Combine(Root,"cad-runtime","python.exe")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};start.ArgumentList.Add(Path.Combine(Root,"ConvertMesh.py"));start.ArgumentList.Add(path);start.ArgumentList.Add(output);using(var p=Process.Start(start)){var stdout=p.StandardOutput.ReadToEndAsync();var stderr=p.StandardError.ReadToEndAsync();if(!p.WaitForExit(Path.GetExtension(path).Equals(".obj",StringComparison.OrdinalIgnoreCase)?150000:25000)){p.Kill(true);throw new IOException("Conversion timed out");}if(p.ExitCode!=0)throw new IOException("Model conversion failed");}if(File.Exists(output+".png"))using(var image=new Bitmap(output+".png"))return new Bitmap(image);return RenderStl(output);}finally{try{File.Delete(output);File.Delete(output+".png");}catch{}}}
 static double Number(string s){return double.Parse(s,CultureInfo.InvariantCulture);}
 public static Bitmap RenderStl(string file){var points=new Point3DCollection();var indices=new Int32Collection();using(var fs=File.OpenRead(file)){if(fs.Length<15)throw new InvalidDataException();using(var reader=new BinaryReader(fs)){bool binary=false;uint count=0;if(fs.Length>=84){fs.Position=80;count=reader.ReadUInt32();binary=84L+count*50L==fs.Length;}if(binary){if(count>LargeStl.Threshold){var preview=LargeStl.Read(reader,count);points=preview.Positions;indices=preview.TriangleIndices;}else for(uint t=0;t<count;t++){reader.ReadSingle();reader.ReadSingle();reader.ReadSingle();for(int j=0;j<3;j++){points.Add(new Point3D(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()));indices.Add(indices.Count);}reader.ReadUInt16();}}else if(fs.Length>75L*1024*1024){var preview=LargeStl.ReadText(file);points=preview.Positions;indices=preview.TriangleIndices;}else{fs.Position=0;using(var text=new StreamReader(fs)){string line;while((line=text.ReadLine())!=null){var a=line.Trim().Split((char[])null,StringSplitOptions.RemoveEmptyEntries);if(a.Length==4&&a[0].Equals("vertex",StringComparison.OrdinalIgnoreCase)){if(points.Count>=4500000)throw new InvalidDataException();points.Add(new Point3D(Number(a[1]),Number(a[2]),Number(a[3])));indices.Add(indices.Count);}}}}}}if(indices.Count==0||indices.Count%3!=0)throw new InvalidDataException("Empty mesh");foreach(var p in points)if(!double.IsFinite(p.X)||!double.IsFinite(p.Y)||!double.IsFinite(p.Z))throw new InvalidDataException();var material=new MaterialGroup();material.Children.Add(new DiffuseMaterial(new SolidColorBrush(System.Windows.Media.Color.FromRgb(184,180,169))));material.Children.Add(new SpecularMaterial(new SolidColorBrush(System.Windows.Media.Color.FromRgb(100,96,85)),32));var group=new Model3DGroup();group.Children.Add(new GeometryModel3D(new MeshGeometry3D{Positions=points,TriangleIndices=indices},material){BackMaterial=material});return ModelRender.RenderGroup(group);}
}

// Content keys survive Shell's random temporary filenames. No resident service.
public static class PreviewCache {
 // Bump whenever thumbnail appearance/parsing changes to invalidate old images.
 const string Revision="preview-20261022-v1";
 public const long DefaultLimit=256L*1024*1024;
 public static string DirectoryPath=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"3DSTL","PreviewCache");
 static string Key(string source){using var input=File.OpenRead(source);return Revision+"-"+Path.GetExtension(source).TrimStart('.').ToLowerInvariant()+"-"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(input));}
 public static Bitmap Get(string source,Func<Bitmap> render,string directory=null,long limit=DefaultLimit) {
  string key;
  try {key=Key(source);}
  catch(IOException){return render();}catch(UnauthorizedAccessException){return render();}
  directory=directory??DirectoryPath;string file=Path.Combine(directory,key+".png");
  var cached=Read(file);if(cached!=null)return cached;
  System.Threading.Mutex mutex;
  try {mutex=new System.Threading.Mutex(false,"Local\\3DSTL-"+key);}
  catch(UnauthorizedAccessException){return render();}
  using(mutex) {
   bool owned=false;
   try {
    try {owned=mutex.WaitOne(150000);}catch(System.Threading.AbandonedMutexException){owned=true;}
    if(!owned)throw new IOException("Another process is still generating this model preview");
    cached=Read(file);if(cached!=null)return cached;
    var result=render();
    try {if(Key(source)==key){Save(file,result);Trim(directory,limit);}}catch(IOException){}catch(UnauthorizedAccessException){}catch(System.Runtime.InteropServices.ExternalException){}
    return result;
   } finally {if(owned)mutex.ReleaseMutex();}
  }
 }
 static Bitmap Read(string file) {
  try {
   if(!File.Exists(file)||new FileInfo(file).Length>8L*1024*1024)return null;
   using var image=Image.FromFile(file);
   if(image.Width>1024||image.Height>1024)return null;
   var result=new Bitmap(image);
   try {File.SetLastWriteTimeUtc(file,DateTime.UtcNow);}catch(IOException){}catch(UnauthorizedAccessException){}
   return result;
  }catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}catch(ArgumentException){return null;}catch(OutOfMemoryException){return null;}
 }
 static void Save(string file,Bitmap image) {
  Directory.CreateDirectory(Path.GetDirectoryName(file));string temp=file+"."+Guid.NewGuid().ToString("N")+".tmp";
  try {image.Save(temp,System.Drawing.Imaging.ImageFormat.Png);File.Move(temp,file,true);}
  finally {try{File.Delete(temp);}catch(IOException){}catch(UnauthorizedAccessException){}}
 }
 public static void Trim(string directory,long limit=DefaultLimit) {
  var files=new DirectoryInfo(directory).GetFiles("*.png");
  Array.Sort(files,(a,b)=>b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));long retained=0;
  foreach(var file in files) {
   try {long size=file.Length;if(file.LastWriteTimeUtc<DateTime.UtcNow.AddDays(-30)||size>limit-retained)file.Delete();else retained+=size;}
   catch(IOException){}catch(UnauthorizedAccessException){}
  }
  foreach(var file in new DirectoryInfo(directory).GetFiles("*.tmp"))
   try{if(file.LastWriteTimeUtc<DateTime.UtcNow.AddDays(-1))file.Delete();}catch(IOException){}catch(UnauthorizedAccessException){}
 }
}
