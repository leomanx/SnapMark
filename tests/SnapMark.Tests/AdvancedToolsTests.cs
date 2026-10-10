using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;
using SnapMark.Editor;
using SnapMark.Recording;

internal static class AdvancedToolsTests
{
    private static byte[] Pixels(BitmapSource source)
    {
        var converted = new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);
        var result = new byte[source.PixelWidth * source.PixelHeight * 4]; converted.CopyPixels(result, source.PixelWidth * 4, 0); return result;
    }
    private static BitmapSource White(int w, int h) => BitmapSource.Create(w, h, 96, 96, PixelFormats.Pbgra32, null, Enumerable.Repeat((byte)255, w * h * 4).ToArray(), w * 4);
    public static void Run(Action<bool, string> check, string folder)
    {
        check(ToolCatalog.Categories.All(c => ToolCatalog.Tools.Any(t => t.CategoryId == c.Id)), "all seven categories contain functioning tools");
        check(ColorMath.FromHsv(0, 1, 1) == Colors.Red && ColorMath.FromHsv(120, 1, 1) == Colors.Lime && ColorMath.FromHsv(240, 1, 1) == Colors.Blue, "color wheel HSV primary hues");
        check(ColorMath.FromHsv(-120, 1, 1) == Colors.Blue && ColorMath.FromHsv(360, 1, 1) == Colors.Red, "color wheel hue wraps in both directions");
        check(ColorMath.FromHsv(75, 0, 1) == Colors.White && ColorMath.FromHsv(75, 1, 0) == Colors.Black, "color wheel neutral and zero brightness colors");
        check(ColorMath.TryHex(" #a3f ", out var shortHex) && ColorMath.Hex(shortHex) == "#AA33FF" && !ColorMath.TryHex("#GGGGGG", out _) && !ColorMath.TryHex("#12345", out _), "custom HEX shorthand normalization and invalid input rejection");
        var sampleColor = Color.FromRgb(24, 137, 218); var hsv = ColorMath.ToHsv(sampleColor);
        check(ColorMath.FromHsv(hsv.Hue, hsv.Saturation, hsv.Value) == sampleColor, "custom color RGB HSV round trip");
        check(GeometryMath.StepLabel(26, true) == "Z" && GeometryMath.StepLabel(27, true) == "AA" && GeometryMath.StepLabel(52, true) == "AZ", "alphabetic steps continue beyond Z");
        check(GeometryMath.ParseStep("XX", true) == 648 && GeometryMath.ParseStep("12", false) == 12, "step starting labels parse numeric/alphabetic values");
        var polygon = new PixelPoint[] { new(0, 0), new(10, 0), new(10, 20), new(0, 20) };
        check(GeometryMath.Area(polygon) == 200 && GeometryMath.Length(polygon) == 40, "polygon area and continuous path length");
        check(Math.Abs(GeometryMath.Angle(new(0, 10), new(0, 0), new(10, 0)) - 90) < .001, "three-point angle measurement");
        var a = new Annotation(Guid.NewGuid(), AnnotationKind.Polygon, polygon[0], polygon[^1], Points: polygon);
        var moved = a.Move(5, 8);
        check(moved.Points![2] == new PixelPoint(15, 28), "moving polygon transforms every vertex");
        var resized = a.Resize(new(0, 0, 20, 40));
        check(resized.Points![2] == new PixelPoint(20, 40), "resizing polygon transforms every vertex");
        var document = new EditorDocument(100, 100); var history = new CommandHistory();
        history.Execute(new CalibrationCommand(document, 10, "mm"));
        check(document.UnitsPerPixel == 10 && document.Unit == "mm", "calibration stores scale and unit per image");
        history.Undo(); check(document.UnitsPerPixel == 1 && document.Unit == "px", "calibration undo restores pixel units");
        document.Annotations.Add(a); var b = a with { Id = Guid.NewGuid() }; document.Annotations.Add(b);
        history.Execute(new ReorderAnnotationCommand(document, a.Id, true)); history.Undo();
        check(document.Annotations[0].Id == a.Id, "layer reorder undo restores stacking order");
        history.Execute(new CompositeCommand(new IEditorCommand[] { new DeleteAnnotationCommand(document, a), new DeleteAnnotationCommand(document, b) })); history.Undo();
        check(document.Annotations.Select(x => x.Id).SequenceEqual(new[] { a.Id, b.Id }), "multi-delete undo restores all objects in order");
        var inside = new Annotation(Guid.NewGuid(), AnnotationKind.Rectangle, new(10, 10), new(20, 20));
        var crossing = inside with { Id = Guid.NewGuid(), Start = new(25, 15), End = new(45, 25) };
        var outside = inside with { Id = Guid.NewGuid(), Start = new(70, 70), End = new(90, 90) };
        check(MarqueeHitTest.Matches(inside, new(0, 0), new(30, 30)) && !MarqueeHitTest.Matches(crossing, new(0, 0), new(30, 30)), "eraser left-to-right requires full enclosure");
        check(MarqueeHitTest.Matches(crossing, new(30, 30), new(0, 0)) && !MarqueeHitTest.Matches(outside, new(30, 30), new(0, 0)), "eraser right-to-left includes crossing objects only");
        var diagonal = new Annotation(Guid.NewGuid(), AnnotationKind.Line, new(0, 0), new(100, 100));
        check(!MarqueeHitTest.Matches(diagonal, new(90, 20), new(80, 10)) && MarqueeHitTest.Matches(diagonal, new(60, 60), new(40, 40)), "crossing selection checks actual line rather than its bounding box");
        var rotated = inside with { Rotation = 45 };
        check(!MarqueeHitTest.Matches(rotated, new(10, 10), new(20, 20)) && MarqueeHitTest.Matches(rotated, new(0, 0), new(30, 30)), "window enclosure respects rotated bounds");
        var eraseDocument = new EditorDocument(100, 100); eraseDocument.Annotations.AddRange(new[] { inside, crossing, outside });
        var eraseSurface = new EditorSurface(White(100, 100), eraseDocument);
        eraseSurface.EraseRegion(new(30, 30), new(0, 0));
        check(eraseDocument.Annotations.SequenceEqual(new[] { outside }), "marquee eraser deletes matching batch");
        eraseSurface.History.Undo();
        check(eraseDocument.Annotations.SequenceEqual(new[] { inside, crossing, outside }), "one undo restores erased batch and stacking order");
        eraseSurface.History.Redo();
        check(eraseDocument.Annotations.SequenceEqual(new[] { outside }), "redo erases the same batch");
        eraseSurface.EraseRegion(new(0, 0), new(0, 0)); eraseSurface.History.Undo();
        check(eraseDocument.Annotations.Count == 3, "empty eraser rectangle does not add undo step");
        var source = White(120, 100); var renderer = new AnnotationRenderer();
        foreach (var kind in new[] { AnnotationKind.Line, AnnotationKind.RoundedRectangle, AnnotationKind.Ellipse, AnnotationKind.Circle, AnnotationKind.Triangle,
            AnnotationKind.Polygon, AnnotationKind.Freehand, AnnotationKind.Polyline, AnnotationKind.OpenDot, AnnotationKind.FilledDot, AnnotationKind.Plane,
            AnnotationKind.Callout, AnnotationKind.Note, AnnotationKind.RevisionCloud, AnnotationKind.Dimension, AnnotationKind.Distance, AnnotationKind.Length,
            AnnotationKind.Area, AnnotationKind.Angle, AnnotationKind.Symbol })
        {
            var doc = new EditorDocument(120, 100);
            doc.Annotations.Add(new(Guid.NewGuid(), kind, new(20, 20), new(90, 70), Text: "A", FontSize: 14,
                Points: kind is AnnotationKind.Polygon or AnnotationKind.Freehand or AnnotationKind.Polyline or AnnotationKind.Length or AnnotationKind.Area or AnnotationKind.Angle ? new PixelPoint[] { new(20, 20), new(90, 20), new(90, 70) } : null, Variant: "north", Closed: true));
            check(Pixels(renderer.Render(source, doc)).Any(x => x != 255), "render/export tool: " + kind);
        }
        foreach (var symbol in ToolCatalog.Symbols)
        {
            var doc = new EditorDocument(120, 100); doc.Annotations.Add(new(Guid.NewGuid(), AnnotationKind.Symbol, new(20, 20), new(90, 70), Text: "1", FontSize: 14, Variant: symbol));
            check(Pixels(renderer.Render(source, doc)).Any(x => x != 255), "drawing symbol: " + symbol);
        }
        var gradientDoc = new EditorDocument(120, 100);
        gradientDoc.Annotations.Add(new(Guid.NewGuid(), AnnotationKind.Plane, new(10, 10), new(110, 90), Fill: ShapeFill.Gradient, SecondColor: "#0000FF", Opacity: .5));
        var gradient = Pixels(renderer.Render(source, gradientDoc));
        check(gradient[(20 * 120 + 20) * 4] != gradient[(70 * 120 + 100) * 4] && gradient[(20 * 120 + 20) * 4 + 1] >= 120, "gradient and opacity blend into final output");
        var solidDoc = new EditorDocument(120, 100); solidDoc.Annotations.Add(new(Guid.NewGuid(), AnnotationKind.Line, new(10, 50), new(110, 50)));
        var dashDoc = new EditorDocument(120, 100); dashDoc.Annotations.Add(solidDoc.Annotations[0] with { LinePattern = LinePattern.Dash });
        check(Pixels(renderer.Render(source, solidDoc)).Count(x => x != 255) > Pixels(renderer.Render(source, dashDoc)).Count(x => x != 255), "dash line produces visible gaps");
        var imageSurface = new EditorSurface(source); var red = BitmapSource.Create(30, 30, 96, 96, PixelFormats.Bgra32, null,
            Enumerable.Range(0, 30 * 30 * 4).Select(i => (byte)(i % 4 is 2 or 3 ? 255 : 0)).ToArray(), 30 * 4);
        imageSurface.InsertImage(red);
        check(imageSurface.Document.Assets.Count == 1 && imageSurface.Document.Annotations[0].Kind == AnnotationKind.Image, "history/clipboard insertion creates embedded editable image object");
        var composed = Pixels(renderer.Render(source, imageSurface.Document)); var index = (25 * 120 + 25) * 4;
        check(composed[index] == 0 && composed[index + 2] == 255, "inserted image is included in flattened export");
        imageSurface.Document.Annotations.Insert(0, new(Guid.NewGuid(), AnnotationKind.Redact, new(20, 20), new(50, 50)));
        composed = Pixels(renderer.Render(source, imageSurface.Document)); check(composed[index + 2] == 0 && composed[index + 3] == 255, "redaction covers inserted images regardless of stacking order");
        imageSurface.ResizeCanvas(200, 100); check(renderer.Render(source, imageSurface.Document).PixelWidth == 200, "canvas expansion supports side-by-side composition");
        imageSurface.History.Undo(); check(imageSurface.Document.Width == 120, "canvas expansion undo restores original size");
        var project = System.IO.Path.Combine(folder, "composition.snapmark");
        imageSurface.Document.UnitsPerPixel = 3.5; imageSurface.Document.Unit = "mm";
        imageSurface.Document.Annotations.Add(new(Guid.NewGuid(), AnnotationKind.Polygon, new(1, 1), new(10, 10), Points: polygon, Fill: ShapeFill.Gradient, Opacity: .5, LinePattern: LinePattern.Dash));
        ProjectStorage.Save(project, source, imageSurface.Document); var loaded = ProjectStorage.Load(project);
        check(loaded.Document.Assets.Count == 1 && loaded.Document.Unit == "mm" && loaded.Document.UnitsPerPixel == 3.5, "editable project preserves image layers and calibration");
        check(loaded.Document.Annotations[^1].Points!.SequenceEqual(polygon) && loaded.Document.Annotations[^1].Fill == ShapeFill.Gradient && loaded.Document.Annotations[^1].LinePattern == LinePattern.Dash, "editable project preserves polygon geometry and styles");
        check(Pixels(renderer.Render(loaded.Image, loaded.Document)).SequenceEqual(Pixels(renderer.Render(source, imageSurface.Document))), "editable project round trip preserves flattened output pixels");
        var args = RecordingService.Arguments(new(new(-100, 20, 101, 99), "out.mp4", 15, false), "temporary.mp4");
        check(args.Contains("gdigrab") && args.Contains("-100") && args.Contains("101x99") && args.Contains("pad=ceil(iw/2)*2:ceil(ih/2)*2"), "video capture arguments retain negative origins and pad odd output sizes");
        check(args.Contains("-an") && args.Contains("0") && args.Last() == "temporary.mp4", "video recording is local, silent and targets temporary output");
    }
    public static void RecordSmoke(Action<bool, string> check, string folder)
    {
        var bounds = SnapMark.Capture.NativeDesktop.Monitors.First();
        var request = new RecordingRequest(new(bounds.X, bounds.Y, 65, 63), System.IO.Path.Combine(folder, "video-test.mp4"), 10, false);
        var recorder = new RecordingService(); recorder.StartAsync(request).GetAwaiter().GetResult();
        check(recorder.IsRecording, "real FFmpeg desktop capture starts");
        Thread.Sleep(1200); var result = recorder.StopAsync().GetAwaiter().GetResult();
        check(!recorder.IsRecording && new System.IO.FileInfo(result).Length > 100, "real MP4 recording finalizes on stop");
        var encoder = RecordingService.FindEncoder()!;
        var info = new ProcessStartInfo(encoder) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
        foreach (var value in new[] { "-v", "error", "-i", result, "-f", "null", "-" }) info.ArgumentList.Add(value);
        using var process = Process.Start(info)!; var errors = process.StandardError.ReadToEnd(); process.WaitForExit();
        check(process.ExitCode == 0 && errors.Length == 0, "recorded MP4 fully decodes without errors");
    }
}
