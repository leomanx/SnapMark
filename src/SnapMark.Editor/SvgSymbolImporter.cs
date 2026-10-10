using System.IO;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SnapMark.Editor;
public static class SvgSymbolImporter
{
    public static BitmapSource Load(string path, string currentColor = "#EF4444")
    {
        if (new FileInfo(path).Length > 1_000_000) throw new InvalidDataException("SVG limit: 1 MB.");
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1_000_000 });
        var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Empty SVG.");
        if (root.Name.LocalName != "svg") throw new InvalidDataException("Expected an SVG root.");
        var allowed = new HashSet<string> { "svg", "g", "path", "rect", "circle", "ellipse", "line", "polyline", "polygon", "title", "desc" };
        if (root.DescendantsAndSelf().Count() > 1000 || root.DescendantsAndSelf().Any(e => !allowed.Contains(e.Name.LocalName) || e.Attributes().Any(a => a.Name.LocalName is "href" or "filter" or "clip-path" or "mask" or "class" || a.Name.LocalName.StartsWith("on")))) throw new InvalidDataException("Supported SVG: paths and basic shapes/groups. No external references, scripts, CSS classes, filters, clipping or masks.");
        var view = Numbers(root.Attribute("viewBox")?.Value ?? $"0 0 {root.Attribute("width")?.Value ?? "512"} {root.Attribute("height")?.Value ?? "512"}");
        if (view.Length != 4 || view[2] <= 0 || view[3] <= 0 || view.Any(x => !double.IsFinite(x)) || view[2] > 100000 || view[3] > 100000) throw new InvalidDataException("Invalid SVG viewBox.");
        var scale = 512 / Math.Max(view[2], view[3]); var width = Math.Max(1, (int)Math.Ceiling(view[2] * scale)); var height = Math.Max(1, (int)Math.Ceiling(view[3] * scale));
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(scale, scale)); dc.PushTransform(new TranslateTransform(-view[0], -view[1]));
            Draw(root, dc, Brushes.Black, null, 1, currentColor); dc.Pop(); dc.Pop();
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); bitmap.Freeze(); return bitmap;
    }
    private static double[] Numbers(string text) => text.Replace(',', ' ').Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
    private static void Draw(XElement element, DrawingContext dc, Brush? inheritedFill, Brush? inheritedStroke, double inheritedWeight, string currentColor)
    {
        string? Attr(string name)
        {
            var direct = element.Attribute(name)?.Value; if (direct is not null) return direct;
            return element.Attribute("style")?.Value.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split(':', 2)).FirstOrDefault(pair => pair.Length == 2 && pair[0].Trim() == name)?[1].Trim();
        }
        Brush? BrushValue(string? value, Brush? fallback) => value is null ? fallback : value == "none" ? null : new SolidColorBrush((Color)ColorConverter.ConvertFromString(value == "currentColor" ? currentColor : value)!);
        double Number(string name, double fallback = 0) => Attr(name) is { } s ? double.Parse(s, CultureInfo.InvariantCulture) : fallback;
        if (element.Attribute("transform") is not null) throw new InvalidDataException("SVG transform attributes are not supported yet; flatten transforms before import.");
        if (element.Attribute("style") is { } style && style.Value.Split(';', StringSplitOptions.RemoveEmptyEntries).Any(s => s.Split(':', 2)[0].Trim() is not ("fill" or "stroke" or "stroke-width" or "opacity" or "fill-opacity" or "stroke-opacity"))) throw new InvalidDataException("Unsupported SVG style property.");
        var fill = BrushValue(Attr("fill"), inheritedFill); var stroke = BrushValue(Attr("stroke"), inheritedStroke); var weight = Number("stroke-width", inheritedWeight);
        if (fill is not null) { fill = fill.Clone(); fill.Opacity = Math.Clamp(Number("fill-opacity", inheritedFill?.Opacity ?? 1), 0, 1); }
        if (stroke is not null) { stroke = stroke.Clone(); stroke.Opacity = Math.Clamp(Number("stroke-opacity", inheritedStroke?.Opacity ?? 1), 0, 1); }
        var pen = stroke is null ? null : new Pen(stroke, Math.Clamp(weight, 0, 10000)); dc.PushOpacity(Math.Clamp(Number("opacity", 1), 0, 1));
        switch (element.Name.LocalName)
        {
            case "svg": case "g": foreach (var child in element.Elements()) Draw(child, dc, fill, stroke, weight, currentColor); break;
            case "path": dc.DrawGeometry(fill, pen, Geometry.Parse(Attr("d") ?? throw new InvalidDataException("Path missing d."))); break;
            case "rect": dc.DrawRoundedRectangle(fill, pen, new Rect(Number("x"), Number("y"), Number("width"), Number("height")), Number("rx"), Number("ry", Number("rx"))); break;
            case "circle": dc.DrawEllipse(fill, pen, new Point(Number("cx"), Number("cy")), Number("r"), Number("r")); break;
            case "ellipse": dc.DrawEllipse(fill, pen, new Point(Number("cx"), Number("cy")), Number("rx"), Number("ry")); break;
            case "line": dc.DrawLine(pen ?? new Pen(Brushes.Transparent, 1), new Point(Number("x1"), Number("y1")), new Point(Number("x2"), Number("y2"))); break;
            case "polyline": case "polygon":
                var values = Numbers(Attr("points") ?? ""); if (values.Length < 4 || values.Length % 2 != 0) throw new InvalidDataException("Invalid SVG points.");
                var geometry = new StreamGeometry(); using (var context = geometry.Open()) { context.BeginFigure(new(values[0], values[1]), element.Name.LocalName == "polygon", element.Name.LocalName == "polygon"); for (var i = 2; i < values.Length; i += 2) context.LineTo(new(values[i], values[i + 1]), true, false); } dc.DrawGeometry(element.Name.LocalName == "polygon" ? fill : null, pen, geometry); break;
        }
        dc.Pop();
    }
}
