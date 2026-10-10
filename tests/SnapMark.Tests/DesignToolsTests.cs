using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;
using SnapMark.Editor;
using SnapMark.Export;

internal static class DesignToolsTests
{
    public static void WriteDemo(string path)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, 900, 520));
            var pen = new Pen(Brushes.SlateGray, 5);
            dc.DrawRectangle(null, pen, new Rect(70, 100, 740, 320));
            dc.DrawLine(pen, new Point(400, 100), new Point(400, 420));
            dc.DrawLine(pen, new Point(400, 275), new Point(810, 275));
            var title = new FormattedText("DESIGN REVIEW · LEVEL 01", System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Segoe UI"), 24, Brushes.SlateGray, 1);
            dc.DrawText(title, new Point(70, 35));
            var info = new FormattedText("Example contact: design@example.test", System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Segoe UI"), 18, Brushes.SlateGray, 1);
            dc.DrawText(info, new Point(70, 460));
        }
        var image = new RenderTargetBitmap(900, 520, 96, 96, PixelFormats.Pbgra32); image.Render(visual);
        new ExportService().SavePng(image, path);
    }
    private static byte[] Pixels(BitmapSource image)
    {
        var converted = new FormatConvertedBitmap(image, PixelFormats.Pbgra32, null, 0);
        var pixels = new byte[image.PixelWidth * image.PixelHeight * 4]; converted.CopyPixels(pixels, image.PixelWidth * 4, 0); return pixels;
    }
    public static void Run(Action<bool, string> check, string folder)
    {
        check(ToolCatalog.Categories.Count == 7 && ToolCatalog.Categories.Select(c => c.Id).Distinct().Count() == 7, "seven stable tool categories");
        check(ToolCatalog.Tools.Select(t => t.Id).Distinct().Count() == ToolCatalog.Tools.Count &&
            Enum.GetValues<EditorTool>().All(t => ToolCatalog.Tools.Count(d => d.Tool == t) == 1) &&
            ToolCatalog.Tools.All(t => ToolCatalog.Categories.Any(c => c.Id == t.CategoryId)), "registry tools have unique IDs and valid categories");
        check(ToolCatalog.Stamps.Select(s => s.Id).Distinct().Count() == ToolCatalog.Stamps.Count &&
            new[] { "RFI", "MARKUP", "FOR COORDINATION", "VERIFY" }.All(t => ToolCatalog.Stamps.Any(s => s.Text == t)), "design stamp catalog");
        const int w = 32, h = 24;
        var raw = new byte[w * h * 4];
        for (var y = 0; y < h; y++) for (var x = 0; x < w; x++)
        {
            var i = (y * w + x) * 4; var value = (byte)(x % 2 == 0 ? 0 : 255);
            raw[i] = raw[i + 1] = raw[i + 2] = value; raw[i + 3] = 255;
        }
        var source = BitmapSource.Create(w, h, 144, 144, PixelFormats.Pbgra32, null, raw, w * 4); source.Freeze();
        var effects = new PixelEffects();
        var mosaic = new Annotation(Guid.NewGuid(), AnnotationKind.Pixelate, new(4, 4), new(20, 16), EffectSize: 4);
        var processed = Pixels(effects.Process(source, mosaic));
        check(processed.Where((_, i) => i % 4 != 3).All(b => b == 127), "pixelate averages complete blocks without changing alpha");
        var blur = mosaic with { Id = Guid.NewGuid(), Kind = AnnotationKind.Blur };
        var blurred = Pixels(effects.Process(source, blur));
        check(blurred.Where((_, i) => i % 4 != 3).All(b => b > 0 && b < 255), "blur reduces source stripe detail");
        check(Pixels(source).SequenceEqual(raw), "ROI processing leaves original pixels untouched");
        var solidPixels = Pixels(effects.Process(source, mosaic with { Kind = AnnotationKind.Redact }));
        check(solidPixels.Where((_, i) => i % 4 != 3).All(b => b == 0) && solidPixels.Where((_, i) => i % 4 == 3).All(b => b == 255), "direct redaction processing returns only opaque black pixels");
        var clipped = mosaic with { Start = new(-4, -3), End = new(6, 5) };
        check(PixelEffects.ClippedBounds(source, clipped) == new PixelRect(0, 0, 6, 5), "effects clamp fractional/off-image bounds");
        var edge = mosaic with { Start = new(31, 23), End = new(40, 30), Kind = AnnotationKind.Blur, EffectSize = 48 };
        check(effects.Process(source, edge).PixelWidth == 1 && effects.Process(source, edge).PixelHeight == 1, "one-pixel edge ROI handles large blur radius");
        var moved = mosaic.Move(1, 0);
        check(!ReferenceEquals(effects.Process(source, mosaic), effects.Process(source, moved)), "effect cache invalidated for moved annotations");
        var changed = mosaic with { EffectSize = 8 };
        check(!ReferenceEquals(effects.Process(source, mosaic), effects.Process(source, changed)), "effect cache invalidated for strength changes");
        var document = new EditorDocument(w, h); document.Annotations.Add(blur);
        var renderer = new AnnotationRenderer(); var composed = Pixels(renderer.Render(source, document));
        check(composed[0] == raw[0] && composed[(2 * w + 25) * 4] == raw[(2 * w + 25) * 4], "effect export leaves pixels outside ROI unchanged");
        document.Annotations.Add(new(Guid.NewGuid(), AnnotationKind.Redact, new(8, 8), new(16, 16)));
        document.Annotations.Add(mosaic);
        composed = Pixels(renderer.Render(source, document));
        var covered = (10 * w + 10) * 4;
        check(composed[covered] == 0 && composed[covered + 1] == 0 && composed[covered + 2] == 0 && composed[covered + 3] == 255,
            "solid cover stays opaque under overlapping later pixelate");
        document.Crop = new(8, 8, 8, 8);
        var cropped = renderer.Render(source, document); var cropPixels = Pixels(cropped);
        check(cropped.PixelWidth == 8 && cropped.PixelHeight == 8 && cropPixels.Where((_, i) => i % 4 != 3).All(b => b == 0), "crop retains solid redaction at translated origin");
        new ExportService().SavePng(cropped, System.IO.Path.Combine(folder, "redacted.png"));
        check(Pixels(HistoryService.LoadImage(System.IO.Path.Combine(folder, "redacted.png"))).SequenceEqual(cropPixels), "redacted PNG round trip preserves processed pixels");
        var history = new CommandHistory(); var original = document.Annotations[0];
        history.Execute(new ChangeAnnotationCommand(document, original, original with { EffectSize = 20 })); history.Undo();
        check(document.Annotations[0] == original, "effect strength undo restores annotation");
        history.Redo(); check(document.Annotations[0].EffectSize == 20, "effect strength redo");
        var stamp = new Annotation(Guid.NewGuid(), AnnotationKind.Stamp, new(5, 5), new(5, 5), "#EF4444", 3, "ตรวจสอบ / VERIFY", 18);
        var bounds = AnnotationRenderer.GetBounds(stamp);
        check(bounds.Width > 100 && bounds.Height > 20, "stamp bounds use actual Thai/English text metrics");
        var stampDocument = new EditorDocument(400, 100); stampDocument.Annotations.Add(stamp);
        var white = BitmapSource.Create(400, 100, 96, 96, PixelFormats.Pbgra32, null, Enumerable.Repeat((byte)255, 400 * 100 * 4).ToArray(), 400 * 4);
        var stampImage = renderer.Render(white, stampDocument);
        check(Pixels(stampImage).Where((_, i) => i % 4 == 2).Any(b => b != 255), "stamp text/border renders into exported image");
        history = new(); history.Execute(new ChangeAnnotationCommand(stampDocument, stamp, stamp with { Text = "RFI-02" })); history.Undo();
        check(stampDocument.Annotations[0].Text == stamp.Text, "stamp text edit is undoable");
        // Exercise the actual WPF surface, including a moved effect and stamp; compare pixels to export.
        var surface = new EditorSurface(white);
        surface.Document.Annotations.Add(stamp); surface.Document.Annotations.Add(new(Guid.NewGuid(), AnnotationKind.Redact, new(300, 5), new(380, 50)));
        surface.Measure(new Size(400, 100)); surface.Arrange(new Rect(0, 0, 400, 100)); surface.UpdateLayout();
        var preview = new RenderTargetBitmap(400, 100, 96, 96, PixelFormats.Pbgra32); preview.Render(surface);
        check(Pixels(preview).SequenceEqual(Pixels(renderer.Render(white, surface.Document))), "actual editor preview matches flattened output without selection UI");
        var settings = new SettingsService(System.IO.Path.Combine(folder, "privacy")); settings.Save(new() { KeepHistory = false });
        check(!settings.Load().KeepHistory, "disable original history setting round trip");
    }
}
