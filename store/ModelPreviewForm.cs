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
 readonly Button open = new Button { AutoSize=true }, sample = new Button { AutoSize=true };
 readonly ComboBox language = new ComboBox { DropDownStyle=ComboBoxStyle.DropDownList, Width=150 };
 readonly string languageFile, initialFile;
 CancellationTokenSource pending;
 bool english, busy;
 string currentFile, error;
 public bool PreviewReady { get; private set; }
 public event Action<bool> PreviewCompleted;
 public ModelPreviewForm(string file=null, string settingsDirectory=null) {
  initialFile=file;
  languageFile=Path.Combine(settingsDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"3DSTL"),"language.txt");
  try { english=File.Exists(languageFile)&&File.ReadAllText(languageFile).Trim()=="en"; } catch(IOException) {} catch(UnauthorizedAccessException) {}
  Text="3DSTL";ClientSize=new Size(900,760);MinimumSize=new Size(640,480);StartPosition=FormStartPosition.CenterScreen;
  Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(19,28,42);ForeColor=Color.White;AllowDrop=true;
  var toolbar=new FlowLayoutPanel { Dock=DockStyle.Top,Height=54,Padding=new Padding(10) };
  language.ForeColor=Color.Black;language.BackColor=Color.White;
  foreach(var button in new[]{open,sample}){button.FlatStyle=FlatStyle.Flat;button.BackColor=Color.FromArgb(38,71,94);button.ForeColor=Color.White;button.Padding=new Padding(6,2,6,2);}
  language.Items.AddRange(new object[]{"简体中文","English"});language.SelectedIndex=english?1:0;
  toolbar.Controls.Add(open);toolbar.Controls.Add(sample);toolbar.Controls.Add(language);
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
  open.Text=english?"Open model…":"打开模型…";sample.Text=english?"Try sample":"查看示例";
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
  catch(Exception e) {if(pending!=job||IsDisposed)return;error=e is FileNotFoundException?"missing":e is InvalidDataException?"invalid":"access";}

  finally { if(pending==job&&!IsDisposed){pending=null;busy=false;Labels();PreviewCompleted?.Invoke(PreviewReady);}job.Dispose(); }
 }
 public static async Task<Bitmap> RenderImage(string file,CancellationToken cancel) {
  file=Path.GetFullPath(file);if(!File.Exists(file))throw new FileNotFoundException();
  string ext=Path.GetExtension(file).TrimStart('.').ToLowerInvariant();
  if(!AssociationManager.Formats.Contains(ext))throw new InvalidDataException();
  long limit=ext=="stl"?1024L*1024*1024:ext=="3mf"?512L*1024*1024:128L*1024*1024;
  if(new FileInfo(file).Length<=0||new FileInfo(file).Length>limit)throw new InvalidDataException();
  string exe=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","worker","3DSTL.Render.exe"));
  if(!File.Exists(exe))throw new FileNotFoundException();
  string output=Path.Combine(Path.GetTempPath(),"3DSTL-preview-"+Guid.NewGuid().ToString("N")+".bmp");
  using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancel);timeout.CancelAfter(TimeSpan.FromMinutes(3));
  var start=new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=Path.GetDirectoryName(exe)};
  start.ArgumentList.Add(file);start.ArgumentList.Add(output);start.ArgumentList.Add("512");
  using var process=new Process { StartInfo=start };
  try {
   cancel.ThrowIfCancellationRequested();process.Start();
   try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); }
   catch { try{if(!process.HasExited)process.Kill(true);}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}await process.WaitForExitAsync().ConfigureAwait(false);throw; }
   if(process.ExitCode!=0||!File.Exists(output))throw new InvalidDataException();
   using var input=Image.FromFile(output);return new Bitmap(input);
  } finally {try{File.Delete(output);}catch(IOException){}catch(UnauthorizedAccessException){}}
 }
 public static int Snapshot(string file,string output) {
  int code=1;using var form=new ModelPreviewForm(file);
  form.PreviewCompleted+=ok=>{if(ok){using var bmp=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(output);code=0;}form.Close();};
  Application.Run(form);return code;
 }
}
