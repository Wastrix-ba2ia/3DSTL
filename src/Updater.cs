using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

class Updater : Form {
 readonly Label message = new Label { Dock = DockStyle.Fill, Padding = new Padding(20), Text = "正在准备更新 / Preparing update…" };
 readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Bottom, Height = 12, Style = ProgressBarStyle.Marquee };
 readonly string taskFile;
 bool applying;
 Updater(string file) {
  taskFile = file; Text = "3DSTL · 在线升级 / Update"; ClientSize = new Size(550,140);
  StartPosition = FormStartPosition.CenterScreen; Font = new Font("Microsoft YaHei UI",10);
  Controls.Add(message); Controls.Add(progress);
  FormClosing += (_, e) => { if (applying) e.Cancel = true; };
  Shown += async (_, _) => {
   applying = true;
   try { await Task.Run(() => Install(taskFile, text => BeginInvoke(new Action(() => message.Text = text)))); }
   catch (Exception e) { Environment.ExitCode = 1; MessageBox.Show(this, "更新未完成，原版本保留。\r\nUpdate failed; the previous version is retained.\r\n\r\n" + e.Message, "3DSTL", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
   finally { applying = false; Close(); }
  };
 }
 static readonly string[] Formats = { "3mf","stl","obj","ply","off","amf","gcode","gco","step","stp","sldprt","sldasm","slddrw","prtdot","asmdot","drwdot","eprt","easm","edrw" };
 static string Id(string ext) { int n = Array.IndexOf(Formats, ext); return n == 0 ? "{B7E33521-1381-4ED2-9C3F-989B9CF937E8}" : "{A37F1000-17A0-4D18-BA10-" + n.ToString("000000000000") + "}"; }
 static void Install(string file, Action<string> report) {
  using var gate = new Mutex(false, "Local\\3DSTL.OnlineUpdate");
  bool locked; try { locked = gate.WaitOne(0); } catch (AbandonedMutexException) { locked = true; }
  if (!locked) throw new IOException("已有升级任务在运行 / Another update is running");
  try {
   var task = JsonSerializer.Deserialize<UpdateTask>(File.ReadAllText(file)) ?? throw new InvalidDataException("Missing update task");
   string version = UpdateClient.ParseVersion(task.Version).ToString(3);
   string data = Path.GetFullPath(task.DataRoot);
   var allowed = new[] { Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Environment.GetEnvironmentVariable("LOCALAPPDATA") }
    .Where(path => !string.IsNullOrEmpty(path)).Select(path => Path.GetFullPath(Path.Combine(path,"3DSTL")));
   if (!allowed.Contains(data, StringComparer.OrdinalIgnoreCase)) throw new InvalidDataException("Unexpected application data directory");
   if (!Path.GetFullPath(task.Zip).StartsWith(data + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !System.Text.RegularExpressions.Regex.IsMatch(task.Sha256 ?? "", "^[a-fA-F0-9]{64}$"))
    throw new InvalidDataException("Unexpected staged update package");
   Directory.CreateDirectory(data);
   using var log = new StreamWriter(Path.Combine(data, "update.log"), true) { AutoFlush = true };
   log.WriteLine(DateTimeOffset.Now + " Installing " + version);
   var oldServers = new Dictionary<string,string>(); string installed = null;
   string shortcutPath = null, shortcutHash = null; byte[] shortcutBackup = null;
   try {
    report("正在校验安装包 / Verifying download…");
    if (UpdateClient.Hash(task.Zip) != task.Sha256.ToLowerInvariant()) throw new InvalidDataException("SHA256 verification failed");
    report("正在解压新版，原版保留 / Extracting new version…");
    string versions = Path.Combine(data,"Versions"); Directory.CreateDirectory(versions);
    // Extract into an unreferenced version directory. Moving thousands of freshly
    // scanned files can fail while antivirus holds a directory handle; activation
    // still happens only after the complete payload and binary version validate.
    installed = Path.Combine(versions,version + "-" + Guid.NewGuid().ToString("N").Substring(0,8));
    UpdateClient.Extract(task.Zip, installed, version);
    var binaryVersion = FileVersionInfo.GetVersionInfo(Path.Combine(installed,"manager","3DSTL.dll"));
    if (new Version(binaryVersion.FileMajorPart,binaryVersion.FileMinorPart,binaryVersion.FileBuildPart) != UpdateClient.ParseVersion(version))
     throw new InvalidDataException("Application binary version does not match release");
    report("正在等待管理器退出 / Waiting for the manager to close…");
    try {
     using var parent = Process.GetProcessById(task.ParentId);
     if (string.Equals(parent.MainModule.FileName, Path.GetFullPath(task.PreviousExe), StringComparison.OrdinalIgnoreCase) && !parent.WaitForExit(30000))
      throw new IOException("请关闭旧版 3DSTL 后重试 / Close the old manager and retry");
    } catch (ArgumentException) { }
    var active = new List<string>();
    foreach (string ext in (task.Formats ?? Array.Empty<string>()).Distinct()) {
     if (!Formats.Contains(ext)) throw new InvalidDataException("Unknown thumbnail format");
     using var association = Registry.CurrentUser.OpenSubKey("Software\\Classes\\." + ext + "\\shellex\\{e357fccd-a995-4576-b01f-234630154e96}");
     if (!string.Equals(Convert.ToString(association?.GetValue("")),Id(ext),StringComparison.OrdinalIgnoreCase)) continue;
     string key = "Software\\Classes\\CLSID\\" + Id(ext) + "\\InprocServer32";
     using var server = Registry.CurrentUser.OpenSubKey(key);
     oldServers[key] = server?.GetValue("") as string; active.Add(ext);
    }
    string manager = Path.Combine(installed,"manager","3DSTL.exe");
    report("正在切换到新版 / Activating the new version…");
    if (active.Count > 0) {
     var start = new ProcessStartInfo(manager) { UseShellExecute = false, CreateNoWindow = true };
     start.ArgumentList.Add("--register-only"); foreach (string ext in active) start.ArgumentList.Add(ext);
     using var process = Process.Start(start);
     if (!process.WaitForExit(60000)) { process.Kill(true); process.WaitForExit(); throw new IOException("New version registration timed out"); }
     if (process.ExitCode != 0) throw new IOException("New version registration failed; restoring previous components");
     foreach (string ext in active) {
      using var key = Registry.CurrentUser.OpenSubKey("Software\\Classes\\CLSID\\"+Id(ext)+"\\InprocServer32");
      if (!string.Equals(Convert.ToString(key?.GetValue("")), Path.Combine(installed,"shell","Aux3mfThumbnail.comhost.dll"),StringComparison.OrdinalIgnoreCase))
       throw new IOException("New component registration could not be verified");
     }
    }
    // Recovery data, language preferences, and model files are outside Versions.
    object shell = null, shortcut = null;
    try {
     shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
     shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"3DSTL.lnk");
     if (File.Exists(shortcutPath)) shortcutBackup = File.ReadAllBytes(shortcutPath);
     dynamic link = ((dynamic)shell).CreateShortcut(shortcutPath); shortcut = link;
     link.TargetPath = manager; link.WorkingDirectory = installed; link.Save();
     shortcutHash = UpdateClient.Hash(shortcutPath);
    } finally { if(shortcut!=null)Marshal.ReleaseComObject(shortcut);if(shell!=null)Marshal.ReleaseComObject(shell); }
    // Ask the desktop shell to launch the manager independently of the updater's
    // process tree (important when a launcher owns a kill-on-close job object).
    object desktop=null;
    try{desktop=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));((dynamic)desktop).ShellExecute(manager,"--updated",installed,"open",1);}
    finally{if(desktop!=null)Marshal.ReleaseComObject(desktop);}
    bool launched=false;
    for(int attempt=0;attempt<20&&!launched;attempt++){
     foreach(var app in Process.GetProcessesByName("3DSTL"))using(app){try{if(string.Equals(app.MainModule.FileName,manager,StringComparison.OrdinalIgnoreCase)){launched=true;log.WriteLine("Installed "+installed+"; manager PID "+app.Id);break;}}catch(InvalidOperationException){}}
     if(!launched)Thread.Sleep(250);
    }
    if(!launched)throw new IOException("New manager could not start");
   } catch (Exception e) {
    // Restore only entries still pointing at this attempted installation.
    foreach (var previous in oldServers) {
     using var key = Registry.CurrentUser.OpenSubKey(previous.Key, true);
     string current = key?.GetValue("") as string;
     if (installed != null && current != null && current.StartsWith(installed + Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) {
      if (previous.Value == null) key.DeleteValue("",false); else key.SetValue("",previous.Value);
     }
    }
    if(shortcutHash!=null&&File.Exists(shortcutPath)&&UpdateClient.Hash(shortcutPath)==shortcutHash){
     if(shortcutBackup==null)File.Delete(shortcutPath);else File.WriteAllBytes(shortcutPath,shortcutBackup);
    }
    log.WriteLine("FAILED " + e); throw;
   }
  } finally { gate.ReleaseMutex(); }
 }
 [STAThread] static int Main(string[] args) {
  if(args.Length==1&&args[0]=="--version"){Console.WriteLine("3DSTL.Update "+UpdateClient.CurrentVersion);return 0;}
  if (args.Length == 2 && args[0] == "--apply-quiet") {
   try { Install(args[1],Console.WriteLine); return 0; } catch(Exception e) { Console.Error.WriteLine(e); return 1; }
  }
  if (args.Length != 2 || args[0] != "--apply") return 2;
  Application.EnableVisualStyles(); Application.Run(new Updater(args[1])); return Environment.ExitCode;
 }
}
