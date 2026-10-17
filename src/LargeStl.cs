using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

// Full-file scans with bounded display geometry; never modifies the source.
public static class LargeStl {
 public const long InputLimit = 1024L * 1024 * 1024;
 public const uint Threshold = 1500000;
 const int PreviewFaces = 400000, Cells = 128;
 public static bool IsLarge(string path) {
  if (Path.GetExtension(path).Equals(".3mf", StringComparison.OrdinalIgnoreCase)) {
   using var archive = ZipFile.OpenRead(path);
   return archive.Entries.Where(e => e.FullName.EndsWith(".model",StringComparison.OrdinalIgnoreCase)).Sum(e => e.Length) > 64L * 1024 * 1024;
  }
  if (!Path.GetExtension(path).Equals(".stl", StringComparison.OrdinalIgnoreCase)) return false;
  using var file = File.OpenRead(path);
  if (file.Length < 84) return false;
  file.Position = 80;
  using var reader = new BinaryReader(file);
  uint count = reader.ReadUInt32();
  return 84L + count * 50L == file.Length ? count > Threshold : file.Length > 75L * 1024 * 1024;
 }
 static IEnumerable<Point3D> BinaryVertices(BinaryReader reader, uint count) {
  reader.BaseStream.Position = 84;
  for (uint face = 0; face < count; face++) {
   reader.BaseStream.Position += 12;
   for (int vertex = 0; vertex < 3; vertex++) yield return new Point3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
   reader.BaseStream.Position += 2;
  }
 }
 static IEnumerable<Point3D> TextVertices(string path) {
  using var reader = new StreamReader(path);
  string line;
  while ((line = reader.ReadLine()) != null) {
   var fields = line.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
   if (fields.Length == 4 && fields[0].Equals("vertex", StringComparison.OrdinalIgnoreCase))
    yield return new Point3D(double.Parse(fields[1], CultureInfo.InvariantCulture), double.Parse(fields[2], CultureInfo.InvariantCulture), double.Parse(fields[3], CultureInfo.InvariantCulture));
  }
 }
 public static MeshGeometry3D Read(BinaryReader reader, uint count) => Reduce(() => BinaryVertices(reader, count));
 public static MeshGeometry3D ReadText(string path) => Reduce(() => TextVertices(path));
 public static MeshGeometry3D Reduce(Func<IEnumerable<Point3D>> scan, int cells = Cells) {
  var min = new Point3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
  var max = new Point3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);
  long vertexCount = 0;
  foreach (var p in scan()) {
   if (!double.IsFinite(p.X) || !double.IsFinite(p.Y) || !double.IsFinite(p.Z)) throw new InvalidDataException("Non-finite STL vertex");
   min.X = Math.Min(min.X, p.X); min.Y = Math.Min(min.Y, p.Y); min.Z = Math.Min(min.Z, p.Z);
   max.X = Math.Max(max.X, p.X); max.Y = Math.Max(max.Y, p.Y); max.Z = Math.Max(max.Z, p.Z);
   vertexCount++;
  }
  double cell = Math.Max(max.X - min.X, Math.Max(max.Y - min.Y, max.Z - min.Z)) / cells;
  if (!(cell > 0) || vertexCount % 3 != 0) throw new InvalidDataException("Invalid STL mesh");
  var points = new Point3DCollection(); var vertices = new Dictionary<long, int>();
  var retained = new List<(int A, int B, int C)>(); var faces = new HashSet<(int, int, int)>();
  var random = new Random(0); long candidates = 0;
  int Index(Point3D p) {
   long x = (long)Math.Round((p.X - min.X) / cell), y = (long)Math.Round((p.Y - min.Y) / cell), z = (long)Math.Round((p.Z - min.Z) / cell);
   long key = x | (y << 10) | (z << 20);
   if (!vertices.TryGetValue(key, out int index)) { index = points.Count; vertices.Add(key, index); points.Add(p); }
   return index;
  }
  using var iterator = scan().GetEnumerator();
  while (iterator.MoveNext()) {
   int a = Index(iterator.Current); iterator.MoveNext(); int b = Index(iterator.Current); iterator.MoveNext(); int c = Index(iterator.Current);
   if (a == b || a == c || b == c) continue;
   var key = a < b && a < c ? (a, b, c) : b < c ? (b, c, a) : (c, a, b);
   if (faces.Contains(key)) continue;
   candidates++;
   if (retained.Count < PreviewFaces) { faces.Add(key); retained.Add(key); }
   else {
    // For unusually irregular geometry, use bounded uniform reservoir storage
    // across the whole model rather than dropping every face after the limit.
    long slot = random.NextInt64(candidates);
    if (slot < PreviewFaces) { faces.Remove(retained[(int)slot]); retained[(int)slot] = key; faces.Add(key); }
   }
  }
  if (retained.Count == 0) throw new InvalidDataException("No preview triangles");
  var indices = new Int32Collection(retained.Count * 3);
  foreach (var face in retained) { indices.Add(face.A); indices.Add(face.B); indices.Add(face.C); }
  return new MeshGeometry3D { Positions = points, TriangleIndices = indices };
 }
}
