using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public sealed class UpdateSettings {
 public bool Automatic { get; set; } = true;
 public DateTimeOffset LastCheckUtc { get; set; }
}
public sealed class UpdateRelease {
 public string Version { get; set; }
 public string Name { get; set; }
 public string Url { get; set; }
 public string ChecksumUrl { get; set; }
 public long Size { get; set; }
 public string Digest { get; set; }
}
public sealed class UpdateTask {
 public string Zip { get; set; }
 public string Sha256 { get; set; }
 public string Version { get; set; }
 public string DataRoot { get; set; }
 public string PreviousExe { get; set; }
 public int ParentId { get; set; }
 public string[] Formats { get; set; } = Array.Empty<string>();
}
public sealed class UpdateClient : IDisposable {
 public const string Repository = "https://github.com/Wastrix-ba2ia/3DSTL";
 public const string LatestApi = "https://api.github.com/repos/Wastrix-ba2ia/3DSTL/releases/latest";
 public const long MaximumArchive = 1024L * 1024 * 1024;
 public static string CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
 public static bool StoreBuild {
  get {
#if STORE
   return true;
#else
   return false;
#endif
  }
 }
 readonly HttpClient http;
 public UpdateClient(HttpMessageHandler handler = null) {
  http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false });
  http.Timeout = Timeout.InfiniteTimeSpan;
  http.DefaultRequestHeaders.UserAgent.ParseAdd("3DSTL/" + CurrentVersion);
  http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
 }
 public static Version ParseVersion(string value) {
  string text = value?.TrimStart('v', 'V');
  if (text == null || !Regex.IsMatch(text, @"^\d+\.\d+\.\d+$") || !Version.TryParse(text, out var version))
   throw new InvalidDataException("Invalid stable release version");
  return version;
 }
 public static UpdateRelease ParseRelease(string json, string current) {
  using var doc = JsonDocument.Parse(json); var root = doc.RootElement;
  if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) return null;
  string tag = root.GetProperty("tag_name").GetString(); var version = ParseVersion(tag);
  if (version <= ParseVersion(current)) return null;
  string name = "3DSTL-" + version.ToString(3) + "-win-x64.zip";
  JsonElement package = default, checksum = default;
  foreach (var asset in root.GetProperty("assets").EnumerateArray()) {
   if (asset.GetProperty("name").GetString() == name) package = asset;
   if (asset.GetProperty("name").GetString() == "SHA256SUMS.txt") checksum = asset;
  }
  if (package.ValueKind == JsonValueKind.Undefined || checksum.ValueKind == JsonValueKind.Undefined)
   throw new InvalidDataException("Release is missing the Windows x64 package or SHA256SUMS.txt");
  string expected = Repository + "/releases/download/" + Uri.EscapeDataString(tag) + "/";
  string url = package.GetProperty("browser_download_url").GetString(), sums = checksum.GetProperty("browser_download_url").GetString();
  if (url != expected + name || sums != expected + "SHA256SUMS.txt") throw new InvalidDataException("Unexpected release download source");
  long size = package.GetProperty("size").GetInt64();
  if (size <= 0 || size > MaximumArchive) throw new InvalidDataException("Unexpected update package size");
  string digest = package.TryGetProperty("digest", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() : null;
  return new UpdateRelease { Version = version.ToString(3), Name = name, Url = url, ChecksumUrl = sums, Size = size, Digest = digest };
 }
 static bool Allowed(Uri uri) => uri.Scheme == "https" && uri.IsDefaultPort &&
  new[] { "api.github.com", "github.com", "release-assets.githubusercontent.com", "objects.githubusercontent.com" }.Contains(uri.Host.ToLowerInvariant());
 async Task<HttpResponseMessage> Get(string url, CancellationToken token) {
  var uri = new Uri(url);
  for (int hop = 0; hop < 6; hop++) {
   if (!Allowed(uri)) throw new InvalidDataException("Update redirect is not an approved GitHub HTTPS address");
   var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
   int status = (int)response.StatusCode;
   if (status >= 300 && status < 400) {
    var location = response.Headers.Location; response.Dispose();
    if (location == null) throw new InvalidDataException("Missing update redirect address");
    uri = location.IsAbsoluteUri ? location : new Uri(uri, location); continue;
   }
   try { response.EnsureSuccessStatusCode(); return response; } catch { response.Dispose(); throw; }
  }
  throw new IOException("Too many update download redirects");
 }
 async Task<string> SmallText(string url, CancellationToken token) {
  using var response = await Get(url, token).ConfigureAwait(false);
  using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
  using var output = new MemoryStream(); byte[] buffer = new byte[8192]; int read;
  while ((read = await stream.ReadAsync(buffer, token).ConfigureAwait(false)) != 0) {
   if (output.Length + read > 1024 * 1024) throw new InvalidDataException("Update metadata is too large");
   output.Write(buffer, 0, read);
  }
  return System.Text.Encoding.UTF8.GetString(output.ToArray());
 }
 public async Task<UpdateRelease> Check(string current, CancellationToken token) => ParseRelease(await SmallText(LatestApi, token).ConfigureAwait(false), current);
 public static string ParseChecksum(string text, string name) {
  var matches = text.Split('\n').Select(line => Regex.Match(line.Trim(), @"^([a-fA-F0-9]{64})\s+\*?(.+)$"))
   .Where(match => match.Success && match.Groups[2].Value == name).ToArray();
  if (matches.Length != 1) throw new InvalidDataException("Missing or ambiguous package checksum");
  return matches[0].Groups[1].Value.ToLowerInvariant();
 }
 public static string Hash(string path) { using var file = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant(); }
 public async Task<(string Path, string Hash)> Download(UpdateRelease release, string directory, IProgress<int> progress, CancellationToken token) {
  string hash = ParseChecksum(await SmallText(release.ChecksumUrl, token).ConfigureAwait(false), release.Name);
  if (!string.IsNullOrEmpty(release.Digest) && !string.Equals(release.Digest, "sha256:" + hash, StringComparison.OrdinalIgnoreCase))
   throw new InvalidDataException("GitHub digest does not match the published checksum");
  Directory.CreateDirectory(directory); string target = System.IO.Path.Combine(directory, release.Name);
  if (File.Exists(target) && new FileInfo(target).Length == release.Size && Hash(target) == hash) return (target, hash);
  string temporary = target + ".part-" + Guid.NewGuid().ToString("N");
  try {
   using (var response = await Get(release.Url, token).ConfigureAwait(false)) {
    using var input = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
    using var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, true);
    byte[] buffer = new byte[65536]; long total = 0; int read, last = -1;
    while ((read = await input.ReadAsync(buffer, token).ConfigureAwait(false)) != 0) {
     total += read; if (total > release.Size || total > MaximumArchive) throw new InvalidDataException("Downloaded package is larger than declared");
     await output.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
     int percent = (int)(total * 100 / release.Size); if (percent != last) { progress?.Report(percent); last = percent; }
    }
    if (total != release.Size) throw new InvalidDataException("Incomplete update package");
   }
   token.ThrowIfCancellationRequested();
   if (Hash(temporary) != hash) throw new InvalidDataException("Update package SHA256 verification failed");
   File.Move(temporary, target, true); return (target, hash);
  } finally { if (File.Exists(temporary)) File.Delete(temporary); }
 }
 public static void Extract(string zip, string destination, string version) {
  version = ParseVersion(version).ToString(3);
  if (Directory.Exists(destination)) throw new IOException("Update destination already exists");
  string root = System.IO.Path.GetFullPath(destination) + System.IO.Path.DirectorySeparatorChar;
  string prefix = "3DSTL-" + version + "-win-x64/";
  using var archive = ZipFile.OpenRead(zip);
  if (archive.Entries.Count > 20000) throw new InvalidDataException("Too many update files");
  long total = 0;
  // Validate every destination before writing any files.
  foreach (var entry in archive.Entries) {
   string name = entry.FullName.Replace('\\', '/');
   if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Contains(':')) throw new InvalidDataException("Unexpected archive root");
   string relative = name.Substring(prefix.Length);
   string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, relative));
   if (relative.Split('/').Any(segment => segment == "..") || (relative.Length > 0 && !path.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
    throw new InvalidDataException("Unsafe archive path");
   if (((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000 || (entry.ExternalAttributes & 0x400) != 0)
    throw new InvalidDataException("Archive links are not supported");
   total += entry.Length; if (total > 2L * 1024 * 1024 * 1024) throw new InvalidDataException("Unpacked update exceeds size limit");
  }
  Directory.CreateDirectory(destination);
  foreach (var entry in archive.Entries) {
   string relative = entry.FullName.Replace('\\', '/').Substring(prefix.Length);
   if (relative.Length == 0 || relative.EndsWith('/')) continue;
   string path = System.IO.Path.Combine(destination, relative);
   Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)); entry.ExtractToFile(path, false);
  }
  foreach (string file in new[] { "manager/3DSTL.exe", "manager/3DSTL.dll", "manager/updater/3DSTL.Update.exe", "manager/refresh/3DSTL.Refresh.exe", "shell/Aux3mfThumbnail.comhost.dll", "shell/Aux3mfThumbnail.dll", "release.json" })
   if (!File.Exists(System.IO.Path.Combine(destination, file))) throw new InvalidDataException("Incomplete update payload: " + file);
  using var manifest = JsonDocument.Parse(File.ReadAllText(System.IO.Path.Combine(destination, "release.json")));
  if (manifest.RootElement.GetProperty("product").GetString() != "3DSTL" || manifest.RootElement.GetProperty("version").GetString() != version || manifest.RootElement.GetProperty("platform").GetString() != "win-x64")
   throw new InvalidDataException("Update manifest identity does not match");
 }
 public void Dispose() => http.Dispose();
}
