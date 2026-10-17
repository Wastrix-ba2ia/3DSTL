using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

partial class ManagerForm {
 readonly Button updateButton = new Button { AutoSize = true, Height = 34, FlatStyle = FlatStyle.Flat };
 readonly CheckBox automaticUpdates = new CheckBox { AutoSize = true, Padding = new Padding(8,5,0,0) };
 readonly Label updateStatus = new Label { AutoSize = false, Width = 760, Height = 46 };
 readonly CancellationTokenSource updateLifetime = new CancellationTokenSource();
 readonly string updateData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"3DSTL");
 UpdateSettings updateSettings;
 UpdateRelease availableUpdate;
 string stagedUpdate, stagedHash;
 bool updateBusy;
 void InitUpdates(FlowLayoutPanel bottom, bool online) {
  var github = new LinkLabel { Text = "GitHub 项目主页", AutoSize = true, LinkColor = Color.LightSkyBlue };
  github.LinkClicked += (_,_) => Process.Start(new ProcessStartInfo(UpdateClient.Repository) { UseShellExecute = true });
  bottom.Controls.Add(github); bottom.SetFlowBreak(github,true);
  updateSettings = ReadUpdateSettings();
  updateButton.BackColor = Color.FromArgb(38,71,94);updateButton.ForeColor=ForeColor;
  automaticUpdates.Checked = updateSettings.Automatic && !UpdateClient.StoreBuild;
  automaticUpdates.Enabled = !UpdateClient.StoreBuild;
  bottom.Controls.Add(updateButton);bottom.Controls.Add(automaticUpdates);bottom.SetFlowBreak(automaticUpdates,true);
  updateStatus.ForeColor=Color.FromArgb(170,187,204);bottom.Controls.Add(updateStatus);
  updateButton.Click += async (_,_) => {
   if (UpdateClient.StoreBuild) { Process.Start(new ProcessStartInfo("https://apps.microsoft.com/detail/9NQ6SSHMXRQM") { UseShellExecute=true });return; }
   if (stagedUpdate != null) { InstallUpdate();return; }
   await CheckOnlineUpdate(false);
  };
  automaticUpdates.CheckedChanged += (_,_) => { updateSettings.Automatic=automaticUpdates.Checked;SaveUpdateSettings(); };
  FormClosed += (_,_) => updateLifetime.Cancel();
  Shown += async (_,_) => { if(online&&!UpdateClient.StoreBuild&&updateSettings.Automatic&&DateTimeOffset.UtcNow-updateSettings.LastCheckUtc>TimeSpan.FromHours(24))await CheckOnlineUpdate(true); };
  RefreshUpdateLabels();
 }
 UpdateSettings ReadUpdateSettings(){try{return JsonSerializer.Deserialize<UpdateSettings>(File.ReadAllText(Path.Combine(updateData,"updates.json")))??new UpdateSettings();}catch(IOException){return new UpdateSettings();}catch(JsonException){return new UpdateSettings();}catch(UnauthorizedAccessException){return new UpdateSettings();}}
 void SaveUpdateSettings(){try{Directory.CreateDirectory(updateData);string file=Path.Combine(updateData,"updates.json");File.WriteAllText(file+".tmp",JsonSerializer.Serialize(updateSettings));File.Move(file+".tmp",file,true);}catch(IOException){}catch(UnauthorizedAccessException){}}
 void RefreshUpdateLabels(){
  updateButton.Text=UpdateClient.StoreBuild?(english?"Open Microsoft Store":"打开 Microsoft Store"):(stagedUpdate!=null?(english?"Install update v":"安装更新 v")+availableUpdate.Version:(english?"Check for updates":"检查更新"));
  automaticUpdates.Text=english?"Automatically check and download updates":"自动检查并下载更新";
  automaticUpdates.Tag=automaticUpdates.Text;updateButton.Tag=updateButton.Text;
  updateStatus.Text=UpdateClient.StoreBuild?(english?"Store edition is updated by Microsoft Store.":"商店版由 Microsoft Store 更新。"):(english?"Current version: v":"当前版本：v")+UpdateClient.CurrentVersion;
 }
 async Task CheckOnlineUpdate(bool automatic){
  if(updateBusy)return;updateBusy=true;updateButton.Enabled=false;
  try{
   updateStatus.Text=english?"Checking GitHub releases…":"正在检查 GitHub 新版本…";
   using var client=new UpdateClient();using var check=CancellationTokenSource.CreateLinkedTokenSource(updateLifetime.Token);check.CancelAfter(TimeSpan.FromSeconds(20));
   availableUpdate=await client.Check(UpdateClient.CurrentVersion,check.Token);
   updateSettings.LastCheckUtc=DateTimeOffset.UtcNow;SaveUpdateSettings();
   if(availableUpdate==null){updateStatus.Text=(english?"No available updates. Current version: v":"没有可用更新，当前版本：v")+UpdateClient.CurrentVersion;return;}
   using var download=CancellationTokenSource.CreateLinkedTokenSource(updateLifetime.Token);download.CancelAfter(TimeSpan.FromMinutes(30));
   var progress=new Progress<int>(percent=>{if(!IsDisposed)updateStatus.Text=(english?"Downloading v":"正在下载 v")+availableUpdate.Version+" · "+percent+"%";});
   var result=await client.Download(availableUpdate,Path.Combine(updateData,"Updates",availableUpdate.Version),progress,download.Token);
   stagedUpdate=result.Path;stagedHash=result.Hash;
   updateButton.Text=(english?"Install update v":"安装更新 v")+availableUpdate.Version;updateButton.Tag=updateButton.Text;
   updateStatus.Text=english?"Download verified. Click Install update to switch versions.":"下载及校验完成，点击“安装更新”切换到新版。";
  }catch(OperationCanceledException){if(!IsDisposed)updateStatus.Text=english?"Update cancelled or timed out; retry when connected.":"更新已取消或连接超时，可稍后重试。";}
  catch(Exception e){if(!IsDisposed){updateStatus.Text=english?"Update failed. Click Check for updates to retry.":"检查或下载失败，可点击“检查更新”重试。";if(!automatic)ShowMessage(e.Message,"3DSTL 更新 / Update",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
  finally{updateBusy=false;if(!IsDisposed)updateButton.Enabled=true;}
 }
 void InstallUpdate(){
  try{
   string helper=Path.Combine(AppContext.BaseDirectory,"updater","3DSTL.Update.exe");
   if(!File.Exists(helper))throw new IOException(english?"Update installer missing. Download the complete package from GitHub.":"缺少升级组件，请从 GitHub 下载完整安装包。");
   if(RefreshRunning()){updateStatus.Text=english?"Wait for the desktop refresh to finish, then install the update.":"桌面预览仍在生成，请等任务完成后再安装更新。";return;}
   var task=new UpdateTask{Zip=stagedUpdate,Sha256=stagedHash,Version=availableUpdate.Version,DataRoot=updateData,PreviousExe=Environment.ProcessPath,ParentId=Environment.ProcessId,Formats=AssociationManager.Formats.Where(manager.Active).ToArray()};
   string taskFile=Path.Combine(updateData,"Updates","task-"+Guid.NewGuid().ToString("N")+".json");File.WriteAllText(taskFile,JsonSerializer.Serialize(task));
   var start=new ProcessStartInfo(helper){UseShellExecute=false};start.ArgumentList.Add("--apply");start.ArgumentList.Add(taskFile);
   Process.Start(start);Close();
  }catch(Exception e){ShowMessage(e.Message,"3DSTL 更新 / Update",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
 }
}
