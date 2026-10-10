using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;

namespace SnapMark.Editor;

public interface IAnnotationRenderer { BitmapSource Render(BitmapSource image, EditorDocument document); }
public sealed class AnnotationRenderer : IAnnotationRenderer
{
    private readonly PixelEffects effects = new();
    private BitmapSource? compositeSource, composite;
    private Annotation[] compositeImages = [];
    private int compositeWidth, compositeHeight;
    private readonly Dictionary<Guid, BitmapSource> imageCache = [];
    public static PixelRect GetBounds(Annotation a)
    {
        if (a.Kind is AnnotationKind.Number or AnnotationKind.OpenDot or AnnotationKind.FilledDot) return new(a.Start.X - a.FontSize * .8, a.Start.Y - a.FontSize * .8, a.FontSize * 1.6, a.FontSize * 1.6);
        if (a.Kind is not (AnnotationKind.Text or AnnotationKind.Stamp or AnnotationKind.Note)) return a.Bounds;
        var text = Text(a.Text, a.FontSize, Brushes.Black, a.Kind == AnnotationKind.Stamp || a.Bold, a.FontFamily, a.TextAlign, a.Italic, a.Underline, a.TextWidth);
        var padding = a.Kind is AnnotationKind.Stamp or AnnotationKind.Note ? 10 : a.TextBackground ? Math.Clamp(a.TextPadding, 0, 64) : 0;
        return new(a.Start.X, a.Start.Y, text.MaxTextWidth + padding * 2, text.Height + padding * 2);
    }
    public static MatrixTransform ObjectTransform(Annotation a)
    {
        var b = GetBounds(a); var x = b.X + b.Width / 2; var y = b.Y + b.Height / 2;
        var matrix = Matrix.Identity; matrix.ScaleAt(a.MirrorHorizontal ? -1 : 1, a.MirrorVertical ? -1 : 1, x, y); matrix.RotateAt(a.Rotation, x, y);
        return new MatrixTransform(matrix);
    }
    public void DrawDocument(DrawingContext dc, BitmapSource image, IEnumerable<Annotation> annotations, EditorDocument? document = null)
    {
        var all = annotations.ToArray();
        var images = all.Where(a => a.Kind == AnnotationKind.Image).ToArray();
        var width = document?.Width ?? image.PixelWidth; var height = document?.Height ?? image.PixelHeight;
        if (document is not null && (images.Length > 0 || width != image.PixelWidth || height != image.PixelHeight))
        {
            if (!ReferenceEquals(compositeSource, image) || !images.SequenceEqual(compositeImages) || compositeWidth != width || compositeHeight != height)
            {
                if (!ReferenceEquals(compositeSource, image)) imageCache.Clear();
                var visual = new DrawingVisual();
                using (var layer = visual.RenderOpen())
                {
                    layer.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));
                    layer.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
                    foreach (var a in images)
                    {
                        if (a.AssetId is not { } id || !document.Assets.TryGetValue(id, out var asset)) continue;
                        if (!imageCache.TryGetValue(id, out var bitmap))
                        {
                            using var stream = new System.IO.MemoryStream(asset.Png);
                            bitmap = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0]; bitmap.Freeze(); imageCache[id] = bitmap;
                        }
                        var b = a.Bounds;
                        layer.PushOpacity(Math.Clamp(a.Opacity, 0, 1)); layer.PushTransform(ObjectTransform(a));
                        layer.DrawImage(bitmap, new Rect(b.X, b.Y, b.Width, b.Height)); layer.Pop(); layer.Pop();
                    }
                }
                var merged = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); merged.Render(visual); merged.Freeze();
                composite = merged; compositeSource = image; compositeImages = images; compositeWidth = width; compositeHeight = height;
            }
            image = composite!;
        }
        dc.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
        // Opaque covers always remain above blur/mosaic, regardless of creation order.
        foreach (var a in all.Where(PixelEffects.IsEffect).OrderBy(a => a.Kind == AnnotationKind.Redact ? 1 : 0))
        {
            var b = PixelEffects.ClippedBounds(image, a);
            if (b.Width < 1 || b.Height < 1) continue;
            var rect = new Rect(b.X, b.Y, b.Width, b.Height);
            if (a.Kind == AnnotationKind.Redact) dc.DrawRectangle(Brushes.Black, null, rect);
            else dc.DrawImage(effects.Process(image, a), rect);
        }
        foreach (var a in all.Where(a => !PixelEffects.IsEffect(a) && a.Kind != AnnotationKind.Image)) Draw(dc, a, document);
    }
    public static void Draw(DrawingContext dc, Annotation a, EditorDocument? document = null)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.Color));
        var textBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.TextColor ?? a.Color));
        var stroke = new SolidColorBrush(brush.Color) { Opacity = Math.Clamp(a.StrokeOpacity, 0, 1) };
        var pen = new Pen(stroke, a.Thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        pen.DashStyle = a.LinePattern switch { LinePattern.Dash => DashStyles.Dash, LinePattern.Dot => DashStyles.Dot, LinePattern.DashDot => DashStyles.DashDot, LinePattern.LongDash => new DashStyle(new double[] { 8, 3 }, 0), _ => DashStyles.Solid };
        var start = new Point(a.Start.X, a.Start.Y); var end = new Point(a.End.X, a.End.Y);
        var b = a.Bounds; var rect = new Rect(b.X, b.Y, b.Width, b.Height);
        var fillColor = (Color)ColorConverter.ConvertFromString(a.FillColor ?? a.Color);
        var fillBrush = new SolidColorBrush(fillColor) { Opacity = Math.Clamp(a.FillOpacity ?? (a.Kind == AnnotationKind.Highlight ? .35 : 1), 0, 1) };
        Brush? fill = a.Fill switch { ShapeFill.Solid => fillBrush, ShapeFill.Gradient => new LinearGradientBrush(fillColor, (Color)ColorConverter.ConvertFromString(a.SecondColor), 45) { Opacity = fillBrush.Opacity }, _ => null };
                dc.PushOpacity(Math.Clamp(a.Opacity, 0, 1));
        dc.PushTransform(ObjectTransform(a));
        switch (a.Kind)
        {
            case AnnotationKind.Arrow:
                dc.DrawLine(pen, start, end);
                var vector = start - end;
                if (vector.Length < 1) break;
                vector.Normalize(); var side = new Vector(-vector.Y, vector.X); var size = Math.Max(12, a.Thickness * 4);
                var geometry = new StreamGeometry();
                using (var g = geometry.Open()) { g.BeginFigure(end, true, true); g.LineTo(end + vector * size + side * size * .45, true, false); g.LineTo(end + vector * size - side * size * .45, true, false); }
                dc.DrawGeometry(stroke, null, geometry); break;
            case AnnotationKind.Line: dc.DrawLine(pen, start, end); Marker(dc, start, end, a.StartMarker, stroke, a.Thickness); Marker(dc, end, start, a.EndMarker, stroke, a.Thickness); break;
            case AnnotationKind.Rectangle: dc.DrawRectangle(fill, pen, rect); break;
            case AnnotationKind.Plane: dc.DrawRectangle(fill ?? fillBrush, null, rect); break;
            case AnnotationKind.RoundedRectangle: dc.DrawRoundedRectangle(fill, pen, rect, 12, 12); break;
            case AnnotationKind.Circle:
            case AnnotationKind.Ellipse: dc.DrawEllipse(fill, pen, new Point(b.X + b.Width / 2, b.Y + b.Height / 2), b.Width / 2, b.Height / 2); break;
            case AnnotationKind.Triangle: DrawPath(dc, new[] { new PixelPoint(b.X + b.Width / 2, b.Y), new(b.Right, b.Bottom), new(b.X, b.Bottom) }, true, fill, pen); break;
            case AnnotationKind.Polygon:
            case AnnotationKind.Polyline:
            case AnnotationKind.Freehand:
                if (a.Points is not null) DrawPath(dc, a.Points, a.Kind == AnnotationKind.Polygon || a.Closed, fill, pen); break;
            case AnnotationKind.OpenDot: dc.DrawEllipse(null, pen, start, a.FontSize * .8, a.FontSize * .8); break;
            case AnnotationKind.FilledDot: dc.DrawEllipse(fillBrush, null, start, a.FontSize * .8, a.FontSize * .8); break;
            case AnnotationKind.RevisionCloud:
                Cloud(dc, a.Points ?? new[] { new PixelPoint(b.X, b.Y), new(b.Right, b.Y), new(b.Right, b.Bottom), new(b.X, b.Bottom) }, pen); break;
            case AnnotationKind.Dimension:
            case AnnotationKind.Distance:
                dc.DrawLine(pen, start, end); Marker(dc, start, end, EndpointMarker.Arrow, stroke, a.Thickness); Marker(dc, end, start, EndpointMarker.Arrow, stroke, a.Thickness);
                var dimension = MeasurementLabel(a, document);
                DrawLabel(dc, dimension, new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2 - a.FontSize / 2 - 8), a, brush); break;
            case AnnotationKind.Area:
            case AnnotationKind.Angle:
            case AnnotationKind.Length:
                if (a.Points is not { Count: >= 2 } points || (a.Kind != AnnotationKind.Length && points.Count < 3)) break;
                DrawPath(dc, points, a.Kind == AnnotationKind.Area || a.Closed, fill, pen);
                var value = MeasurementLabel(a, document);
                DrawLabel(dc, value, new Point(b.X + b.Width / 2, b.Y - a.FontSize / 2 - 8), a, brush); break;
            case AnnotationKind.Callout:
                dc.DrawLine(pen, start, end); dc.DrawEllipse(fillBrush, pen, start, a.FontSize, a.FontSize);
                var callout = Text(a.Text, a.FontSize, a.TextColor is null ? Brushes.White : textBrush, a.Bold, a.FontFamily, a.TextWidth > 0 ? a.TextAlign : TextAlign.Center, a.Italic, a.Underline, a.TextWidth > 0 ? a.TextWidth : a.FontSize * 2); dc.DrawText(callout, new Point(start.X - callout.MaxTextWidth / 2, start.Y - callout.Height / 2)); break;
            case AnnotationKind.Symbol: DrawSymbol(dc, a, pen, brush, rect); break;
            case AnnotationKind.Highlight:
                dc.DrawRectangle(fillBrush, null, rect); break;
            case AnnotationKind.Number:
                dc.DrawEllipse(fillBrush, pen, start, a.FontSize * .8, a.FontSize * .8);
                var label = Text(a.Text, a.FontSize, a.TextColor is null ? Brushes.White : textBrush, a.Bold, a.FontFamily, a.TextWidth > 0 ? a.TextAlign : TextAlign.Center, a.Italic, a.Underline, a.TextWidth > 0 ? a.TextWidth : a.FontSize * 1.6);
                dc.DrawText(label, new Point(start.X - label.MaxTextWidth / 2, start.Y - label.Height / 2)); break;
            case AnnotationKind.Text:
                var textPadding = a.TextBackground ? Math.Clamp(a.TextPadding, 0, 64) : 0;
                if (a.TextBackground)
                {
                    var textBounds = GetBounds(a);
                    var backgroundBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.TextBackgroundColor)) { Opacity = Math.Clamp(a.TextBackgroundOpacity, 0, 1) };
                    dc.DrawRoundedRectangle(backgroundBrush, null, new Rect(textBounds.X, textBounds.Y, textBounds.Width, textBounds.Height), 3, 3);
                }
                dc.DrawText(Text(a.Text, a.FontSize, textBrush, a.Bold, a.FontFamily, a.TextAlign, a.Italic, a.Underline, a.TextWidth), new Point(start.X + textPadding, start.Y + textPadding)); break;
            case AnnotationKind.Note:
            case AnnotationKind.Stamp:
                var bounds = GetBounds(a);
                dc.DrawRoundedRectangle(new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.FillColor ?? "#FFFFFF")) { Opacity = fillBrush.Opacity }, pen, new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height), 3, 3);
                dc.DrawText(Text(a.Text, a.FontSize, textBrush, a.Bold || a.Kind == AnnotationKind.Stamp, a.FontFamily, a.TextAlign, a.Italic, a.Underline, a.TextWidth), new Point(start.X + 10, start.Y + 10)); break;
        }
        dc.Pop(); dc.Pop();
    }
    private static void DrawLabel(DrawingContext dc, string label, Point position, Annotation a, Brush brush)
    {
        var text = Text(label, a.FontSize, a.TextColor is null ? brush : new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.TextColor)), a.Bold, a.FontFamily, a.TextAlign, a.Italic, a.Underline, a.TextWidth);
        var origin = new Point(position.X - text.MaxTextWidth / 2, position.Y - text.Height / 2);
        if (a.TextBackground)
        {
            var padding = Math.Clamp(a.TextPadding, 0, 64);
            var background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.TextBackgroundColor)) { Opacity = Math.Clamp(a.TextBackgroundOpacity, 0, 1) };
            dc.DrawRoundedRectangle(background, null, new Rect(origin.X - padding, origin.Y - padding, text.MaxTextWidth + padding * 2, text.Height + padding * 2), 3, 3);
        }
        dc.DrawText(text, origin);
    }
    public static string MeasurementLabel(Annotation a, EditorDocument? document)
    {
        var scale = document?.UnitsPerPixel ?? 1; var unit = a.MeasureShowUnit ? " " + (document?.Unit ?? "px") : "";
        string Number(double v) => v.ToString("F" + Math.Clamp(a.MeasureDecimals, 0, 6), CultureInfo.CurrentCulture);
        var points = a.Points ?? [];
        var value = a.Kind switch
        {
            AnnotationKind.Dimension when a.Text.Length > 0 => a.Text,
            AnnotationKind.Area when points.Count >= 3 => $"A {Number(GeometryMath.Area(points) * scale * scale)}{unit}{(a.MeasureShowUnit ? "²" : "")} · P {Number(GeometryMath.PathLength(points, true) * scale)}{unit}",
            AnnotationKind.Length when points.Count >= 2 => $"{(a.Closed ? "P " : "L ")}{Number(GeometryMath.PathLength(points, a.Closed) * scale)}{unit}",
            AnnotationKind.Angle when points.Count >= 3 => Number(GeometryMath.Angle(points[0], points[1], points[2])) + (a.MeasureShowUnit ? "°" : ""),
            _ => Number(GeometryMath.Distance(a.Start, a.End) * scale) + unit
        };
        return a.MeasurePrefix + value + a.MeasureSuffix;
    }
    private static void DrawPath(DrawingContext dc, IReadOnlyList<PixelPoint> points, bool closed, Brush? fill, Pen pen)
    {
        if (points.Count < 2) return;
        var geometry = new StreamGeometry(); using (var g = geometry.Open()) { g.BeginFigure(new Point(points[0].X, points[0].Y), fill is not null, closed); foreach (var p in points.Skip(1)) g.LineTo(new Point(p.X, p.Y), true, false); }
        dc.DrawGeometry(fill, pen, geometry);
    }
    private static void Marker(DrawingContext dc, Point tip, Point other, EndpointMarker marker, Brush brush, double weight)
    {
        var radius = Math.Max(4, weight * 2);
        if (marker is EndpointMarker.FilledDot or EndpointMarker.OpenDot) dc.DrawEllipse(marker == EndpointMarker.FilledDot ? brush : Brushes.White, new Pen(brush, weight), tip, radius, radius);
        if (marker != EndpointMarker.Arrow) return;
        var v = other - tip; if (v.Length < 1) return; v.Normalize(); var side = new Vector(-v.Y, v.X); var size = Math.Max(10, weight * 4);
        DrawPath(dc, new[] { new PixelPoint(tip.X, tip.Y), new((tip + v * size + side * size * .4).X, (tip + v * size + side * size * .4).Y), new((tip + v * size - side * size * .4).X, (tip + v * size - side * size * .4).Y) }, true, brush, new Pen(brush, 1));
    }
    private static void Cloud(DrawingContext dc, IReadOnlyList<PixelPoint> points, Pen pen)
    {
        if (points.Count < 3) return;
        var geometry = new StreamGeometry(); using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(points[0].X, points[0].Y), false, true);
            for (var i = 0; i < points.Count; i++)
            {
                var a = points[i]; var b = points[(i + 1) % points.Count]; var length = GeometryMath.Distance(a, b); var count = Math.Max(1, (int)(length / 16));
                for (var j = 1; j <= count; j++)
                {
                    var end = new Point(a.X + (b.X - a.X) * j / count, a.Y + (b.Y - a.Y) * j / count);
                    var mid = new Point(a.X + (b.X - a.X) * (j - .5) / count + (b.Y - a.Y) / Math.Max(1, length) * 7,
                        a.Y + (b.Y - a.Y) * (j - .5) / count - (b.X - a.X) / Math.Max(1, length) * 7);
                    g.QuadraticBezierTo(mid, end, true, false);
                }
            }
        }
        dc.DrawGeometry(null, pen, geometry);
    }
    private static void DrawSymbol(DrawingContext dc, Annotation a, Pen pen, Brush brush, Rect r)
    {
        var symbolFill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.FillColor ?? "#FFFFFF")) { Opacity = Math.Clamp(a.FillOpacity ?? 1, 0, 1) };
        var center = new Point(r.X + r.Width / 2, r.Y + r.Height / 2); var size = Math.Min(r.Width, r.Height) / 2;
        switch (a.Variant)
        {
            case "north":
            case "slope": dc.DrawLine(pen, new Point(center.X, r.Bottom), new Point(center.X, r.Top)); Marker(dc, new Point(center.X, r.Top), new Point(center.X, r.Bottom), EndpointMarker.Arrow, pen.Brush, a.Thickness); break;
            case "centerline":
            case "axis": dc.DrawLine(pen, new Point(r.Left, center.Y), new Point(r.Right, center.Y)); dc.DrawLine(pen, new Point(center.X, r.Top), new Point(center.X, r.Bottom)); break;
            case "break": DrawPath(dc, new[] { new PixelPoint(r.Left, center.Y), new(center.X - 10, center.Y), new(center.X, r.Top), new(center.X + 10, r.Bottom), new(center.X + 20, center.Y), new(r.Right, center.Y) }, false, null, pen); break;
            case "section":
            case "elevation": dc.DrawEllipse(symbolFill, pen, center, size, size); dc.DrawLine(pen, new Point(r.Left, center.Y), new Point(r.Right, center.Y)); Marker(dc, new Point(r.Right, center.Y), center, EndpointMarker.Arrow, pen.Brush, a.Thickness); break;
            case "level": DrawPath(dc, new[] { new PixelPoint(r.Left, r.Top), new(r.Right, r.Top), new(center.X, center.Y) }, true, a.FillColor is null ? new SolidColorBrush(((SolidColorBrush)brush).Color) { Opacity = symbolFill.Opacity } : symbolFill, pen); dc.DrawLine(pen, center, new Point(r.Right, center.Y)); break;
            case "warning": DrawPath(dc, new[] { new PixelPoint(center.X, r.Top), new(r.Right, r.Bottom), new(r.Left, r.Bottom) }, true, symbolFill, pen); break;
            case "check": DrawPath(dc, new[] { new PixelPoint(r.Left, center.Y), new(center.X, r.Bottom), new(r.Right, r.Top) }, false, null, pen); break;
            case "cross": dc.DrawLine(pen, r.TopLeft, r.BottomRight); dc.DrawLine(pen, r.BottomLeft, r.TopRight); break;
            default: dc.DrawEllipse(symbolFill, pen, center, size, size); break;
        }
        var label = Text(a.Text.Length > 0 ? a.Text : a.Variant == "north" ? "N" : a.Variant == "warning" ? "!" : "", a.FontSize, a.TextColor is null ? brush : new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.TextColor)), a.Bold, a.FontFamily, a.TextWidth > 0 ? a.TextAlign : TextAlign.Center, a.Italic, a.Underline, a.TextWidth > 0 ? a.TextWidth : r.Width);
        dc.DrawText(label, new Point(center.X - label.MaxTextWidth / 2, center.Y - label.Height / 2));
    }
    private static FormattedText Text(string value, double size, Brush color, bool bold = false, string family = "Segoe UI", TextAlign alignment = TextAlign.Left, bool italic = false, bool underline = false, double width = 0)
    {
        var text = new FormattedText(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(new FontFamily(family), italic ? FontStyles.Italic : FontStyles.Normal, bold ? FontWeights.SemiBold : FontWeights.Normal, FontStretches.Normal), size, color, 1);
        if (underline) text.SetTextDecorations(TextDecorations.Underline);
        text.MaxTextWidth = Math.Max(1, Math.Max(width, text.WidthIncludingTrailingWhitespace));
        text.TextAlignment = alignment switch { TextAlign.Center => System.Windows.TextAlignment.Center, TextAlign.Right => System.Windows.TextAlignment.Right, _ => System.Windows.TextAlignment.Left }; return text;
    }
    public BitmapSource Render(BitmapSource image, EditorDocument document)
    {
        var crop = document.Crop ?? new PixelRect(0, 0, document.Width, document.Height);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, crop.Width, crop.Height)));
            dc.PushTransform(new TranslateTransform(-crop.X, -crop.Y));
            DrawDocument(dc, image, document.Annotations, document);
            dc.Pop(); dc.Pop();
        }
        var bitmap = new RenderTargetBitmap((int)crop.Width, (int)crop.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual); bitmap.Freeze(); return bitmap;
    }
}
