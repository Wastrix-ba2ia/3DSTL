using System;using System.IO;using System.Linq;using System.Drawing;using System.Windows.Forms;using Microsoft.Win32;using System.Text.Json;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Diagnostics;using System.Threading.Tasks;
class Saved {public bool Exists{get;set;}public string Value{get;set;}="";}
class AssociationManager {
 public static readonly string[] Formats={"3mf","stl","obj","ply","off","amf","gcode","gco","step","stp","sldprt","sldasm","slddrw","prtdot","asmdot","drwdot","eprt","easm","edrw"};
 public string Root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..")),RegistryRoot="",BackupFile="";
 public static string Id(string ext){return ext;}
 public bool Active(string ext){return true;}
 public void Repair(string[] formats,bool refreshDesktop=true){Notify();}
 public void Rebind(string[] formats){Notify();}
 public string Uninstall(){Process.Start(new ProcessStartInfo("ms-settings:appsfeatures"){UseShellExecute=true});return "请在 Windows 设置中卸载 3DSTL，Windows 会自动移除缩略图关联。";}
 [DllImport("shell32.dll")]static extern void SHChangeNotify(uint e,uint f,IntPtr a,IntPtr b);
 public static void Notify(){SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);}
}
partial class ManagerForm:Form {
 bool english,repairBusy,snapshotMode; readonly ProgressBar progress=new ProgressBar{Dock=DockStyle.Bottom,Height=7,Visible=false};readonly System.Windows.Forms.Timer refreshTimer=new System.Windows.Forms.Timer{Interval=1000};readonly string refreshLog=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"3DSTL","thumbnail-refresh.log"); readonly string languageFile=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"3DSTL","language.txt");
 readonly AssociationManager manager=new AssociationManager();readonly CheckedListBox formats=new CheckedListBox();readonly Label status=new Label();readonly Button repair=new Button(),uninstall=new Button();
 public ManagerForm(bool online=true,string settingsDirectory=null){if(settingsDirectory!=null)languageFile=Path.Combine(settingsDirectory,"language.txt");english=File.Exists(languageFile)&&File.ReadAllText(languageFile)=="en";Text="3DSTL · 模型缩略图管理";AutoScaleMode=AutoScaleMode.Dpi;ClientSize=new Size(850,820);MinimumSize=new Size(810,790);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.FromArgb(19,28,42);ForeColor=Color.FromArgb(229,238,247);Font=new Font("Microsoft YaHei UI",11);var layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(26),ColumnCount=1,RowCount=7};foreach(int h in new[]{47,55,270,58,55,70})layout.RowStyles.Add(new RowStyle(SizeType.Absolute,h));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(layout);
 var header=new FlowLayoutPanel{Dock=DockStyle.Fill};header.Controls.Add(new Label{Text="3DSTL",Font=new Font(Font.FontFamily,25,FontStyle.Bold),Width=520,Height=45});var language=new ComboBox{Name="LanguageSelector",Width=165,DropDownStyle=ComboBoxStyle.DropDownList,ForeColor=Color.Black};language.Items.AddRange(new object[]{"简体中文","English"});language.SelectedIndex=english?1:0;language.SelectedIndexChanged+=delegate{english=language.SelectedIndex==1;if(!snapshotMode){Directory.CreateDirectory(Path.GetDirectoryName(languageFile));File.WriteAllText(languageFile,english?"en":"zh");}UpdateStatus();ApplyLanguage(this);RefreshUpdateLabels();Text=english?"3DSTL · Model Thumbnails":"3DSTL · 模型缩略图管理";};header.Controls.Add(language);layout.Controls.Add(header,0,0);layout.Controls.Add(new Label{Text="在 Windows 资源管理器和桌面直接查看模型预览\r\n本地处理 · 浅灰立体效果 · 不修改模型文件",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(158,193,217)},0,1);
 formats.Dock=DockStyle.Fill;formats.BackColor=Color.FromArgb(28,43,61);formats.ForeColor=ForeColor;formats.BorderStyle=BorderStyle.None;formats.CheckOnClick=false;formats.Enabled=false;formats.ColumnWidth=385;formats.MultiColumn=true;formats.IntegralHeight=false;layout.Controls.Add(formats,0,2);
 status.Dock=DockStyle.Fill;status.AutoEllipsis=true;status.ForeColor=Color.FromArgb(129,219,203);var statusPanel=new Panel{Dock=DockStyle.Fill};statusPanel.Controls.Add(status);statusPanel.Controls.Add(progress);layout.Controls.Add(statusPanel,0,3);refreshTimer.Tick+=delegate{ReadRefreshProgress();};FormClosed+=delegate{refreshTimer.Dispose();};
 var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill};repair.Text="刷新缩略图";uninstall.Text="在 Windows 中卸载";var refresh=new Button{Text="刷新状态"};foreach(var b in new[]{repair,uninstall,refresh}){b.AutoSize=true;b.Padding=new Padding(10,5,10,5);b.FlatStyle=FlatStyle.Flat;b.BackColor=Color.FromArgb(38,71,94);b.ForeColor=ForeColor;buttons.Controls.Add(b);}layout.Controls.Add(buttons,0,4);
 layout.Controls.Add(new Label{Text="商店版由 Windows 管理格式关联；卸载时自动移除。\r\n已打开的桌面可能需要重新登录 Windows 后更新。",Dock=DockStyle.Fill,ForeColor=Color.FromArgb(170,187,204)},0,5);
 var bottom=new FlowLayoutPanel{Dock=DockStyle.Fill};bottom.Controls.Add(new Label{Text="作者：BA2IA   QQ：21780   感谢 Codex\r\nMIT 开源 · 可自由使用、修改、分发（保留许可）",AutoSize=true});var license=new LinkLabel{Text="许可证",AutoSize=true,LinkColor=Color.LightSkyBlue};license.LinkClicked+=delegate{Open("LICENSE");};var third=new LinkLabel{Text="第三方许可与限制",AutoSize=true,LinkColor=Color.LightSkyBlue};third.LinkClicked+=delegate{Open("THIRD-PARTY-NOTICES.txt");};bottom.Controls.Add(license);bottom.Controls.Add(third);var about=new LinkLabel{Text="软件介绍 / 全部格式",AutoSize=true,LinkColor=Color.LightSkyBlue};about.LinkClicked+=delegate{ShowMessage("3DSTL 是免费的本地模型缩略图工具，让资源管理器和桌面直接显示模型预览。\\n\\n模型渲染：STL、3MF、OBJ、PLY、OFF、AMF、STEP、STP。\\n图片或路径预览：GCODE、GCO。\\n仅提取内置预览：SLDPRT、SLDASM、SLDDRW、PRTDOT、ASMDOT、DRWDOT、EPRT、EASM、EDRW。\\n\\n作者：BA2IA\\nQQ：21780\\n感谢 Codex\\n原创源码采用 MIT 许可，可自由使用、修改和分发，须保留版权及许可证。第三方组件遵循各自许可证。".Replace("\\n",Environment.NewLine),"关于 3DSTL",MessageBoxButtons.OK,MessageBoxIcon.Information);};bottom.Controls.Add(about);var logs=new LinkLabel{Text="刷新日志",AutoSize=true,LinkColor=Color.LightSkyBlue};logs.LinkClicked+=delegate{if(File.Exists(refreshLog))Process.Start(new ProcessStartInfo("notepad.exe"){UseShellExecute=true,ArgumentList={refreshLog}});else ShowMessage(english?"No refresh log yet.":"还没有刷新记录。","3DSTL",MessageBoxButtons.OK,MessageBoxIcon.Information);};layout.Controls.Add(bottom,0,6);InitUpdates(bottom,online);
 repair.Click+=async delegate{await RepairSelected();};uninstall.Click+=delegate{if(ShowMessage("在 Windows 设置中卸载 3DSTL？\r\n模型文件不会删除。","卸载",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)Run(manager.Uninstall);};refresh.Click+=delegate{if(refreshTimer.Enabled)ReadRefreshProgress();else UpdateStatus();};UpdateStatus();ApplyLanguage(this);RefreshUpdateLabels();Text=english?"3DSTL · Model Thumbnails":"3DSTL · 模型缩略图管理";if(manager.RegistryRoot=="Software\\Classes"&&RefreshRunning()){refreshTimer.Start();ReadRefreshProgress();}}
 DialogResult ShowMessage(string text,string caption,MessageBoxButtons buttons,MessageBoxIcon icon){return MessageBox.Show(this,Translate(text),Translate(caption),buttons,icon);}
 void ApplyLanguage(Control control){foreach(Control child in control.Controls){if(child is ComboBox||child is CheckedListBox||child==status)continue;if(child.Tag==null)child.Tag=child.Text;child.Text=Translate(Convert.ToString(child.Tag));ApplyLanguage(child);}}
 string Translate(string text){if(!english)return text;var map=new Dictionary<string,string>{
 {"在 Windows 资源管理器和桌面直接查看模型预览","Model thumbnails in Windows File Explorer and on your desktop"},
 {"本地处理 · 浅灰立体效果 · 不修改模型文件","Local processing · Light gray rendering · Original files stay intact"},
 {"刷新缩略图","Refresh thumbnails"},{"在 Windows 中卸载","Uninstall via Windows"},{"请在 Windows 设置中卸载 3DSTL，Windows 会自动移除缩略图关联。","Uninstall 3DSTL in Windows Settings. Windows removes the thumbnail associations."},{"已通知 Windows 刷新。返回目录按 F5 查看。","Windows has been notified. Press F5 in File Explorer."},{"一键修复 / 安装","Repair / Install"},{"卸载缩略图功能","Uninstall thumbnails"},{"刷新状态","Refresh status"},
 {"商店版由 Windows 管理格式关联；卸载时自动移除。","Windows manages Store file associations and removes them on uninstall."},
 {"已打开的桌面可能需要重新登录 Windows 后更新。","First previews may take time. Sign out/in if the desktop stays cached."},
 {"作者：BA2IA   QQ：21780   感谢 Codex","Author: BA2IA   QQ: 21780   Thanks to Codex"},
 {"MIT 开源 · 可自由使用、修改、分发（保留许可）","MIT · Free to use, modify and distribute (keep the license)"},
 {"GitHub 项目主页","GitHub project"},{"刷新日志","Refresh log"},{"第三方许可与限制","Third-party licenses & limits"},{"软件介绍 / 全部格式","About / All formats"},{"许可证","License"},
 {"在 Windows 设置中卸载 3DSTL？","Open Windows Settings to uninstall 3DSTL?"},
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
  status.Text=english?"Notifying Windows to refresh…":"正在通知 Windows 刷新，请稍候…";
  try{
   await Task.Run(()=>manager.Repair(selected));UpdateStatus();
   status.Text=english?"Windows has been notified to refresh thumbnails.":"已通知 Windows 刷新缩略图。";
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
 [STAThread]static void Main(string[] args){if(args.Length==1&&args[0]=="--version"){Console.WriteLine("3DSTL "+UpdateClient.CurrentVersion);return;}Application.EnableVisualStyles();if(args.Length>1&&(args[0]=="--repair"||args[0]=="--register-only")){try{var manager=new AssociationManager();var chosen=args.Skip(1).Where(x=>AssociationManager.Formats.Contains(x)).ToArray();if(args[0]=="--register-only")manager.Rebind(chosen);else manager.Repair(chosen);}catch(Exception e){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"repair-error.txt"),e.Message);Environment.ExitCode=1;}return;}if(args.Contains("--selftest-ui"))return;if(args.Contains("--selftest"))return;using(var form=new ManagerForm(args.Length==0)){if(args.Contains("--updated"))form.updateStatus.Text="已更新到 v"+UpdateClient.CurrentVersion+"。缩略图组件如未更新，请重新登录 Windows。";if(args.Length==2&&(args[0]=="--snapshot"||args[0]=="--snapshot-en")){form.snapshotMode=true;form.Controls.Find("LanguageSelector",true).OfType<ComboBox>().First().SelectedIndex=args[0]=="--snapshot-en"?1:0;if(args[0]=="--snapshot-en"){form.english=true;form.UpdateStatus();form.ApplyLanguage(form);form.RefreshUpdateLabels();form.Text="3DSTL · Model Thumbnails";}form.Show();Application.DoEvents();using(var bmp=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bmp,new Rectangle(Point.Empty,bmp.Size));bmp.Save(args[1]);}return;}Application.Run(form);}}
}
