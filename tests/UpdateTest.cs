using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class UpdateTest {
 sealed class FakeHttp : HttpMessageHandler {
  readonly Func<HttpRequestMessage,HttpResponseMessage> send;
  public FakeHttp(Func<HttpRequestMessage,HttpResponseMessage> callback) { send=callback; }
  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) { token.ThrowIfCancellationRequested();return Task.FromResult(send(request)); }
 }
 static void Assert(bool ok,string message){if(!ok)throw new Exception(message);}
 static async Task Reject(Func<Task> action,string name){try{await action();}catch(InvalidDataException){return;}throw new Exception("Accepted invalid input: "+name);}
 static void Add(ZipArchive archive,string name,string value){using var writer=new StreamWriter(archive.CreateEntry(name).Open());writer.Write(value);}
 static byte[] Archive(string version,string extra=null){
  using var stream=new MemoryStream();
  using(var zip=new ZipArchive(stream,ZipArchiveMode.Create,true)){
   string root="3DSTL-"+version+"-win-x64/";
   foreach(string file in new[]{"manager/3DSTL.exe","manager/3DSTL.dll","manager/updater/3DSTL.Update.exe","manager/refresh/3DSTL.Refresh.exe","shell/Aux3mfThumbnail.dll","shell/Aux3mfThumbnail.comhost.dll"})Add(zip,root+file,"fixture");
   Add(zip,root+"release.json",JsonSerializer.Serialize(new{product="3DSTL",version,platform="win-x64"}));
   if(extra!=null)Add(zip,root+extra,"escape");
  }
  return stream.ToArray();
 }
 static async Task<int> Main(string[] args){
  if(args.Length>0&&args[0]=="--live"){
   using var client=new UpdateClient();using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(30));
   var release=await client.Check("0.0.0",timeout.Token);Console.WriteLine("Latest public desktop release: "+release.Version+" ("+release.Size+" bytes)");
   if(args.Length==2){int last=-10;var result=await client.Download(release,args[1],new Progress<int>(value=>{if(value>=last+10){last=value;Console.WriteLine("Download "+value+"%");}}),timeout.Token);Console.WriteLine("Verified download SHA256: "+result.Hash);}
   return 0;
  }
  string root=Path.Combine(Path.GetTempPath(),"3DSTL-update-test-"+Guid.NewGuid());Directory.CreateDirectory(root);
  try{
   string version="9.8.7",name="3DSTL-9.8.7-win-x64.zip",prefix=UpdateClient.Repository+"/releases/download/v9.8.7/";
   byte[] archive=Archive(version);string hash=Convert.ToHexString(SHA256.HashData(archive)).ToLowerInvariant();
   string metadata=JsonSerializer.Serialize(new{draft=false,prerelease=false,tag_name="v9.8.7",assets=new object[]{new{name,size=archive.Length,browser_download_url=prefix+name,digest="sha256:"+hash},new{name="SHA256SUMS.txt",size=100,browser_download_url=prefix+"SHA256SUMS.txt"}}});
   Assert(UpdateClient.ParseRelease(metadata,version)==null,"Same version offered as an update");
   Assert(UpdateClient.ParseRelease(metadata,"10.0.0")==null,"Downgrade offered");
   Assert(UpdateClient.ParseRelease(metadata.Replace("\"prerelease\":false","\"prerelease\":true"),"0.1.0")==null,"Prerelease offered");
   await Reject(()=>Task.FromResult(UpdateClient.ParseRelease(metadata.Replace(prefix,"https://evil.example/"),"0.1.0")),"foreign release source");
   await Reject(()=>Task.FromResult(UpdateClient.ParseChecksum(hash+"  other.zip",name)),"wrong checksum filename");
   int downloads=0;
   using(var client=new UpdateClient(new FakeHttp(request=>{
    string url=request.RequestUri.ToString();
    if(url==UpdateClient.LatestApi)return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(metadata)};
    if(url==prefix+"SHA256SUMS.txt")return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(hash+"  "+name+"\n")};
    if(url==prefix+name){downloads++;return new HttpResponseMessage(HttpStatusCode.Redirect){Headers={Location=new Uri("https://release-assets.githubusercontent.com/fixture.zip")}};}
    if(url=="https://release-assets.githubusercontent.com/fixture.zip")return new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(archive)};
    throw new Exception("Unexpected request "+url);
   }))){
    var release=await client.Check("0.1.0",CancellationToken.None);
    var ready=await client.Download(release,Path.Combine(root,"download"),null,CancellationToken.None);
    Assert(ready.Hash==hash&&UpdateClient.Hash(ready.Path)==hash,"Valid download not verified");
    await client.Download(release,Path.Combine(root,"download"),null,CancellationToken.None);Assert(downloads==1,"Verified cached download not reused");
    UpdateClient.Extract(ready.Path,Path.Combine(root,"valid"),version);
    Assert(File.Exists(Path.Combine(root,"valid","manager","3DSTL.exe")),"Valid payload not extracted");
    File.WriteAllBytes(ready.Path,Encoding.UTF8.GetBytes("corrupted"));
    await client.Download(release,Path.Combine(root,"download"),null,CancellationToken.None);Assert(downloads==2,"Corrupt cached download was reused");
   }
   string unsafeZip=Path.Combine(root,"unsafe.zip");File.WriteAllBytes(unsafeZip,Archive(version,"../../outside.txt"));
   await Reject(()=>{UpdateClient.Extract(unsafeZip,Path.Combine(root,"unsafe"),version);return Task.CompletedTask;},"zip traversal");
   Assert(!Directory.Exists(Path.Combine(root,"unsafe")),"Unsafe archive wrote files before validation");
   using(var client=new UpdateClient(new FakeHttp(request=>{
    if(request.RequestUri.ToString().EndsWith("SHA256SUMS.txt"))return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(hash+"  "+name)};
    return new HttpResponseMessage(HttpStatusCode.Redirect){Headers={Location=new Uri("http://evil.example/fixture.zip")}};
   })))await Reject(async()=>{await client.Download(UpdateClient.ParseRelease(metadata,"0.1.0"),Path.Combine(root,"redirect"),null,CancellationToken.None);},"insecure redirect");
   using(var client=new UpdateClient(new FakeHttp(request=>new HttpResponseMessage(HttpStatusCode.OK){Content=request.RequestUri.ToString().EndsWith("SHA256SUMS.txt")?new StringContent(hash+"  "+name):new ByteArrayContent(new byte[archive.Length])})))
    await Reject(async()=>{await client.Download(UpdateClient.ParseRelease(metadata,"0.1.0"),Path.Combine(root,"bad-hash"),null,CancellationToken.None);},"corrupted downloaded payload");
   Assert(Directory.GetFiles(Path.Combine(root,"bad-hash")).Length==0,"Partial corrupt download not cleaned up");
   Console.WriteLine("PASS update version selection, downgrade/prerelease rejection, official download source, redirects, checksum validation, cached downloads, corrupt package rejection, safe extraction");return 0;
  }finally{Directory.Delete(root,true);}
 }
}
