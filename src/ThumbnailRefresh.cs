using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

[ComImport, Guid("f676c15d-596a-4ce2-8234-33996f445db1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface ThumbnailCache {
 void GetThumbnail([MarshalAs(UnmanagedType.Interface)] RefreshShellItem item, uint size, uint flags,
  out RefreshBitmap bitmap, out uint cacheFlags, out RefreshThumbId id);
}
[StructLayout(LayoutKind.Sequential)] struct RefreshThumbId { public Guid Value; }
[ComImport, Guid("091162a4-bc96-411f-aae8-c5122cd03363"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface RefreshBitmap {
 void GetSharedBitmap(out IntPtr bitmap); void GetSize(out Size size); void GetFormat(out uint format);
 void InitializeBitmap(IntPtr bitmap, uint format); void Detach(out IntPtr bitmap);
}
[ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface RefreshShellItem { }

class ThumbnailRefresh {
 [DllImport("shell32.dll", CharSet=CharSet.Unicode, PreserveSig=false)]
 static extern void SHCreateItemFromParsingName(string path, IntPtr bind, ref Guid id,
  [MarshalAs(UnmanagedType.Interface)] out RefreshShellItem item);
 [DllImport("shell32.dll", CharSet=CharSet.Unicode)]
 static extern void SHChangeNotify(uint evt, uint flags, string item, IntPtr other);
 static readonly string[] Extensions = { ".stl", ".3mf", ".obj", ".ply", ".off", ".amf", ".step", ".stp", ".gcode", ".gco" };
 static bool Enabled(string file) {
  using var key = Registry.CurrentUser.OpenSubKey("Software\\Classes\\" + Path.GetExtension(file) + "\\shellex\\{e357fccd-a995-4576-b01f-234630154e96}");
  string value = Convert.ToString(key?.GetValue(""));
  return value == "{B7E33521-1381-4ED2-9C3F-989B9CF937E8}" || value.StartsWith("{A37F1000-17A0-4D18-BA10-", StringComparison.OrdinalIgnoreCase);
 }
 // Used by the background refresher and by the queue regression test.
 public static IEnumerable<string> OrderFiles(IEnumerable<string> files) {
  var jobs = new List<(string File, bool Large, long Bytes)>();
  foreach (string file in files) {
   try { jobs.Add((file, LargeStl.IsLarge(file), new FileInfo(file).Length)); }
   catch (IOException) { jobs.Add((file, false, long.MaxValue)); }
   catch (UnauthorizedAccessException) { jobs.Add((file, false, long.MaxValue)); }
  }
  return jobs.OrderBy(job => job.Large).ThenBy(job => job.Bytes).ThenBy(job => job.File, StringComparer.OrdinalIgnoreCase).Select(job => job.File);
 }
 [STAThread] static int Main(string[] args) {
  if(args.Length==1&&args[0]=="--version"){Console.WriteLine("3DSTL.Refresh "+System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3));return 0;}
  using var gate = new Mutex(false, "Local\\3DSTL.DesktopThumbnailRefresh");
  // Serialize refreshes. Do not discard a new refresh requested during a run.
  try { gate.WaitOne(); } catch (AbandonedMutexException) { }
  try {
   Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
   string logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "3DSTL");
   Directory.CreateDirectory(logDirectory);
   using var log = new StreamWriter(Path.Combine(logDirectory, "thumbnail-refresh.log"), false) { AutoFlush = true };
   log.WriteLine("Desktop: " + Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
   foreach (string ext in new[] { ".stl", ".3mf" }) { using var key = Registry.CurrentUser.OpenSubKey("Software\\Classes\\" + ext + "\\shellex\\{e357fccd-a995-4576-b01f-234630154e96}"); log.WriteLine("Handler " + ext + ": " + Convert.ToString(key?.GetValue(""))); }
   var candidates = args.Length == 0
    ? Directory.EnumerateFiles(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)).Where(file => Extensions.Contains(Path.GetExtension(file).ToLowerInvariant()) && Enabled(file))
    : args.Select(Path.GetFullPath);
   var files = OrderFiles(candidates).ToArray();
   log.WriteLine("Refresh started: " + DateTimeOffset.Now);
   foreach (var file in files) {
    try { log.WriteLine("QUEUED " + (LargeStl.IsLarge(file) ? "LARGE " : "NORMAL ") + file); }
    catch (IOException) { log.WriteLine("QUEUED UNAVAILABLE " + file); }
    catch (UnauthorizedAccessException) { log.WriteLine("QUEUED UNAVAILABLE " + file); }
   }
   int failures = 0;
   foreach (string file in files) {
    log.WriteLine("CURRENT " + file);
    object cache = null; RefreshShellItem item = null;
    try {
     string extension=Path.GetExtension(file).ToLowerInvariant();
     long limit = extension==".stl" ? LargeStl.InputLimit : (extension==".obj"||extension==".3mf") ? 512L*1024*1024 : 128L * 1024 * 1024;
     if (new FileInfo(file).Length > limit) throw new IOException("Input exceeds " + limit + " bytes");
     Guid iid = typeof(RefreshShellItem).GUID;
     SHCreateItemFromParsingName(file, IntPtr.Zero, ref iid, out item);
     cache = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("50ef4544-ac9f-4a8e-b21b-8a26180db13f")));
     // Start with the largest result so Windows can reuse it for smaller sizes.
     foreach (uint size in new uint[] { 256, 96, 48 }) {
      RefreshBitmap bitmap = null;
      try { ((ThumbnailCache)cache).GetThumbnail(item, size, size==256?4u:0x40u, out bitmap, out _, out _); }
      finally { if (bitmap != null) Marshal.ReleaseComObject(bitmap); }
     }
     SHChangeNotify(0x1000, 0x1005, Path.GetDirectoryName(file), IntPtr.Zero);
     log.WriteLine("OK " + file);
    } catch (Exception e) { failures++; log.WriteLine("FAILED " + file + " " + e.HResult.ToString("X") + " " + e.Message); }
    finally {
     if (item != null) Marshal.ReleaseComObject(item);
     if (cache != null) Marshal.ReleaseComObject(cache);
    }
   }
   log.WriteLine("Completed: " + (files.Length - failures) + " succeeded, " + failures + " failed.");
   return failures == 0 ? 0 : 1;
  } finally { gate.ReleaseMutex(); }
 }
}
