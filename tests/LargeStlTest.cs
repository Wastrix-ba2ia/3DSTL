using System;
using System.IO;
using System.Linq;

class LargeStlTest {
 static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
 static void Triangle(BinaryWriter writer, float offset) {
  for (int i = 0; i < 3; i++) writer.Write(0f);
  foreach (float n in new float[] { offset, 0, 0, offset + 1, 0, 0, offset, 1, 0 }) writer.Write(n);
  writer.Write((ushort)0);
 }
 [STAThread] static int Main() {
  string root = Path.Combine(Path.GetTempPath(), "3DSTL-large-test-" + Guid.NewGuid());
  Directory.CreateDirectory(root);
  try {
   string small = Path.Combine(root, "small.stl"), large = Path.Combine(root, "large.stl");
   using (var writer = new BinaryWriter(File.Create(small))) { writer.Write(new byte[80]); writer.Write(1u); Triangle(writer, 0); }
   const uint count = LargeStl.Threshold + 4;
   using (var writer = new BinaryWriter(File.Create(large))) {
    writer.Write(new byte[80]); writer.Write(count);
    using var record = new MemoryStream(); using var face = new BinaryWriter(record); Triangle(face, 0);
    var block = record.ToArray();
    for (uint i = 0; i < count - 1; i++) writer.Write(block);
    Triangle(writer, 100);
   }
   Assert(!LargeStl.IsLarge(small) && LargeStl.IsLarge(large), "Large-file classification failed");
   var ordered = ThumbnailRefresh.OrderFiles(new[] { large, small }).ToArray();
   Assert(ordered.SequenceEqual(new[] { small, large }), "Large STL must run after a small model even when enqueued first");
   string missing = Path.Combine(root, "missing.stl");
   Assert(ThumbnailRefresh.OrderFiles(new[] { small, missing }).Contains(missing), "Unavailable input must be reported as a failure, not silently omitted");
   using (var reader = new BinaryReader(File.OpenRead(large))) {
    var mesh = LargeStl.Read(reader, count);
    Assert(mesh.TriangleIndices.Count > 0 && mesh.TriangleIndices.Count <= 1200000, "Preview face budget exceeded");
    Assert(mesh.Positions.Max(p => p.X) >= 100, "Preview discarded geometry at the end of the input");
   }
   // Face 1 is not one of the evenly sampled faces. It must still be validated.
   using (var writer = new BinaryWriter(File.OpenWrite(large))) { writer.BaseStream.Position = 84 + 50 + 12; writer.Write(float.NaN); }
   bool rejected = false;
   try { using var reader = new BinaryReader(File.OpenRead(large)); LargeStl.Read(reader, count); }
   catch (InvalidDataException) { rejected = true; }
   Assert(rejected, "Unsampled invalid geometry escaped full-file validation");
   Console.WriteLine("PASS: normal-first queue, >1.5M-face preview, bounded geometry, end-of-file coverage, complete vertex validation");
   return 0;
  } finally { Directory.Delete(root, true); }
 }
}
