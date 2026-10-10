using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;
using SnapMark.Editor;
using SnapMark.Export;
using SnapMark.Capture;

internal static class Program
{
    private static int assertions;
    private static void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL: " + name); assertions++; Console.WriteLine("PASS: " + name); }
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--audio-devices")) { Console.WriteLine("Default: " + SnapMark.Recording.AudioSession.DefaultDeviceId(false)); foreach (var d in SnapMark.Recording.AudioSession.Devices(false)) Console.WriteLine(d.Id + " " + d.Name); Console.WriteLine("Microphone default: " + SnapMark.Recording.AudioSession.DefaultDeviceId(true)); foreach (var d in SnapMark.Recording.AudioSession.Devices(true)) Console.WriteLine(d.Id + " " + d.Name); return; }
        if (Environment.GetEnvironmentVariable("SNAPMARK_TEST_WORKER") is { } workerPath) SnapMark.Recording.AudioWorker.Executable = workerPath;
        if (args.Contains("--audio-only")) { var audioFolder = Path.Combine(Path.GetTempPath(), "SnapMark-audio-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(audioFolder); try { RoadmapTests.AudioSmoke(Check, audioFolder); } finally { Directory.Delete(audioFolder, true); } Console.WriteLine($"{assertions} audio assertions passed."); return; }
        var rect = PixelRect.Between(new(20, 40), new(-10, 5));
        Check(rect == new PixelRect(-10, 5, 30, 35), "reverse region drag / negative screen coordinates");
        Check(new PixelRect(-100, 0, 200, 100).Intersect(new(0, 0, 200, 100)) == new PixelRect(0, 0, 100, 100), "monitor intersection");
        var recordingDesktop = new PixelRect(-500, -100, 1500, 1000); var recordingRegion = new PixelRect(-200, 50, 320, 240);
        foreach (var handle in RecordingRegionGeometry.Handles(recordingRegion))
        {
            Check(RecordingRegionGeometry.Hit(recordingRegion, handle.Point) == handle.Handle, "recording resize handle: " + handle.Handle);
            var resized = RecordingRegionGeometry.Drag(recordingRegion, handle.Handle, handle.Point, new(handle.Point.X + 25, handle.Point.Y + 15), recordingDesktop);
            Check(resized != recordingRegion && resized.Width >= 2 && resized.Height >= 2 && resized.Intersect(recordingDesktop) == resized, "recording handle resizes inside desktop: " + handle.Handle);
        }
        Check(RecordingRegionGeometry.Drag(recordingRegion, RegionHandle.Move, new(0, 0), new(50, 30), recordingDesktop) == new PixelRect(-150, 80, 320, 240), "recording frame moves without changing physical pixel dimensions");
        var minimum = RecordingRegionGeometry.Drag(recordingRegion, RegionHandle.TopLeft, new(-200, 50), new(9999, 9999), recordingDesktop);
        Check(minimum.Width == 2 && minimum.Height == 2 && minimum.Right == recordingRegion.Right && minimum.Bottom == recordingRegion.Bottom, "recording resize clamps crossing handles to minimum size while preserving opposite corner");
        Check(RecordingRegionGeometry.Drag(recordingRegion, RegionHandle.Move, new(0, 0), new(-9999, -9999), recordingDesktop) == new PixelRect(-500, -100, 320, 240), "recording frame movement clamps correctly on negative monitor coordinates");
        var doc = new EditorDocument(100, 80); var history = new CommandHistory();
        var a = new Annotation(Guid.NewGuid(), AnnotationKind.Rectangle, new(10, 10), new(40, 30), "#FF0000", 2);
        history.Execute(new AddAnnotationCommand(doc, a)); history.Undo(); Check(doc.Annotations.Count == 0 && history.CanRedo, "undo add");
        history.Redo(); Check(doc.Annotations.Single() == a, "redo add");
        history.Execute(new ChangeAnnotationCommand(doc, a, a.Move(5, -2))); history.Undo(); Check(doc.Annotations.Single() == a, "undo move");
        history.Execute(new CropCommand(doc, new(10, 10, 40, 30))); Check(!history.CanRedo, "new command invalidates redo");
        history.Undo(); Check(doc.Crop is null, "non-destructive crop undo"); history.Redo();
        history.Execute(new DeleteAnnotationCommand(doc, a)); history.Undo(); Check(doc.Annotations.Single() == a, "delete preserves z order on undo");
        var pixels = Enumerable.Repeat((byte)255, 100 * 80 * 4).ToArray();
        var image = BitmapSource.Create(100, 80, 144, 144, PixelFormats.Bgra32, null, pixels, 100 * 4); image.Freeze();
        var rendered = new AnnotationRenderer().Render(image, doc);
        Check(rendered.PixelWidth == 40 && rendered.PixelHeight == 30, "export crop in image pixels regardless of source DPI");
        var output = new byte[40 * 30 * 4]; rendered.CopyPixels(output, 40 * 4, 0);
        Check(output[2] > 200 && output[1] < 100, "vector annotation renders at cropped origin");
        Check(doc.Annotations.Count == 1 && image.PixelWidth == 100, "render preserves base image and vectors");
        var folder = Path.Combine(Path.GetTempPath(), "SnapMark-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var export = new ExportService(); var path = Path.Combine(folder, "test.png"); export.SavePng(rendered, path);
            Check(HistoryService.LoadImage(path).PixelWidth == 40, "PNG round trip");
            var settings = new SettingsService(folder); settings.Save(new() { AutoCopy = false, HistoryLimit = 50 });
            Check(!settings.Load().AutoCopy && settings.Load().HistoryLimit == 50, "settings round trip");
            var savedEncoder = SnapMark.Recording.RecordingService.EncoderPath;
            try
            {
                var encoder = Path.Combine(folder, "ffmpeg.exe"); File.WriteAllText(encoder, "path-resolution fixture, never executed");
                settings.Save(new() { FfmpegPath = encoder }); Check(settings.Load().FfmpegPath == encoder, "explicit FFmpeg preference persists");
                SnapMark.Recording.RecordingService.EncoderPath = encoder; Check(SnapMark.Recording.RecordingService.FindEncoder() == encoder, "explicit FFmpeg preference takes precedence");
                File.Delete(encoder); Check(SnapMark.Recording.RecordingService.FindEncoder() is null, "missing explicit encoder does not silently use PATH");
                SnapMark.Recording.RecordingService.EncoderPath = "ffmpeg.exe"; Check(SnapMark.Recording.RecordingService.FindEncoder() is null, "relative FFmpeg preference rejected");
            }
            finally { SnapMark.Recording.RecordingService.EncoderPath = savedEncoder; }
            File.WriteAllText(Path.Combine(folder, "settings.json"), "{broken"); Check(settings.Load().AutoCopy, "malformed settings recovery");
            var captures = new HistoryService(Path.Combine(folder, "history"), export);
            captures.Add(image, 2); captures.Add(image, 2); captures.Add(image, 2);
            Check(captures.Entries().Count == 2, "bounded local history retention");
            DesignToolsTests.Run(Check, folder);
            AdvancedToolsTests.Run(Check, folder);
            WorkflowTests.Run(Check, folder);
            RoadmapTests.Run(Check, folder);
            PhotoToolsTests.Run(Check, folder, args.Contains("--recording"));
            if (args.Contains("--audio")) Task.Run(() => RoadmapTests.AudioSmoke(Check, folder)).GetAwaiter().GetResult();
            var pluginIndex = Array.IndexOf(args, "--plugin"); if (pluginIndex >= 0 && pluginIndex + 1 < args.Length) RoadmapTests.PluginSmoke(Check, folder, args[pluginIndex + 1]);
            if (args.Contains("--recording")) { AdvancedToolsTests.RecordSmoke(Check, folder); MediaSmoke.Run(Check, folder); }
            if (args.Contains("--capture"))
            {
                var bounds = NativeDesktop.VirtualBounds; var screen = NativeDesktop.Snapshot(bounds);
                Check(screen.PixelWidth == (int)bounds.Width && screen.PixelHeight == (int)bounds.Height, "real desktop capture dimensions");
                var crop = NativeDesktop.Crop(screen, new(0, 0, 16, 16)); Check(crop.PixelWidth == 16, "real captured bitmap crop");
            }
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
        Console.WriteLine($"{assertions} assertions passed.");
        if (args.Contains("--ui-model")) { UiLayoutTests.Run(Check); Console.WriteLine($"{assertions} assertions passed including UI model."); }
        var demo = Array.IndexOf(args, "--demo");
        if (demo >= 0 && demo + 1 < args.Length) DesignToolsTests.WriteDemo(args[demo + 1]);
    }
}
