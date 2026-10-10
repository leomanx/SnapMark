using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SnapMark.Editor;
public sealed record ComparisonResult(BitmapSource Difference, long ChangedPixels, long TotalPixels)
{
    public double ChangedPercent => 100d * ChangedPixels / TotalPixels;
}
public static class ImageComparison
{
    public static ComparisonResult Compare(BitmapSource before, BitmapSource after, int threshold = 16)
    {
        if (before.PixelWidth != after.PixelWidth || before.PixelHeight != after.PixelHeight) throw new ArgumentException("Comparison needs equal image dimensions. Crop/resize the reference first; images are not stretched automatically.");
        if ((long)before.PixelWidth * before.PixelHeight > 32_000_000) throw new ArgumentException("Comparison limit: 32 megapixels.");
        threshold = Math.Clamp(threshold, 0, 255); var stride = before.PixelWidth * 4;
        byte[] Pixels(BitmapSource source) { var converted = new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0); var bytes = new byte[stride * source.PixelHeight]; converted.CopyPixels(bytes, stride, 0); return bytes; }
        var a = Pixels(before); var b = Pixels(after); var output = new byte[a.Length]; long count = 0;
        for (var i = 0; i < a.Length; i += 4)
        {
            var delta = Math.Max(Math.Abs(a[i] - b[i]), Math.Max(Math.Abs(a[i + 1] - b[i + 1]), Math.Max(Math.Abs(a[i + 2] - b[i + 2]), Math.Abs(a[i + 3] - b[i + 3]))));
            if (delta > threshold) { output[i] = 40; output[i + 1] = 30; output[i + 2] = 255; count++; }
            else { var gray = (byte)((a[i] + a[i + 1] + a[i + 2]) / 6); output[i] = output[i + 1] = output[i + 2] = gray; }
            output[i + 3] = 255;
        }
        var diff = BitmapSource.Create(before.PixelWidth, before.PixelHeight, 96, 96, PixelFormats.Pbgra32, null, output, stride); diff.Freeze(); return new(diff, count, before.PixelWidth * (long)before.PixelHeight);
    }
}
