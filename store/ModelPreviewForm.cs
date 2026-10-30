using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// A local image preview, not an editor. Rendering stays outside the UI process.
sealed class ModelPreviewForm : Form {
 readonly PictureBox picture = new PictureBox { Dock=DockStyle.Fill, SizeMode=PictureBoxSizeMode.Zoom, BackColor=Color.FromArgb(225,225,223) };
 readonly Label status = new Label { Dock=DockStyle.Bottom, Height=56, Padding=new Padding(12), AutoEllipsis=true };
 readonly Button open = new Button { AutoSize=true }, sample = new Button { AutoSize=true }, settings = new Button { AutoSize=true };
 readonly ComboBox language = new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList, Width=150 };
 readonly string languageFile, initialFile;
 CancellationTokenSource pending;
 bool english, busy;
 string currentFile, error;
 public bool PreviewReady { get; private set; }
 public event Action<bool> PreviewCompleted;
 public ModelPreviewForm(string file=null, string settingsDirectory=null, Action showSettings=null) {
  initialFile=file;
  languageFile=Path.Combine(settingsDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"3DSTL"),"language.txt");
  try { english=File.Exists(languageFile)&&File.ReadAllText(languageFile).Trim()=="en"; } catch(IOException) {} catch(UnauthorizedAccessException) {}
  Text="3DSTL";ClientSize=new Size(900,760);MinimumSize=new Size(640,480);StartPosition=FormStartPosition.CenterScreen;
  Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(19,28,42);ForeColor=Color.White;AllowDrop=true;
  var toolbar=new FlowLayoutPanel { Dock=DockStyle.Top,Height=54,Padding=new Padding(10) };
  language.ForeColor=Color.Black;language.BackColor=Color.White;
  foreach(var button in new[]{open,sample,settings}){button.FlatStyle=FlatStyle.Flat;button.BackColor=Color.FromArgb(38,71,94);button.ForeColor=Color.White;button.Padding=new Padding(6,2,6,2);}
  language.Items.AddRange(new object[]{"简体中文","English"});language.SelectedIndex=english?1:0;
  toolbar.Controls.Add(open);toolbar.Controls.Add(sample);toolbar.Controls.Add(language);var folderButton=new Button{Text="文件夹 / Folder",AutoSize=true};folderButton.Click+=(_,_)=>new FolderPreviewForm().Show(this);toolbar.Controls.Add(folderButton);
  if(showSettings!=null){toolbar.Controls.Add(settings);settings.Click+=(_,_)=>showSettings();}
  Controls.Add(picture);Controls.Add(status);Controls.Add(toolbar);
  open.Click+=(_,_)=>{using var picker=new OpenFileDialog { Filter="3D models|"+string.Join(";",AssociationManager.Formats.Select(x=>"*."+x))+"|All files|*.*",CheckFileExists=true };if(picker.ShowDialog(this)==DialogResult.OK)_=LoadModel(picker.FileName);};
  sample.Click+=(_,_)=>{_=LoadModel(Path.Combine(AppContext.BaseDirectory,"..","Samples","sample.stl"));};
  language.SelectedIndexChanged+=(_,_)=>{english=language.SelectedIndex==1;try{Directory.CreateDirectory(Path.GetDirectoryName(languageFile));File.WriteAllText(languageFile,english?"en":"zh");}catch(IOException){}catch(UnauthorizedAccessException){}Labels();};
  DragEnter+=(_,e)=>{if(e.Data.GetDataPresent(DataFormats.FileDrop))e.Effect=DragDropEffects.Copy;};
  DragDrop+=(_,e)=>{if(e.Data.GetData(DataFormats.FileDrop) is string[] files&&files.Length>0)_=LoadModel(files[0]);};
  Shown+=async(_,_)=>{if(initialFile!=null)await LoadModel(initialFile);};
  FormClosed+=(_,_)=>{pending?.Cancel();picture.Image?.Dispose();picture.Image=null;};
  Labels();
 }
 void Labels() {
  open.Text=english?"Open model…":"打开模型…";sample.Text=english?"Try sample":"查看示例";settings.Text=english?"Thumbnail settings":"缩略图设置";
  Text=PreviewReady?Path.GetFileName(currentFile)+" · 3DSTL":"3DSTL · "+(english?"Model preview":"模型预览");
  status.Text=busy?(english?"Generating preview, please wait… Large files may take up to 3 minutes.":"正在生成预览，请稍候… 大文件可能需要等待 3 分钟。"):
   error!=null?(english?"Cannot preview this file. ":"无法预览此文件。")+(error=="timeout"?(english?"Rendering timed out or was cancelled.":"渲染超时或已取消。"):error=="missing"?(english?"The selected file or renderer is missing.":"所选文件或渲染组件不存在。"):error=="invalid"?(english?"Unsupported, damaged, or too large. Try a smaller STL/3MF file.":"格式不支持、文件损坏或超过限制，请尝试较小的 STL/3MF 文件。"):(english?"Check file permissions or try a smaller STL/3MF file.":"请检查文件访问权限，或尝试较小的 STL/3MF 文件。")) :
   PreviewReady?Path.GetFileName(currentFile)+(english?" · Image preview only; original file is unchanged.":" · 仅图片预览，原始文件未修改。"):
   (english?"Open or drop a model here, or try the bundled sample. This is an image preview, not a 3D editor.":"打开或拖入模型，也可查看内置示例。这里显示图片预览，不提供 3D 编辑。");
 }
 public async Task LoadModel(string file) {
  pending?.Cancel();var job=new CancellationTokenSource();pending=job;
  PreviewReady=false;error=null;busy=true;currentFile=file;
  var old=picture.Image;picture.Image=null;old?.Dispose();Labels();
  try {
   var image=await Task.Run(()=>RenderImage(file,job.Token));
   if(IsDisposed||pending!=job){image.Dispose();return;}
   picture.Image=image;PreviewReady=true;
  } catch(OperationCanceledException) {if(pending!=job||IsDisposed)return;error="timeout";}
  catch(Exception e) {if(pending!=job||IsDisposed)return;error=e is FileNotFoundException?"missing":e is InvalidDataException?"invalid":"access";RecordError(e);}

  finally { if(pending==job&&!IsDisposed){pending=null;busy=false;Labels();PreviewCompleted?.Invoke(PreviewReady);}job.Dispose(); }
 }
 void RecordError(Exception exception) {
  try {
   string path=Path.Combine(Path.GetDirectoryName(languageFile),"preview-error.log");
   Directory.CreateDirectory(Path.GetDirectoryName(path));
   if(File.Exists(path)&&new FileInfo(path).Length>65536)File.WriteAllText(path,"");
   File.AppendAllText(path,DateTimeOffset.Now.ToString("O")+" "+exception+Environment.NewLine);
  }catch(IOException){}catch(UnauthorizedAccessException){}
 }
 public static async Task<Bitmap> RenderImage(string file,CancellationToken cancel,bool force=false) {
  file=Path.GetFullPath(file);if(!File.Exists(file))throw new FileNotFoundException();
  string ext=Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
  if(!AssociationManager.Formats.Contains(ext))throw new InvalidDataException();
  long limit=ext=="stl"?1024L*1024*1024:(ext=="3mf"||ext=="obj")?512L*1024*1024:128L*1024*1024;
  if(new FileInfo(file).Length<=0||new FileInfo(file).Length>limit)throw new InvalidDataException();
  string exe=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","worker","3DSTL.Render.exe"));
  if(!File.Exists(exe))throw new FileNotFoundException();
  string output=Path.Combine(Path.GetTempPath(),"3DSTL-preview-"+Guid.NewGuid().ToString("N")+".bmp");
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancel);timeout.CancelAfter(TimeSpan.FromMinutes(3));
  var start=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=Path.GetDirectoryName(exe)};
  start.ArgumentList.Add(file);start.ArgumentList.Add(output);start.ArgumentList.Add("512");if(force)start.ArgumentList.Add("--force");
  using var process=new Process { StartInfo=start };
  try {
   cancel.ThrowIfCancellationRequested();process.Start();
   try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); }
   catch { try{if(!process.HasExited)process.Kill(true);}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}await process.WaitForExitAsync().ConfigureAwait(false);throw; }
   if(process.ExitCode!=0||!File.Exists(output)){
    string detail=File.Exists(output+".error.txt")?File.ReadAllText(output+".error.txt"):"Renderer exit code: "+process.ExitCode;
    throw new InvalidDataException(detail);
   }
   using var input=Image.FromFile(output);return new Bitmap(input);
  } finally {foreach(var path in new[]{output,output+".error.txt"})try{File.Delete(path);}catch(IOException){}catch(UnauthorizedAccessException){}}
 }
 public static int Snapshot(string file,string output) {
  int code=1;using var form=new ModelPreviewForm(file);
  form.PreviewCompleted+=ok=>{if(ok){using var bmp=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(output);code=0;}form.Close();};
  Application.Run(form);return code;
 }
}

sealed class FolderPreviewForm : Form {
 readonly Button choose=new Button{Text="选择文件夹 / Choose folder",AutoSize=true},cancel=new Button{Text="取消 / Cancel",AutoSize=true,Enabled=false};
 readonly ProgressBar bar=new ProgressBar{Dock=DockStyle.Bottom,Height=20};
 readonly TextBox details=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
 readonly Label state=new Label{Dock=DockStyle.Bottom,Height=60,Padding=new Padding(8)};
 readonly CheckBox force=new CheckBox{Text="强制更新 / Force refresh",AutoSize=true};
 CancellationTokenSource lifetime;bool running,closing;
 public FolderPreviewForm(){Text="3DSTL · 文件夹预生成 / Folder previews";Width=780;Height=500;var buttons=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42};buttons.Controls.Add(choose);buttons.Controls.Add(cancel);buttons.Controls.Add(force);Controls.Add(details);Controls.Add(state);Controls.Add(bar);Controls.Add(buttons);
 choose.Click+=async(_,_)=>{using var dialog=new FolderBrowserDialog();if(dialog.ShowDialog(this)==DialogResult.OK)await Generate(dialog.SelectedPath);};cancel.Click+=(_,_)=>{lifetime?.Cancel();state.Text="正在取消当前任务… / Cancelling…";};FormClosing+=(_,e)=>{if(running){e.Cancel=true;closing=true;lifetime.Cancel();}};state.Text="只处理所选文件夹当前层级，小文件优先。任务结束后退出，不修改模型。";}
 public async Task Generate(string folder){if(running)return;running=true;choose.Enabled=false;force.Enabled=false;cancel.Enabled=true;lifetime=new CancellationTokenSource();int done=0,failed=0;
 try {
  state.Text="正在读取文件列表 / Reading file list…";
  var token=lifetime.Token;
  var files=await Task.Run(()=>Directory.EnumerateFiles(folder).Where(p=>AssociationManager.Formats.Contains(Path.GetExtension(p).TrimStart('.').ToLowerInvariant())).Select(p=>{token.ThrowIfCancellationRequested();return new FileInfo(p);}).OrderBy(f=>f.Length).Select(f=>f.FullName).ToArray(),token);
  bar.Maximum=Math.Max(1,files.Length);bar.Value=0;
  foreach(var file in files){token.ThrowIfCancellationRequested();state.Text=$"读取/生成 Reading/rendering {done+1}/{files.Length}\r\n{Path.GetFileName(file)}";
   try{using var image=await ModelPreviewForm.RenderImage(file,token,force.Checked);
    string refresher=Path.Combine(AppContext.BaseDirectory,"refresh","3DSTL.Refresh.exe");
    if(File.Exists(refresher)){var si=new ProcessStartInfo(refresher){UseShellExecute=false,CreateNoWindow=true};si.ArgumentList.Add(file);using var child=Process.Start(si);try{await child.WaitForExitAsync(token);}catch{try{if(!child.HasExited)child.Kill(true);}catch{}await child.WaitForExitAsync();throw;}if(child.ExitCode!=0)throw new IOException("Windows thumbnail extraction failed");}
    details.AppendText("OK "+Path.GetFileName(file)+Environment.NewLine);
   }catch(OperationCanceledException){throw;}catch(Exception e){failed++;details.AppendText("FAILED "+Path.GetFileName(file)+": "+e.Message+Environment.NewLine);}
   done++;bar.Value=done;
  }
  NativeNotify(folder);state.Text=$"完成 Completed: {done-failed}，失败 Failed: {failed}";
 }catch(OperationCanceledException){state.Text=$"已取消 Cancelled: {done} completed, {failed} failed";}catch(Exception e){state.Text=e.Message;}
 finally{running=false;choose.Enabled=true;force.Enabled=true;cancel.Enabled=false;lifetime.Dispose();lifetime=null;if(closing)Close();}}
 [System.Runtime.InteropServices.DllImport("shell32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern void SHChangeNotify(uint e,uint f,string path,IntPtr other);
 static void NativeNotify(string folder)=>SHChangeNotify(0x1000,0x1005,folder,IntPtr.Zero);
}
