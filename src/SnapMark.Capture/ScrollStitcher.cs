using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SnapMark.Capture;

public sealed class ScrollStitcher
{
    private readonly List<(BitmapSource Image, int Skip)> frames = [];
    public int Count => frames.Count;
    public int Height { get; private set; }
    public void Add(BitmapSource image)
    {
        if (image.PixelHeight < 32 || image.PixelWidth > 16000) throw new ArgumentException("Scrolling region must be at least 32 pixels tall and no wider than 16000 pixels.");
        if (frames.Count >= 50) throw new ArgumentException("Maximum 50 scrolling frames.");
        var skip = frames.Count == 0 ? 0 : FindOverlap(frames[^1].Image, image);
        if (skip == image.PixelHeight) return;
        if (frames.Sum(f => (long)f.Image.PixelWidth * f.Image.PixelHeight * 4) + (long)image.PixelWidth * image.PixelHeight * 4 > 256_000_000) throw new ArgumentException("Scrolling frame memory limit: 256 MB. Finish this capture before starting another.");
        if (Height + image.PixelHeight - skip > 16000 || (long)(Height + image.PixelHeight - skip) * image.PixelWidth > 64_000_000) throw new ArgumentException("Stitched image limit: 16000px / 64 megapixels.");
        frames.Add((image, skip)); Height += image.PixelHeight - skip;
    }
    public void Undo() { if (frames.Count == 0) return; var last = frames[^1]; Height -= last.Image.PixelHeight - last.Skip; frames.RemoveAt(frames.Count - 1); }
    public BitmapSource Render()
    {
        if (frames.Count == 0) throw new InvalidOperationException("No scrolling frames.");
        var visual = new DrawingVisual(); using (var dc = visual.RenderOpen()) { int y = 0; foreach (var (image, skip) in frames) { var crop = new CroppedBitmap(image, new Int32Rect(0, skip, image.PixelWidth, image.PixelHeight - skip)); dc.DrawImage(crop, new Rect(0, y, crop.PixelWidth, crop.PixelHeight)); y += crop.PixelHeight; } }
        var result = new RenderTargetBitmap(frames[0].Image.PixelWidth, Height, 96, 96, PixelFormats.Pbgra32); result.Render(visual); result.Freeze(); return result;
    }
    private static int FindOverlap(BitmapSource previous, BitmapSource next)
    {
        if (previous.PixelWidth != next.PixelWidth || previous.PixelHeight != next.PixelHeight) throw new ArgumentException("Scrolling frames must have the same size.");
        var width = previous.PixelWidth; var height = previous.PixelHeight; var stride = width * 4;
        byte[] Pixels(BitmapSource source) { var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0); var pixels = new byte[stride * height]; converted.CopyPixels(pixels, stride, 0); return pixels; }
        var a = Pixels(previous); var b = Pixels(next);
        double Cost(int overlap)
        {
            long error = 0; int samples = 0;
            for (int row = 0; row < overlap; row += Math.Max(1, overlap / 64))
                for (int col = 0; col < width; col += Math.Max(1, width / 48))
                    for (int channel = 0; channel < 3; channel++) { error += Math.Abs(a[((height - overlap + row) * width + col) * 4 + channel] - b[(row * width + col) * 4 + channel]); samples++; }
            return error / (double)samples;
        }
        if (Cost(height) < .05) return height;
        int best = 0; double bestCost = double.MaxValue;
        for (int overlap = height - 1; overlap >= Math.Max(16, height / 8); overlap--) { var cost = Cost(overlap); if (cost < bestCost) { best = overlap; bestCost = cost; } }
        if (bestCost > 3) throw new ArgumentException("No reliable overlap. Scroll down less, exclude fixed headers/footers, then capture again.");
        return best;
    }
}
