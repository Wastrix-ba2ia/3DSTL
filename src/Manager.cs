using System;using System.IO;using System.Linq;using System.Drawing;using System.Windows.Forms;using Microsoft.Win32;using System.Text.Json;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Diagnostics;using System.Threading.Tasks;
class Saved {public bool Exists{get;set;}public string Value{get;set;}="";}
class AssociationManager {
 public static readonly string[] Formats={"3mf","stl","obj","ply","off","amf","gcode","gco","step","stp","sldprt","sldasm","slddrw","prtdot","asmdot","drwdot","eprt","easm","edrw"};
 public string Root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..")),RegistryRoot="Software\\Classes",BackupFile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"3DSTL","associations.json");
 public static string Id(string ext){int i=Array.IndexOf(Formats,ext);return i==0?"{B7E33521-1381-4ED2-9C3F-989B9CF937E8}":"{A37F1000-17A0-4D18-BA10-"+i.ToString("000000000000")+"}";}
 string Key(string ext){return RegistryRoot+"\\."+ext+"\\shellex\\{e357fccd-a995-4576-b01f-234630154e96}";}
 public bool Active(string ext){using(var k=Registry.CurrentUser.OpenSubKey(Key(ext)))return string.Equals(k==null?null:k.GetValue("") as string,Id(ext),StringComparison.OrdinalIgnoreCase);}
 Dictionary<string,Saved> Read(){if(!File.Exists(BackupFile)&&RegistryRoot=="Software\\Classes"){string old=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AuxModelPreview","associations.json");if(File.Exists(old)){Directory.CreateDirectory(Path.GetDirectoryName(BackupFile));File.Copy(old,BackupFile,false);}}return File.Exists(BackupFile)?JsonSerializer.Deserialize<Dictionary<string,Saved>>(File.ReadAllText(BackupFile)):new Dictionary<string,Saved>();}
 void Save(Dictionary<string,Saved> data){Directory.CreateDirectory(Path.GetDirectoryName(BackupFile));string temp=BackupFile+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));File.Move(temp,BackupFile,true);}
 Saved Legacy(string ext){if(ext=="3mf"){string p=Path.Combine(Root,"previous-handler.txt");if(File.Exists(p)){string text=File.ReadAllText(p);return new Saved{Exists=text!="<absent>",Value=text=="<absent>"?"":text};}}else{string p=Path.Combine(Root,"previous-format-handlers.json");if(File.Exists(p)){using(var doc=JsonDocument.Parse(File.ReadAllText(p))){JsonElement entry;if(doc.RootElement.TryGetProperty("."+ext,out entry))return new Saved{Exists=entry.GetProperty("Exists").GetBoolean(),Value=entry.GetProperty("Value").GetString()??""};}}}throw new IOException("缺少 ."+ext+" 的原设置备份，已停止操作以免覆盖原设置。");}
 public void Repair(string[] chosen,bool refreshDesktop=true){string dll=Path.Combine(Root,"shell","Aux3mfThumbnail.comhost.dll");if(!File.Exists(dll)||!File.Exists(Path.ChangeExtension(dll.Replace(".comhost.dll",".dll"),".dll")))throw new IOException("缺少缩略图组件，请保留 shell 文件夹。");if(chosen.Any(x=>x!="3mf"&&x!="stl")&&!File.Exists(Path.Combine(Root,"cad-runtime","python.exe")))throw new IOException("缺少格式转换组件 cad-runtime。");var data=Read();foreach(string ext in chosen){if(!data.ContainsKey(ext)){using(var k=Registry.CurrentUser.OpenSubKey(Key(ext))){var previous=new Saved{Exists=k!=null,Value=k==null?"":Convert.ToString(k.GetValue(""))};data[ext]=previous.Value==Id(ext)?Legacy(ext):previous;}Save(data);}string cls=RegistryRoot+"\\CLSID\\"+Id(ext);using(var k=Registry.CurrentUser.CreateSubKey(cls+"\\InprocServer32")){k.SetValue("",dll);k.SetValue("ThreadingModel","Both");}using(var k=Registry.CurrentUser.CreateSubKey(cls))k.SetValue("DisableProcessIsolation",1,RegistryValueKind.DWord);using(var k=Registry.CurrentUser.CreateSubKey(Key(ext)))k.SetValue("",Id(ext));}Notify();if(refreshDesktop&&RegistryRoot=="Software\\Classes")StartDesktopRefresh();}
 public void Rebind(string[] chosen){
  string dll=Path.Combine(Root,"shell","Aux3mfThumbnail.comhost.dll");
  if(!File.Exists(dll)||!File.Exists(Path.Combine(Root,"shell","Aux3mfThumbnail.dll")))throw new IOException("缺少缩略图组件，请保留 shell 文件夹。");
  foreach(string ext in chosen){
   if(!Formats.Contains(ext)||!Active(ext))continue;
   using(var key=Registry.CurrentUser.CreateSubKey(RegistryRoot+"\\CLSID\\"+Id(ext)+"\\InprocServer32")){key.SetValue("",dll);key.SetValue("ThreadingModel","Both");}
  }
  Notify();
 }
 public string Uninstall(){var data=Read();foreach(string ext in Formats)if(Active(ext)&&!data.ContainsKey(ext)){data[ext]=Legacy(ext);Save(data);}int restored=0;foreach(string ext in Formats){if(!Active(ext))continue;Saved previous;if(!data.TryGetValue(ext,out previous))throw new IOException("缺少原设置备份。");if(previous.Exists){using(var k=Registry.CurrentUser.CreateSubKey(Key(ext)))k.SetValue("",previous.Value);}else Registry.CurrentUser.DeleteSubKey(Key(ext),false);restored++;}foreach(string ext in Formats){string cls=RegistryRoot+"\\CLSID\\"+Id(ext);using(var k=Registry.CurrentUser.OpenSubKey(cls+"\\InprocServer32")){if(k!=null&&Convert.ToString(k.GetValue("")).StartsWith(Root,StringComparison.OrdinalIgnoreCase)){Registry.CurrentUser.DeleteSubKeyTree(cls,false);}}}Notify();return "已卸载 "+restored+" 种格式的缩略图功能，并恢复原关联。\r\n模型文件未改动。备份和程序文件保留，可再次安装。";}
 [DllImport("shell32.dll")]static extern void SHChangeNotify(uint e,uint f,IntPtr a,IntPtr b);
 public static void Notify(){SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);}
 public static void StartDesktopRefresh(){string exe=Path.Combine(AppContext.BaseDirectory,"refresh","3DSTL.Refresh.exe");if(!File.Exists(exe))exe=Path.Combine(AppContext.BaseDirectory,"3DSTL.Refresh.exe");if(!File.Exists(exe))throw new IOException("缺少桌面刷新工具，请重新安装完整程序。");if(!ManagerForm.RefreshRunning())Process.Start(new ProcessStartInfo(exe){UseShellExecute=false,CreateNoWindow=true});}
}
partial class ManagerForm:Form {
 bool english,repairBusy,snapshotMode; readonly ProgressBar progress=new ProgressBar{Dock=DockStyle.Bottom,Height=7,Visible=false};readonly System.Windows.Forms.Timer refreshTimer=new System.Windows.Forms.Timer{Interval=1000};readonly string refreshLog=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"3DSTL","thumbnail-refresh.log"); readonly string languageFile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"3DSTL","language.txt");
 readonly AssociationManager manager=new AssociationManager();readonly CheckedListBox formats=new CheckedListBox();readonly Label status=new Label();readonly Button repair=new Button(),uninstall=new Button();
 public ManagerForm(bool online=true,string settingsDirectory=null){if(settingsDirectory!=null)languageFile=Path.Combine(settingsDirectory,"language.txt");english=File.Exists(languageFile)&&File.ReadAllText(languageFile)=="en";Text="3DSTL · 模型缩略图管理";AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(850,820);MinimumSize=new Size(810,790);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(19,28,42);ForeColor=Color.FromArgb(229,238,247);Font=new Font("Microsoft YaHei UI",11);var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(26),ColumnCount=1,RowCount=7};foreach(int h in new[]{47,55,270,58,55,70})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(layout);
 var header=new FlowLayoutPanel{Dock=DockStyle.Fill};header.Controls.Add(new Label{Text="3DSTL",Font=new Font(Font.FontFamily,25,FontStyle.Bold),Width=520,Height=45});var language=new ComboBox{Name="LanguageSelector",Width=165,DropDownStyle=ComboBoxStyle.DropDownList,ForeColor=Color.Black};language.Items.AddRange(new object[]{"简体中文","English"});language.SelectedIndex=english?1:0;language.SelectedIndexChanged+=delegate{english=language.SelectedIndex==1;if(!snapshotMode){Directory.CreateDirectory(Path.GetDirectoryName(languageFile));File.WriteAllText(languageFile,english?"en":"zh");}UpdateStatus();ApplyLanguage(this);RefreshUpdateLabels();Text=english?"3DSTL · Model Thumbnails":"3DSTL · 模型缩略图管理";};header.Controls.Add(language);layout.Controls.Add(header,0,0);layout.Controls.Add(new Label{Text="在 Windows 资源管理器和桌面直接查看模型预览\r\n本地处理 · 浅灰立体效果 · 不修改模型文件",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(158,193,217)},0,1);
 formats.Dock=DockStyle.Fill;formats.BackColor=Color.FromArgb(28,43,61);formats.ForeColor=ForeColor;formats.BorderStyle=BorderStyle.None;formats.CheckOnClick=true;formats.ColumnWidth=385;formats.MultiColumn=true;formats.IntegralHeight=false;layout.Controls.Add(formats,0,2);
 status.Dock=DockStyle.Fill;status.AutoEllipsis=true;status.ForeColor=Color.FromArgb(129,219,203);var statusPanel=new Panel{Dock=DockStyle.Fill};statusPanel.Controls.Add(status);statusPanel.Controls.Add(progress);layout.Controls.Add(statusPanel,0,3);refreshTimer.Tick+=delegate{ReadRefreshProgress();};FormClosed+=delegate{refreshTimer.Dispose();};
 var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill};repair.Text="一键修复 / 安装";uninstall.Text="卸载缩略图功能";var refresh=new Button{Text="刷新状态"};foreach(var b in new[]{repair,uninstall,refresh}){b.AutoSize=true;b.Padding=new Padding(10,5,10,5);b.FlatStyle=FlatStyle.Flat;b.BackColor=Color.FromArgb(38,71,94);b.ForeColor=ForeColor;buttons.Controls.Add(b);}layout.Controls.Add(buttons,0,4);
 layout.Controls.Add(new Label{Text="修复勾选的格式；卸载恢复全部格式的旧设置。首次生成较慢。\r\n已打开的桌面可能需要重新登录 Windows 后更新。",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(170,187,204)},0,5);
 var bottom=new FlowLayoutPanel{Dock=DockStyle.Fill};bottom.Controls.Add(new Label{Text="作者：BA2IA   QQ：21780   感谢 Codex\r\nMIT 开源 · 可自由使用、修改、分发（保留许可）",AutoSize=true});var license=new LinkLabel{Text="许可证",AutoSize=true,LinkColor=Color.LightSkyBlue};license.LinkClicked+=delegate{Open("LICENSE");};var third=new LinkLabel{Text="第三方许可与限制",AutoSize=true,LinkColor=Color.LightSkyBlue};third.LinkClicked+=delegate{Open("THIRD-PARTY-NOTICES.txt");};bottom.Controls.Add(license);bottom.Controls.Add(third);var about=new LinkLabel{Text="软件介绍 / 全部格式",AutoSize=true,LinkColor=Color.LightSkyBlue};about.LinkClicked+=delegate{ShowMessage("3DSTL 是免费的本地模型缩略图工具，让资源管理器和桌面直接显示模型预览。\\n\\n模型渲染：STL、3MF、OBJ、PLY、OFF、AMF、STEP、STP。\\n图片或路径预览：GCODE、GCO。\\n仅提取内置预览：SLDPRT、SLDASM、SLDDRW、PRTDOT、ASMDOT、DRWDOT、EPRT、EASM、EDRW。\\n\\n作者：BA2IA\\nQQ：21780\\n感谢 Codex\\n原创源码采用 MIT 许可，可自由使用、修改和分发，须保留版权及许可证。第三方组件遵循各自许可证。".Replace("\\n",Environment.NewLine),"关于 3DSTL",MessageBoxButtons.OK,MessageBoxIcon.Information);};bottom.Controls.Add(about);var preview=new LinkLabel{Text="打开模型预览",AutoSize=true,LinkColor=Color.LightSkyBlue};preview.LinkClicked+=delegate{new ModelPreviewForm().Show(this);};bottom.Controls.Add(preview);var folderPreview=new LinkLabel{Text="文件夹预生成",AutoSize=true,LinkColor=Color.LightSkyBlue};folderPreview.LinkClicked+=delegate{new FolderPreviewForm().Show(this);};bottom.Controls.Add(folderPreview);var logs=new LinkLabel{Text="刷新日志",AutoSize=true,LinkColor=Color.LightSkyBlue};logs.LinkClicked+=delegate{if(File.Exists(refreshLog))Process.Start(new ProcessStartInfo("notepad.exe"){UseShellExecute=true,ArgumentList={refreshLog}});else ShowMessage(english?"No refresh log yet.":"还没有刷新记录。","3DSTL",MessageBoxButtons.OK,MessageBoxIcon.Information);};bottom.Controls.Add(logs);layout.Controls.Add(bottom,0,6);InitUpdates(bottom,online);
 repair.Click+=async delegate{await RepairSelected();};uninstall.Click+=delegate{if(ShowMessage("卸载全部模型缩略图功能并恢复原关联？\r\n模型文件不会删除。","卸载",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)Run(manager.Uninstall);};refresh.Click+=delegate{if(refreshTimer.Enabled)ReadRefreshProgress();else UpdateStatus();};UpdateStatus();ApplyLanguage(this);RefreshUpdateLabels();Text=english?"3DSTL · Model Thumbnails":"3DSTL · 模型缩略图管理";if(manager.RegistryRoot=="Software\\Classes"&&RefreshRunning()){refreshTimer.Start();ReadRefreshProgress();}}
 DialogResult ShowMessage(string text,string caption,MessageBoxButtons buttons,MessageBoxIcon icon){return MessageBox.Show(this,Translate(text),Translate(caption),buttons,icon);}
 void ApplyLanguage(Control control){foreach(Control child in control.Controls){if(child is ComboBox||child is CheckedListBox||child==status)continue;if(child.Tag==null)child.Tag=child.Text;child.Text=Translate(Convert.ToString(child.Tag));ApplyLanguage(child);}}
 string Translate(string text){if(!english)return text;var map=new Dictionary<string,string>{
 {"在 Windows 资源管理器和桌面直接查看模型预览","Model thumbnails in Windows File Explorer and on your desktop"},
 {"本地处理 · 浅灰立体效果 · 不修改模型文件","Local processing · Light gray rendering · Original files stay intact"},
 {"一键修复 / 安装","Repair / Install"},{"卸载缩略图功能","Uninstall thumbnails"},{"刷新状态","Refresh status"},
 {"修复勾选的格式；卸载恢复全部格式的旧设置。首次生成较慢。","Repair selected formats. Uninstall restores previous associations."},
 {"已打开的桌面可能需要重新登录 Windows 后更新。","First previews may take time. Sign out/in if the desktop stays cached."},
 {"作者：BA2IA   QQ：21780   感谢 Codex","Author: BA2IA   QQ: 21780   Thanks to Codex"},
 {"MIT 开源 · 可自由使用、修改、分发（保留许可）","MIT · Free to use, modify and distribute (keep the license)"},
 {"文件夹预生成","Generate folder previews"},{"打开模型预览","Open model preview"},{"GitHub 项目主页","GitHub project"},{"刷新日志","Refresh log"},{"第三方许可与限制","Third-party licenses & limits"},{"软件介绍 / 全部格式","About / All formats"},{"许可证","License"},
 {"卸载全部模型缩略图功能并恢复原关联？","Uninstall all model thumbnail handlers and restore previous associations?"},
 {"模型文件不会删除。","Your model files will not be deleted."},{"操作未完成","Operation failed"},{"卸载","Uninstall"},
 {"已启用","Enabled"},{"未启用","Disabled"},{"内置预览","Embedded preview only"},
 {"3DSTL 是免费的本地模型缩略图工具，让资源管理器和桌面直接显示模型预览。","3DSTL is a free local model thumbnail tool for File Explorer and the desktop."},
 {"模型渲染：","Model rendering: "},{"图片或路径预览：","Embedded images or linear toolpaths: "},{"仅提取内置预览：","Embedded previews only: "},
 {"作者：BA2IA","Author: BA2IA"},{"QQ：21780","QQ: 21780"},{"感谢 Codex","Thanks to Codex"},{"关于 3DSTL","About 3DSTL"},
 {"原创源码采用 MIT 许可，可自由使用、修改和分发，须保留版权及许可证。第三方组件遵循各自许可证。","Original source is MIT licensed. You may use, modify and distribute it while retaining copyright and license notices. Dependencies keep their own licenses."},
 {"缺少桌面刷新工具，请重新安装完整程序。","Desktop refresher missing. Reinstall the complete app."},{"请先勾选需要修复的格式。","Select at least one format to repair."},{"缺少缩略图组件，请保留 shell 文件夹。","Thumbnail component missing. Keep the shell folder."},
 {"缺少格式转换组件 cad-runtime。","Missing converter runtime: cad-runtime."},
 {"模型文件未改动。备份和程序文件保留，可再次安装。","Model files were not changed. Backups and app files are kept for reinstallation."},
 {"种格式。桌面预览在后台更新，大 STL 最后处理。","formats. Desktop previews update in the background; large STLs run last."},
 {"种格式的缩略图功能，并恢复原关联。","thumbnail handlers and restored previous associations."},
 {"已修复 ","Repaired "},{"已卸载 ","Uninstalled "}
 };foreach(var entry in map.OrderByDescending(x=>x.Key.Length))text=text.Replace(entry.Key,entry.Value);return text;}
 public static bool RefreshRunning(){
  int session=Process.GetCurrentProcess().SessionId;
  foreach(var process in Process.GetProcessesByName("3DSTL.Refresh"))using(process){try{if(!process.HasExited&&process.SessionId==session)return true;}catch(InvalidOperationException){}}
  return false;
 }
 void Busy(bool busy){repair.Enabled=!busy;uninstall.Enabled=!busy;formats.Enabled=!busy&&manager.RegistryRoot!="";repair.Text=busy?(english?"Working…":"正在处理…"):Translate(Convert.ToString(repair.Tag)??"一键修复 / 安装");}
 async Task RepairSelected(){
  if(repairBusy)return;
  var selected=formats.CheckedIndices.Cast<int>().Select(i=>AssociationManager.Formats[i]).ToArray();
  if(selected.Length==0){status.Text=Translate("请先勾选需要修复的格式。");return;}
  repairBusy=true;Busy(true);progress.Visible=true;progress.Style=ProgressBarStyle.Marquee;
  status.Text=english?"Repairing selected formats…":"正在修复勾选的格式，请稍候…";
  try{
   await Task.Run(()=>manager.Repair(selected));UpdateStatus();
   status.Text=english?"Repair complete: "+selected.Length+" formats.":"修复完成："+selected.Length+" 种格式。";
   if(manager.RegistryRoot=="Software\\Classes"){
    status.Text+=english?" Desktop previews update in the background.":"正在后台刷新桌面预览。";
    refreshTimer.Start();ReadRefreshProgress();
   }else{progress.Style=ProgressBarStyle.Continuous;progress.Value=100;Busy(false);}
  }catch(Exception e){progress.Visible=false;Busy(false);status.Text=Translate(e.Message);ShowMessage(e.Message,"操作未完成",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
  finally{repairBusy=false;}
 }
 static string[] ReadRefreshLog(string path){
  using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var reader=new StreamReader(stream)){
   var lines=new List<string>();string line;while((line=reader.ReadLine())!=null)lines.Add(line);return lines.ToArray();
  }
 }
 void ReadRefreshProgress(){
  if(repairBusy)return;
  bool running=RefreshRunning();
  try{if(File.Exists(refreshLog)){ApplyRefreshProgress(ReadRefreshLog(refreshLog),running);return;}}
  catch(IOException){}catch(UnauthorizedAccessException){}
  Busy(running);progress.Visible=running;status.Text=running?(english?"Repair complete. Preparing desktop previews…":"修复完成，正在准备桌面预览…"):(english?"Repair complete. No background refresh is running.":"修复完成，当前没有运行中的桌面刷新任务。");
  if(!running)refreshTimer.Stop();
 }
 void ApplyRefreshProgress(string[] lines,bool running){
  int total=lines.Count(line=>line.StartsWith("QUEUED ")),ok=lines.Count(line=>line.StartsWith("OK ")),failed=lines.Count(line=>line.StartsWith("FAILED "));
  int done=ok+failed;bool completed=lines.Any(line=>line.StartsWith("Completed:"));
  progress.Visible=true;progress.Style=total==0&&running?ProgressBarStyle.Marquee:ProgressBarStyle.Continuous;
  progress.Value=total==0?(running?0:100):Math.Clamp(done*100/total,0,100);Busy(running);
  string current=lines.LastOrDefault(line=>line.StartsWith("CURRENT "));
  if(running){
   status.ForeColor=Color.FromArgb(129,219,203);
   status.Text=english?"Repair complete · Desktop previews: "+done+" / "+total+" · Failed: "+failed:"修复完成 · 桌面预览："+done+" / "+total+" · 失败："+failed;
   if(current!=null)status.Text+="\r\n"+(english?"Processing: ":"正在处理：")+Path.GetFileName(current.Substring(8));
   else status.Text+="\r\n"+(english?"Large STLs run last. Please wait.":"大 STL 在队列末尾逐个处理，请稍候。");
  }else{
   refreshTimer.Stop();status.ForeColor=failed>0||!completed?Color.FromArgb(255,193,115):Color.FromArgb(129,219,203);
   if(completed)status.Text=english?"Finished: "+ok+" succeeded, "+failed+" failed.":"刷新完成："+ok+" 个成功，"+failed+" 个失败。";
   else status.Text=english?"Refresh stopped: "+done+" / "+total+" processed.":"刷新已停止：已处理 "+done+" / "+total+" 个文件。";
   if(failed>0||!completed)status.Text+="\r\n"+(english?"Open Refresh log for details.":"点击“刷新日志”查看具体原因。");
  }
 }
 void Open(string file){Process.Start(new ProcessStartInfo(Path.Combine(manager.Root,file)){UseShellExecute=true});}
 void UpdateStatus(){var checks=formats.CheckedIndices.Cast<int>().ToHashSet();formats.Items.Clear();int active=0;foreach(string ext in AssociationManager.Formats){bool installed=manager.Active(ext);if(installed)active++;formats.Items.Add("."+ext.ToUpperInvariant()+"  "+(installed?"已启用":"未启用")+(Array.IndexOf(AssociationManager.Formats,ext)>=10?" · 内置预览":""),installed||checks.Contains(formats.Items.Count));}status.Text=english?"Enabled: "+active+" / "+AssociationManager.Formats.Length:"当前已启用 "+active+" / "+AssociationManager.Formats.Length+" 种格式。";for(int i=0;i<formats.Items.Count;i++)formats.Items[i]=Translate(formats.Items[i].ToString());}
 void Run(Func<string> operation){try{string message=operation();UpdateStatus();status.Text=Translate(message);}catch(Exception e){status.Text=Translate(e.Message);ShowMessage(e.Message,"操作未完成",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
 [STAThread]static void Main(string[] args){if(args.Length==1&&args[0]=="--version"){Console.WriteLine("3DSTL "+UpdateClient.CurrentVersion);return;}Application.EnableVisualStyles();if(args.Length>1&&(args[0]=="--repair"||args[0]=="--register-only")){try{var manager=new AssociationManager();var chosen=args.Skip(1).Where(x=>AssociationManager.Formats.Contains(x)).ToArray();if(args[0]=="--register-only")manager.Rebind(chosen);else manager.Repair(chosen);}catch(Exception e){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"repair-error.txt"),e.Message);Environment.ExitCode=1;}return;}if(args.Contains("--selftest-ui")){SelfTestUi();return;}if(args.Contains("--selftest")){SelfTest();return;}if(args.Length==3&&args[0]=="--preview-snapshot"){Environment.ExitCode=ModelPreviewForm.Snapshot(args[1],args[2]);return;}if(args.Length==2&&args[0]=="--preview"){Application.Run(new ModelPreviewForm(args[1]));return;}if(args.Length==1&&!args[0].StartsWith("--")){Application.Run(new ModelPreviewForm(args[0]));return;}using(var form=new ManagerForm(args.Length==0)){if(args.Contains("--updated"))form.updateStatus.Text="已更新到 v"+UpdateClient.CurrentVersion+"。缩略图组件如未更新，请重新登录 Windows。";if(args.Length==2&&(args[0]=="--snapshot"||args[0]=="--snapshot-en")){form.snapshotMode=true;form.Controls.Find("LanguageSelector",true).OfType<ComboBox>().First().SelectedIndex=args[0]=="--snapshot-en"?1:0;if(args[0]=="--snapshot-en"){form.english=true;form.UpdateStatus();form.ApplyLanguage(form);form.RefreshUpdateLabels();form.Text="3DSTL · Model Thumbnails";}form.Show();Application.DoEvents();using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(args[1]);}return;}Application.Run(form);}}
 static void SelfTestUi(){
  using(var form=new ManagerForm(false)){
   form.refreshTimer.Stop();form.Busy(false);string token=Guid.NewGuid().ToString("N");form.manager.RegistryRoot="Software\\AuxModelPreviewTest\\"+token;form.manager.BackupFile=Path.Combine(Path.GetTempPath(),token+".json");
   try{
    for(int i=0;i<form.formats.Items.Count;i++)form.formats.SetItemChecked(i,false);form.formats.SetItemChecked(1,true);form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Show();
    form.repair.PerformClick();
    if(form.repair.Enabled||!form.repairBusy||!form.status.Text.Contains("正在修复"))throw new Exception("Repair click has no immediate busy feedback");
    var timeout=Stopwatch.StartNew();while(form.repairBusy&&timeout.ElapsedMilliseconds<5000){Application.DoEvents();System.Threading.Thread.Sleep(10);}
    if(form.repairBusy||!form.repair.Enabled||!form.manager.Active("stl")||!form.status.Text.Contains("修复完成"))throw new Exception("Repair completion is not visible or registration failed");
    string logTest=Path.Combine(Path.GetTempPath(),token+".log");
    try{using(var writer=new StreamWriter(logTest)){writer.WriteLine("QUEUED NORMAL a.stl");writer.Flush();if(ReadRefreshLog(logTest).Length!=1)throw new Exception("Cannot read progress while worker owns log");}}finally{File.Delete(logTest);}
    var running=new[]{"QUEUED NORMAL a.stl","QUEUED LARGE b.stl","CURRENT b.stl","OK a.stl"};form.ApplyRefreshProgress(running,true);
    if(form.repair.Enabled||form.progress.Value!=50||!form.status.Text.Contains("b.stl"))throw new Exception("Background progress feedback failed");
    form.ApplyRefreshProgress(running.Concat(new[]{"FAILED b.stl 8004B200 failed","Completed: 1 succeeded, 1 failed."}).ToArray(),false);
    if(!form.repair.Enabled||!form.status.Text.Contains("1 个失败")||form.progress.Value!=100)throw new Exception("Failure summary failed");
    form.english=true;form.ApplyRefreshProgress(running.Concat(new[]{"FAILED b.stl 8004B200 failed","Completed: 1 succeeded, 1 failed."}).ToArray(),false);
    if(!form.status.Text.Contains("1 failed"))throw new Exception("English failure summary failed");
    Console.WriteLine("PASS repair-button busy state, actual isolated registration, completion feedback, progress, failure summary, English summary");
   }finally{Registry.CurrentUser.DeleteSubKeyTree(form.manager.RegistryRoot,false);if(File.Exists(form.manager.BackupFile))File.Delete(form.manager.BackupFile);}
  }
 }
 static void SelfTest(){var m=new AssociationManager();string token=Guid.NewGuid().ToString("N");m.RegistryRoot="Software\\AuxModelPreviewTest\\"+token;m.BackupFile=Path.Combine(Path.GetTempPath(),"AuxModelPreviewTest-"+token+".json");string key=m.RegistryRoot+"\\.stl\\shellex\\{e357fccd-a995-4576-b01f-234630154e96}";using(var k=Registry.CurrentUser.CreateSubKey(key))k.SetValue("","{OLD-PROVIDER}");m.Repair(new[]{"stl","obj"});if(!m.Active("stl")||!m.Active("obj"))throw new Exception("Registration failed");m.Repair(new[]{"stl"});m.Uninstall();using(var k=Registry.CurrentUser.OpenSubKey(key))if(Convert.ToString(k.GetValue(""))!="{OLD-PROVIDER}")throw new Exception("Restore failed");if(m.Active("obj"))throw new Exception("Uninstall failed");Registry.CurrentUser.DeleteSubKeyTree(m.RegistryRoot,false);File.Delete(m.BackupFile);var existing=new AssociationManager();existing.RegistryRoot="Software\\AuxModelPreviewTest\\rebind-"+token;existing.BackupFile=Path.Combine(Path.GetTempPath(),"missing-backup-"+token+".json");try{using(var reboundKey=Registry.CurrentUser.CreateSubKey(existing.RegistryRoot+"\\.stl\\shellex\\{e357fccd-a995-4576-b01f-234630154e96}"))reboundKey.SetValue("",AssociationManager.Id("stl"));existing.Rebind(new[]{"stl"});if(File.Exists(existing.BackupFile))throw new Exception("Upgrade unexpectedly rewrote recovery data");using(var reboundKey=Registry.CurrentUser.OpenSubKey(existing.RegistryRoot+"\\CLSID\\"+AssociationManager.Id("stl")+"\\InprocServer32"))if(Convert.ToString(reboundKey.GetValue(""))!=Path.Combine(existing.Root,"shell","Aux3mfThumbnail.comhost.dll"))throw new Exception("Existing component rebind failed");}finally{Registry.CurrentUser.DeleteSubKeyTree(existing.RegistryRoot,false);}Console.WriteLine("PASS repair, repeated repair, restore old association, uninstall, upgrade rebind without modifying recovery data");}
}