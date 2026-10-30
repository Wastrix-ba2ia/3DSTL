using System;using System.IO;using System.Linq;using System.Drawing;using System.Threading;using System.Threading.Tasks;
class PreviewCacheTest {
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
 static Bitmap Image(Color color){var b=new Bitmap(8,8);using var g=Graphics.FromImage(b);g.Clear(color);return b;}
 static int Main(string[] args){string root=Path.Combine(Path.GetTempPath(),"3DSTL-cache-test-"+Guid.NewGuid());Directory.CreateDirectory(root);try{
  string source=Path.Combine(root,"a.obj"),copy=Path.Combine(root,"other-name.obj"),cache=Path.Combine(root,"cache");File.WriteAllText(source,"first");File.Copy(source,copy);int renders=0;
  using(var first=PreviewCache.Get(source,()=>{renders++;return Image(Color.Red);},cache))Check(first.GetPixel(0,0).R==255,"first render displayed");
  using(var hit=PreviewCache.Get(copy,()=>throw new Exception("Cache missed renamed identical model"),cache))Check(hit.GetPixel(0,0).R==255,"same content under another filename reuses image");
  var stamp=File.GetLastWriteTimeUtc(source);File.WriteAllText(source,"other");File.SetLastWriteTimeUtc(source,stamp);
  using(var changed=PreviewCache.Get(source,()=>{renders++;return Image(Color.Blue);},cache))Check(changed.GetPixel(0,0).B==255&&renders==2,"changed content with same length/timestamp invalidates image");
  foreach(var p in Directory.GetFiles(cache,"*.png"))File.WriteAllText(p,"broken PNG");
  using(var repaired=PreviewCache.Get(source,()=>{renders++;return Image(Color.Green);},cache))Check(repaired.GetPixel(0,0).G>0&&renders==3,"corrupt cache regenerates successfully");
  string blocked=Path.Combine(root,"not-a-directory");File.WriteAllText(blocked,"x");
  using(var fallback=PreviewCache.Get(source,()=>Image(Color.White),blocked))Check(fallback.Width==8,"unwritable cache does not block rendering");
  File.WriteAllText(source,"concurrent-"+Guid.NewGuid());int concurrent=0;
  Parallel.For(0,4,_=>{using var b=PreviewCache.Get(source,()=>{Interlocked.Increment(ref concurrent);Thread.Sleep(150);return Image(Color.Yellow);},cache);if(b.GetPixel(0,0).R!=255)throw new Exception("Concurrent pixels differ");});
  Check(concurrent==1,"concurrent requests render once");
  File.WriteAllText(source,"mutate-a");File.WriteAllText(copy,"mutate-a");
  using(var changing=PreviewCache.Get(source,()=>{File.WriteAllText(source,"mutate-b");return Image(Color.Black);},cache)){}
  bool rerendered=false;using(var original=PreviewCache.Get(copy,()=>{rerendered=true;return Image(Color.White);},cache))Check(rerendered&&original.GetPixel(0,0).R==255,"source changed during render is not saved under old content key");
  if(args.Length==1){string remote=Path.Combine(args[0],"3DSTL-test-"+Guid.NewGuid()+".obj");try{File.WriteAllText(remote,"first");int calls=0;using(var a=PreviewCache.Network(remote,p=>{calls++;return Image(Color.Red);})){}using(var locked=File.Open(remote,FileMode.Open,FileAccess.ReadWrite,FileShare.None))using(var b=PreviewCache.Network(remote,p=>throw new Exception("Network content was read on metadata cache hit")))Check(b.GetPixel(0,0).R==255,"NAS hit succeeds while model content is locked");File.WriteAllText(remote,"other");File.SetLastWriteTimeUtc(remote,DateTime.UtcNow.AddSeconds(2));using(var c=PreviewCache.Network(remote,p=>{calls++;return Image(Color.Blue);}))Check(c.GetPixel(0,0).B==255&&calls==2,"NAS metadata change regenerates");PreviewCache.InvalidateNetwork(remote);using(var d=PreviewCache.Network(remote,p=>{calls++;return Image(Color.White);}))Check(calls==3,"NAS force update bypasses path index");}finally{File.Delete(remote);}}
  string prune=Path.Combine(root,"prune");Directory.CreateDirectory(prune);foreach(var pair in new[]{("new",0),("old",1),("expired",40)}){string p=Path.Combine(prune,pair.Item1+".png");File.WriteAllBytes(p,new byte[400]);File.SetLastWriteTimeUtc(p,DateTime.UtcNow.AddDays(-pair.Item2));}
  PreviewCache.Trim(prune,500);Check(File.Exists(Path.Combine(prune,"new.png"))&&Directory.GetFiles(prune).Length==1,"disk budget retains newest and evicts old/expired images");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{Directory.Delete(root,true);}}
}
