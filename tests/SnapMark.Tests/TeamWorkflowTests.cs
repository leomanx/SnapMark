using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.App;
using SnapMark.Core;
using SnapMark.Editor;
using SnapMark.Export;

internal static class TeamWorkflowTests
{
    public static void Run(Action<bool, string> check, string folder)
    {
        var pixels = Enumerable.Repeat((byte)0, 320 * 160 * 4).ToArray(); for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255;
        var image = BitmapSource.Create(320, 160, 96, 96, PixelFormats.Bgra32, null, pixels, 320 * 4); image.Freeze();
        var annotation = new Annotation(Guid.NewGuid(), AnnotationKind.Text, new(10, 10), new(10, 10), "#FF0000", Text: "MARKUP\nTEAM", TextBackground: true, TextBackgroundColor: "#FFFFFF", TextBackgroundOpacity: .5, TextPadding: 12);
        var doc = new EditorDocument(320, 160); doc.Annotations.Add(annotation);
        var renderer = new AnnotationRenderer(); var bounds = AnnotationRenderer.GetBounds(annotation);
        check(bounds.Width > AnnotationRenderer.GetBounds(annotation with { TextBackground = false }).Width + 20, "text background padding participates in selection bounds");
        var rendered = renderer.Render(image, doc); var sample = new byte[4]; rendered.CopyPixels(new Int32Rect(14, 14, 1, 1), sample, 4, 0);
        check(sample[0] is >= 120 and <= 135 && sample[1] == sample[0], "half opacity text background blends with source pixels");
        var project = Path.Combine(folder, "background.snapmark"); ProjectStorage.Save(project, image, doc);
        check(ProjectStorage.Load(project).Document.Annotations.Single() == annotation, "editable project round trip retains text background fields");
        var surface = new EditorSurface(image) { TextBackground = true, TextBackgroundColor = "#123456", TextPadding = 16, TextBackgroundOpacity = .7 };
        var presets = new PresetStore(Path.Combine(folder, "background-presets.json")); presets.Save(surface.SaveStyle("Background")); surface.TextBackground = false; surface.LoadStyle(presets.Load().Single());
        check(surface.TextBackground && surface.TextBackgroundColor == "#123456" && surface.TextPadding == 16 && surface.TextBackgroundOpacity == .7, "personal typography preset restores background style");
        surface.Document.Annotations.Add(annotation); ((HashSet<Guid>)surface.Selection).Add(annotation.Id); surface.ApplyTextBackgroundToSelection();
        check(surface.Document.Annotations.Single().TextBackgroundColor == "#123456", "selected existing text accepts new background style");
        surface.History.Undo(); check(surface.Document.Annotations.Single() == annotation, "text background change restores previous style with one undo");
        doc.Annotations[0] = annotation with { TextPadding = -1 }; ProjectStorage.Save(project, image, doc);
        var rejected = false; try { ProjectStorage.Load(project); } catch (InvalidDataException) { rejected = true; } check(rejected, "malformed project text background is rejected before rendering");
        foreach (var b in new[] { new PixelRect(100, 100, 600, 400), new(-1900, 2, 1800, 900), new(-1920, 0, 3840, 1080) })
        {
            var desktop = new PixelRect(-1920, 0, 3840, 1080); var strips = RecordingFrame.Strips(b, desktop);
            check(strips.All(s => s.Intersect(b).Width * s.Intersect(b).Height == 0 && s == s.Intersect(desktop)), "recording frame stays outside encoded pixels and within physical desktop");
        }
        check(RecordingFrame.Strips(new(100, 100, 600, 400), new(0, 0, 1920, 1080)).Count == 4, "partial desktop capture has all four boundary strips");
        check(TeamFolder.ValidateLink("https://drive.google.com/drive/folders/demo", true).Contains("folders/demo"), "Google shared folder URL accepted");
        check(TeamFolder.ValidateLink("https://tenant.sharepoint.com/:f:/team", false).Contains("sharepoint.com"), "OneDrive business team URL accepted");
        foreach (var url in new[] { "http://drive.google.com/drive/folders/x", "https://drive.google.com.evil.test/x", "file:///C:/x" })
        { rejected = false; try { TeamFolder.ValidateLink(url, true); } catch (ArgumentException) { rejected = true; } check(rejected, "unsafe provider link rejected"); }
        check(TeamFolder.NewFile(folder, false) != TeamFolder.NewFile(folder, false), "team exports get unique filenames for concurrent collaborators");
        var sourceVideo = Path.Combine(folder, "source.mp4"); File.WriteAllBytes(sourceVideo, [1, 2, 3, 4]);
        var copiedVideo = TeamFolder.CopyFileAsync(sourceVideo, folder).GetAwaiter().GetResult();
        check(File.ReadAllBytes(copiedVideo).SequenceEqual(File.ReadAllBytes(sourceVideo)) && copiedVideo != sourceVideo && copiedVideo.EndsWith(".mp4"), "team file transfer preserves source and extension with an independent copy");
        using var cancelCopy = new CancellationTokenSource(); cancelCopy.Cancel(); rejected = false;
        try { TeamFolder.CopyFileAsync(sourceVideo, folder, cancelCopy.Token).GetAwaiter().GetResult(); } catch (OperationCanceledException) { rejected = true; }
        check(rejected && !Directory.EnumerateFiles(folder, "*.tmp").Any(), "cancelled team copy leaves no partial destination or temporary file");
        var cachePath = Path.Combine(folder, "indexed.png"); new ExportService().SavePng(image, cachePath); HistoryTextIndex.WriteCache(cachePath, "en-US", "RFI DETAIL 42");
        check(HistoryTextIndex.Cached(cachePath, "en-US").Contains("DETAIL 42") && HistoryTextIndex.Cached(cachePath, "th-TH") == "", "image text index respects recognition language");
        File.SetLastWriteTimeUtc(cachePath, DateTime.UtcNow.AddMinutes(1)); check(HistoryTextIndex.Cached(cachePath, "en-US") == "", "changed image invalidates old recognized text");
        File.WriteAllText(cachePath + ".ocr.json", "{broken"); check(HistoryTextIndex.Cached(cachePath, "en-US") == "", "corrupt OCR cache cannot break history search");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel(); rejected = false;
        try { HistoryTextIndex.RecognizeAsync(cachePath, "en-US", cancelled.Token).GetAwaiter().GetResult(); } catch (OperationCanceledException) { rejected = true; }
        check(rejected, "cancelled indexing does not start recognition");
        if (LocalOcr.Languages().Any(l => l.Tag == "en-US"))
        {
            var visual = new DrawingVisual(); using (var dc = visual.RenderOpen()) { dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, 700, 140)); dc.DrawText(new FormattedText("SNAPMARK RFI 123", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 48, Brushes.Black, 1), new Point(20, 20)); }
            var textImage = new RenderTargetBitmap(700, 140, 96, 96, PixelFormats.Pbgra32); textImage.Render(visual); textImage.Freeze(); new ExportService().SavePng(textImage, cachePath);
            var recognized = HistoryTextIndex.RecognizeAsync(cachePath, "en-US").GetAwaiter().GetResult();
            check(recognized.Contains("SNAPMARK") && HistoryTextIndex.Cached(cachePath, "en-US").Contains("123"), "history index recognizes and persists actual image text");
        }
    }
}
