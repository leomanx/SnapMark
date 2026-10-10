using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;

namespace SnapMark.Editor;

// Effects process only source pixels; vector overlays and selection UI are never sampled.
public sealed class PixelEffects
{
    private readonly Dictionary<Annotation, BitmapSource> cache = [];
    private BitmapSource? source;
    private long cachedBytes;
    public static bool IsEffect(Annotation a) => a.Kind is AnnotationKind.Blur or AnnotationKind.Pixelate or AnnotationKind.Redact;
    public static PixelRect ClippedBounds(BitmapSource image, Annotation a)
    {
        var b = a.Bounds;
        var x = Math.Clamp((int)Math.Floor(b.X), 0, image.PixelWidth);
        var y = Math.Clamp((int)Math.Floor(b.Y), 0, image.PixelHeight);
        var right = Math.Clamp((int)Math.Ceiling(b.Right), x, image.PixelWidth);
        var bottom = Math.Clamp((int)Math.Ceiling(b.Bottom), y, image.PixelHeight);
        return new(x, y, right - x, bottom - y);
    }
    public BitmapSource Process(BitmapSource image, Annotation a)
    {
        if (!ReferenceEquals(source, image)) { cache.Clear(); cachedBytes = 0; source = image; }
        if (cache.TryGetValue(a, out var cached)) return cached;
        var b = ClippedBounds(image, a);
        var w = (int)b.Width; var h = (int)b.Height;
        if (w == 0 || h == 0) throw new ArgumentException("Effect region does not intersect the image.");
        var roi = new CroppedBitmap(image, new Int32Rect((int)b.X, (int)b.Y, w, h));
        var converted = new FormatConvertedBitmap(roi, PixelFormats.Pbgra32, null, 0);
        var pixels = new byte[checked(w * h * 4)]; converted.CopyPixels(pixels, w * 4, 0);
        var size = Math.Clamp((int)a.EffectSize, 2, 48);
        if (a.Kind == AnnotationKind.Redact)
        {
            Array.Clear(pixels);
            for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
        }
        else if (a.Kind == AnnotationKind.Pixelate) Mosaic(pixels, w, h, size);
        else if (a.Kind == AnnotationKind.Blur)
        {
            // Three separable box passes approximate a Gaussian at O(ROI pixels).
            var temp = new byte[pixels.Length];
            for (var pass = 0; pass < 3; pass++)
            { Box(pixels, temp, w, h, size, true); Box(temp, pixels, w, h, size, false); }
        }
        var result = BitmapSource.Create(w, h, 96, 96, PixelFormats.Pbgra32, null, pixels, w * 4);
        result.Freeze();
        if (cache.Count >= 16 || cachedBytes + pixels.Length > 32 * 1024 * 1024) { cache.Clear(); cachedBytes = 0; }
        if (pixels.Length <= 32 * 1024 * 1024) { cache[a] = result; cachedBytes += pixels.Length; }
        return result;
    }
    private static void Mosaic(byte[] pixels, int w, int h, int size)
    {
        for (var y = 0; y < h; y += size)
        for (var x = 0; x < w; x += size)
        {
            var right = Math.Min(w, x + size); var bottom = Math.Min(h, y + size);
            var count = (right - x) * (bottom - y); var totals = new int[4];
            for (var yy = y; yy < bottom; yy++) for (var xx = x; xx < right; xx++)
                for (var channel = 0; channel < 4; channel++) totals[channel] += pixels[(yy * w + xx) * 4 + channel];
            for (var yy = y; yy < bottom; yy++) for (var xx = x; xx < right; xx++)
                for (var channel = 0; channel < 4; channel++) pixels[(yy * w + xx) * 4 + channel] = (byte)(totals[channel] / count);
        }
    }
    private static void Box(byte[] input, byte[] output, int w, int h, int radius, bool horizontal)
    {
        var length = horizontal ? w : h; var lines = horizontal ? h : w; var span = radius * 2 + 1;
        int Index(int line, int pos, int channel) => (horizontal ? line * w + pos : pos * w + line) * 4 + channel;
        for (var line = 0; line < lines; line++) for (var channel = 0; channel < 4; channel++)
        {
            var sum = 0;
            for (var k = -radius; k <= radius; k++) sum += input[Index(line, Math.Clamp(k, 0, length - 1), channel)];
            for (var pos = 0; pos < length; pos++)
            {
                output[Index(line, pos, channel)] = (byte)(sum / span);
                sum -= input[Index(line, Math.Clamp(pos - radius, 0, length - 1), channel)];
                sum += input[Index(line, Math.Clamp(pos + radius + 1, 0, length - 1), channel)];
            }
        }
    }
}
