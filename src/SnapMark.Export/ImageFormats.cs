using System.IO;
using System.Text;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SnapMark.Export;
public static class ImageFormats
{
    public static void Save(BitmapSource source, string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension == ".pdf") { SavePdf(source, path); return; }
        BitmapEncoder encoder = extension switch { ".png" => new PngBitmapEncoder(), ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 }, ".bmp" => new BmpBitmapEncoder(), ".tif" or ".tiff" => new TiffBitmapEncoder(), _ => throw new ArgumentException("Export formats: PNG, JPEG, BMP, TIFF, PDF.") };
        if (encoder is JpegBitmapEncoder) source = WhiteBackground(source);
        encoder.Frames.Add(BitmapFrame.Create(source)); Atomic(path, stream => encoder.Save(stream));
    }
    private static BitmapSource WhiteBackground(BitmapSource source)
    {
        var visual = new DrawingVisual(); using (var dc = visual.RenderOpen()) { dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, source.PixelWidth, source.PixelHeight)); dc.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight)); }
        var bitmap = new RenderTargetBitmap(source.PixelWidth, source.PixelHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); bitmap.Freeze(); return bitmap;
    }
    private static void Atomic(string path, Action<Stream> write)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!); var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { using (var stream = File.Create(temp)) write(stream); File.Move(temp, path, true); } finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public static void SavePdf(BitmapSource source, string path)
    {
        var jpeg = new JpegBitmapEncoder { QualityLevel = 95 }; jpeg.Frames.Add(BitmapFrame.Create(WhiteBackground(source))); using var data = new MemoryStream(); jpeg.Save(data); var bytes = data.ToArray();
        var w = (source.PixelWidth * .75).ToString("0.###", CultureInfo.InvariantCulture); var h = (source.PixelHeight * .75).ToString("0.###", CultureInfo.InvariantCulture);
        Atomic(path, stream =>
        {
            void Text(string s) => stream.Write(Encoding.ASCII.GetBytes(s)); var offsets = new List<long> { 0 };
            void Object(int number, string text) { offsets.Add(stream.Position); Text($"{number} 0 obj\n{text}\nendobj\n"); }
            Text("%PDF-1.4\n"); Object(1, "<< /Type /Catalog /Pages 2 0 R >>"); Object(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
            Object(3, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {w} {h}] /Resources << /XObject << /Im0 5 0 R >> >> /Contents 4 0 R >>");
            var commands = $"q\n{w} 0 0 {h} 0 0 cm\n/Im0 Do\nQ\n"; Object(4, $"<< /Length {Encoding.ASCII.GetByteCount(commands)} >>\nstream\n{commands}endstream");
            offsets.Add(stream.Position); Text($"5 0 obj\n<< /Type /XObject /Subtype /Image /Width {source.PixelWidth} /Height {source.PixelHeight} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {bytes.Length} >>\nstream\n"); stream.Write(bytes); Text("\nendstream\nendobj\n");
            var xref = stream.Position; Text("xref\n0 6\n0000000000 65535 f \n"); foreach (var offset in offsets.Skip(1)) Text($"{offset:0000000000} 00000 n \n"); Text($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        });
    }
}
