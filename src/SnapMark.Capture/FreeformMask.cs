using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;
namespace SnapMark.Capture;
public static class FreeformMask
{
    public static BitmapSource Apply(BitmapSource image, IReadOnlyList<PixelPoint> localPoints)
    {
        if (localPoints.Count is < 3 or > 1000) throw new ArgumentException("Freeform capture needs 3–1000 points.");
        var geometry = new StreamGeometry(); using (var path = geometry.Open()) { path.BeginFigure(new(localPoints[0].X, localPoints[0].Y), true, true); foreach (var point in localPoints.Skip(1)) path.LineTo(new(point.X, point.Y), true, false); }
        var visual = new DrawingVisual(); using (var dc = visual.RenderOpen()) { dc.PushClip(geometry); dc.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight)); dc.Pop(); }
        var output = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Pbgra32); output.Render(visual); output.Freeze(); return output;
    }
}
public sealed record RegionSelection(PixelRect Bounds, IReadOnlyList<PixelPoint>? Polygon = null);
