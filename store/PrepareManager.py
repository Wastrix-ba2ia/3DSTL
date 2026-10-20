from pathlib import Path
root=Path(__file__).resolve().parents[1]
s=(root/'src/Manager.cs').read_text(encoding='utf-8-sig')
a=s.index('class AssociationManager {');b=s.index('partial class ManagerForm:Form {')
old=s[a:b]
formats=old[old.index('public static readonly string[] Formats='):old.index(';',old.index('public static readonly string[] Formats='))+1]
stub='''class AssociationManager {
 FORMATS
 public string Root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..")),RegistryRoot="",BackupFile="";
 public static string Id(string ext){return ext;}
 public bool Active(string ext){return true;}
 public void Repair(string[] formats,bool refreshDesktop=true){Notify();}
 public void Rebind(string[] formats){Notify();}
 public string Uninstall(){Process.Start(new ProcessStartInfo("ms-settings:appsfeatures"){UseShellExecute=true});return "请在 Windows 设置中卸载 3DSTL，Windows 会自动移除缩略图关联。";}
 [DllImport("shell32.dll")]static extern void SHChangeNotify(uint e,uint f,IntPtr a,IntPtr b);
 public static void Notify(){SHChangeNotify(0x08000000,0,IntPtr.Zero,IntPtr.Zero);}
}
'''.replace('FORMATS',formats)
s=s[:a]+stub+s[b:]
s=s.replace('status.Text=english?"Repair complete: "+selected.Length+" formats.":"修复完成："+selected.Length+" 种格式。";', 'status.Text=english?"Windows has been notified to refresh thumbnails.":"已通知 Windows 刷新缩略图。";')
s=s.replace('formats.CheckOnClick=true;','formats.CheckOnClick=false;formats.Enabled=false;')
s=s.replace('repair.Text="一键修复 / 安装"','repair.Text="刷新缩略图"').replace('uninstall.Text="卸载缩略图功能"','uninstall.Text="在 Windows 中卸载"')
s=s.replace('修复勾选的格式；卸载恢复全部格式的旧设置。首次生成较慢。','商店版由 Windows 管理格式关联；卸载时自动移除。')
s=s.replace('"已修复 "+selected.Length+" 种格式。返回目录按 F5 查看。"','"已通知 Windows 刷新。返回目录按 F5 查看。"')
s=s.replace('"已修复 "+selected.Length+" 种格式。桌面预览在后台更新，大 STL 最后处理。"','"已通知 Windows 刷新。返回目录按 F5 查看。"')
s=s.replace('{"一键修复 / 安装","Repair / Install"}', '{"刷新缩略图","Refresh thumbnails"},{"在 Windows 中卸载","Uninstall via Windows"},{"请在 Windows 设置中卸载 3DSTL，Windows 会自动移除缩略图关联。","Uninstall 3DSTL in Windows Settings. Windows removes the thumbnail associations."},{"已通知 Windows 刷新。返回目录按 F5 查看。","Windows has been notified. Press F5 in File Explorer."},{"一键修复 / 安装","Repair / Install"}')
s=s.replace('Repair selected formats. Uninstall restores previous associations.', 'Windows manages Store file associations and removes them on uninstall.')
s=s.replace('Uninstall all model thumbnail handlers and restore previous associations?', 'Open Windows Settings to uninstall 3DSTL?')
s=s.replace('卸载全部模型缩略图功能并恢复原关联？', '在 Windows 设置中卸载 3DSTL？')
s=s.replace('Uninstall all model thumbnail handlers and restore previous associations?', 'Open Windows Settings to uninstall 3DSTL?')
s=s.replace('正在修复勾选的格式，请稍候…','正在通知 Windows 刷新，请稍候…').replace('Repairing selected formats…','Notifying Windows to refresh…')
# The Store build has no desktop refresh queue/log. Do not expose a dead log link.
s=s.replace('bottom.Controls.Add(logs);', '')
s=s.replace('if(args.Contains("--selftest")){SelfTest();return;}','if(args.Contains("--selftest"))return;')
s=s.replace('if(args.Contains("--selftest-ui")){SelfTestUi();return;}','if(args.Contains("--selftest-ui"))return;')
a=s.find(' static void SelfTestUi(){')
if a>=0:s=s[:a]+'}'+chr(10)
# Fail the build if generated localization keys collide (the 0.2.0.0 certification failure).
import re,collections
start=s.index('var map=new Dictionary<string,string>{');end=s.index('};',start)
keys=re.findall(r'\{"([^"\n]+)",',s[start:end])
duplicates=[key for key,count in collections.Counter(keys).items() if count>1]
if duplicates:raise RuntimeError('Duplicate generated translation keys: '+repr(duplicates))
(root/'store/StoreManager.cs').write_text(s,encoding='utf-8-sig')
