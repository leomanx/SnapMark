using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;
using SnapMark.Editor;
using SnapMark.Capture;
using SnapMark.Export;

internal static class WorkflowTests
{
    public static void Run(Action<bool, string> check, string folder)
    {
        var image = BitmapSource.Create(20, 20, 96, 96, PixelFormats.Bgra32, null, new byte[20 * 20 * 4], 80); image.Freeze();
        var doc = new EditorDocument(20, 20); var annotation = new Annotation(Guid.NewGuid(), AnnotationKind.Line, new(2, 2), new(12, 12)); doc.Annotations.Add(annotation);
        var folderSettings = new AppSettings { HistoryFolder = Path.Combine(folder, "custom-history"), RecoveryFolder = Path.Combine(folder, "custom-recovery"), VideoFolder = Path.Combine(folder, "videos"), ProjectFolder = Path.Combine(folder, "projects"), ExportFolder = Path.Combine(folder, "exports") };
        var settingsStore = new SettingsService(Path.Combine(folder, "folder-settings")); settingsStore.Save(folderSettings); var loadedFolders = settingsStore.Load();
        check(loadedFolders.VideoFolder == folderSettings.VideoFolder && loadedFolders.HistoryFolder == folderSettings.HistoryFolder && loadedFolders.ProjectFolder == folderSettings.ProjectFolder && loadedFolders.RecoveryFolder == folderSettings.RecoveryFolder, "storage settings persist independent history video project recovery paths");
        check(StoragePaths.History(new(), folder) == Path.Combine(folder, "history") && StoragePaths.DataSetting(Path.Combine(folder, "history") + Path.DirectorySeparatorChar, Path.Combine(folder, "history")) == "", "default history remains portable and reset normalizes trailing separator");
        bool invalidFolder = false; try { StoragePaths.Validate("relative/history"); } catch (ArgumentException) { invalidFolder = true; } check(invalidFolder, "storage settings reject relative folders");
        Directory.CreateDirectory(folderSettings.HistoryFolder); var unrelated = Path.Combine(folderSettings.HistoryFolder, "personal.png"); new ExportService().SavePng(image, unrelated);
        var customHistory = new HistoryService(StoragePaths.History(loadedFolders, folder), new ExportService()); customHistory.Add(image, 1); customHistory.Add(image, 1);
        check(customHistory.Entries().Count == 1 && File.Exists(unrelated), "custom history retention preserves unrelated PNG files");
        var switchedHistory = new HistoryService(Path.Combine(folder, "switched-history"), new ExportService()); switchedHistory.Add(image, 1);
        check(customHistory.Entries().Count == 1 && switchedHistory.Entries().Count == 1, "switching history folders leaves previous captures intact");
        var recovery = new RecoveryStore(Path.Combine(folder, "recovery")); var id = Guid.NewGuid(); var other = Guid.NewGuid();
        var snapshot = RecoveryStore.Snapshot(doc); doc.Annotations.Clear();
        recovery.Save(id, image, snapshot); recovery.Save(other, image, snapshot);
        check(recovery.Load(id).Document.Annotations.Count == 1 && recovery.Entries().Count == 2, "recovery snapshots survive source edits and separate editor sessions");
        snapshot.Annotations[0] = annotation with { Text = "changed" }; recovery.Save(id, image, snapshot);
        check(recovery.Load(id).Document.Annotations[0].Text == "changed" && Directory.GetFiles(Path.Combine(folder, "recovery"), "*.tmp").Length == 0, "recovery atomically replaces latest snapshot without temporary residue");
        recovery.Remove(id); check(recovery.Entries().Single().Id == other, "retiring recovery leaves other editor sessions intact");
        var chord = HotkeyGesture.Parse("shift+ctrl+f8");
        check(chord.Modifiers == 6 && chord.Key == 0x77 && chord.Label == "Ctrl+Shift+F8", "configurable hotkey normalizes modifier order and virtual keys");
        check(HotkeyGesture.Parse("PrintScreen").Key == 0x2C && HotkeyGesture.Parse("Ctrl+1").Key == 0x31, "configurable hotkey supports screenshot and digit keys");
        bool invalid = false; try { HotkeyGesture.Parse("Ctrl+Ctrl+F8"); } catch (ArgumentException) { invalid = true; } check(invalid, "duplicate shortcut modifier rejected");
        check(OutputNaming.Create("Review_{date}_{time}_{kind}", "image", new(2026, 10, 5, 8, 9, 10)) == "Review_2026-10-05_080910_000_image", "output naming expands supported placeholders");
        invalid = false; try { OutputNaming.Create("../bad", "image", DateTime.Now); } catch (ArgumentException) { invalid = true; } check(invalid, "filename pattern cannot escape output folder");
        var presets = new PresetStore(Path.Combine(folder, "presets.json")); var surface = new EditorSurface(image) { Color = "#123ABC", ToolOpacity = .37, StampText = "ARCH REVIEW", Fill = ShapeFill.Gradient, LinePattern = LinePattern.Dash };
        presets.Save(surface.SaveStyle("Review")); var restored = presets.Load().Single();
        check(restored.Color == "#123ABC" && restored.Opacity == .37 && restored.StampText == "ARCH REVIEW", "personal style persists color opacity and custom stamp");
        presets.Save(restored with { Name = "review", Thickness = 6 }); check(presets.Load().Count == 1 && presets.Load()[0].Thickness == 6, "named preset replaces existing case-insensitive entry");
        surface.LoadStyle(restored); check(surface.Fill == ShapeFill.Gradient && surface.LinePattern == LinePattern.Dash, "personal preset restores shape and line settings");
        var vertices = new Annotation(Guid.NewGuid(), AnnotationKind.Polygon, new(0, 0), new(0, 10), Points: new PixelPoint[] { new(0, 0), new(10, 0), new(0, 10) }, Rotation: 90);
        var edited = EditorSurface.MoveVertex(vertices, 1, new(15, 20));
        check(edited.Points![1] == new PixelPoint(15, 20) && edited.Rotation == 0 && Math.Abs(edited.Points[0].X - 10) < .001, "vertex edit preserves other rotated vertices in screen space");
        var vertexHistory = new CommandHistory(); var vertexDoc = new EditorDocument(20, 20); vertexDoc.Annotations.Add(vertices);
        vertexHistory.Execute(new ChangeAnnotationCommand(vertexDoc, vertices, edited)); vertexHistory.Undo(); check(vertexDoc.Annotations[0] == vertices, "vertex edit undo restores original geometry and rotation");
        var svg = Path.Combine(folder, "circle.svg"); File.WriteAllText(svg, "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><circle cx='50' cy='50' r='30' fill='currentColor'/></svg>");
        var library = new SymbolLibrary(Path.Combine(folder, "symbols")); var symbol = library.ImportSvg(svg, "#123ABC");
        check(SymbolLibrary.Load(symbol).PixelWidth == 512 && library.Entries().Count == 1, "custom SVG symbol rasterizes locally and persists in library");
        File.WriteAllText(svg, "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><script>alert(1)</script></svg>");
        invalid = false; try { SvgSymbolImporter.Load(svg); } catch (InvalidDataException) { invalid = true; } check(invalid, "SVG importer rejects executable unsupported content");
        check(ImageComparison.Compare(image, image).ChangedPixels == 0, "image comparison reports identical pixels unchanged");
        var changed = new byte[20 * 20 * 4]; changed[2] = changed[3] = 255; var changedImage = BitmapSource.Create(20, 20, 96, 96, PixelFormats.Bgra32, null, changed, 80);
        check(ImageComparison.Compare(image, changedImage, 0).ChangedPixels == 1 && ImageComparison.Compare(image, changedImage, 255).ChangedPixels == 0, "image difference counts changed pixels and honors threshold");
        foreach (var extension in new[] { ".png", ".jpg", ".bmp", ".tif" }) { var path = Path.Combine(folder, "export" + extension); ImageFormats.Save(image, path); check(HistoryService.LoadImage(path).PixelWidth == 20, "image format round trip " + extension); }
        var pdf = Path.Combine(folder, "export.pdf"); ImageFormats.Save(image, pdf);
        check(PdfImporter.PageCount(pdf).GetAwaiter().GetResult() == 1, "PDF export opens with native Windows PDF engine");
        check(PdfImporter.RenderPage(pdf, 1, 96).GetAwaiter().GetResult().PixelWidth == 20, "PDF import preserves page size at 96 DPI");
        var decoded = HistoryService.LoadImage(Path.Combine(folder, "export.png"));
        Task.Run(() => recovery.Save(id, decoded, snapshot)).GetAwaiter().GetResult();
        check(recovery.Load(id).Image.PixelWidth == 20, "background autosave supports decoded image sources");
        Console.WriteLine("Installed local OCR languages: " + string.Join(", ", LocalOcr.Languages().Select(l => l.Tag)));
        if (LocalOcr.Languages().Any(l => l.Tag == "en-US"))
        {
            var visual = new DrawingVisual(); using (var dc = visual.RenderOpen()) { dc.DrawRectangle(Brushes.White, null, new System.Windows.Rect(0, 0, 1000, 160)); dc.DrawText(new System.Windows.Media.FormattedText("SNAPMARK REVIEW 123", System.Globalization.CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight, new Typeface("Arial"), 48, Brushes.Black, 1), new System.Windows.Point(30, 40)); }
            var textImage = new RenderTargetBitmap(1000, 160, 96, 96, PixelFormats.Pbgra32); textImage.Render(visual); textImage.Freeze();
            var text = LocalOcr.ReadAsync(textImage, "en-US").GetAwaiter().GetResult(); check(text.Contains("SNAPMARK") && text.Contains("123"), "Windows local OCR recognizes real English text image");
        }
        var pixels = new byte[40 * 240 * 4]; for (int y = 0; y < 240; y++) for (int x = 0; x < 40; x++) { int i = (y * 40 + x) * 4; pixels[i] = (byte)y; pixels[i + 1] = (byte)((y * 79 + x * 17) % 256); pixels[i + 3] = 255; }
        var longImage = BitmapSource.Create(40, 240, 96, 96, PixelFormats.Bgra32, null, pixels, 160); longImage.Freeze();
        var first = NativeDesktop.Crop(longImage, new(0, 0, 40, 160)); var second = NativeDesktop.Crop(longImage, new(0, 80, 40, 160)); var stitch = new ScrollStitcher(); stitch.Add(first); stitch.Add(second);
        var stitched = new byte[pixels.Length]; stitch.Render().CopyPixels(stitched, 160, 0); check(stitch.Height == 240 && stitched.SequenceEqual(pixels), "scrolling capture finds overlap and reconstructs exact pixels");
        stitch.Add(second); check(stitch.Count == 2, "scrolling duplicate frame is ignored"); stitch.Undo(); check(stitch.Height == 160 && stitch.Count == 1, "scrolling undo removes only appended frame");
        invalid = false; try { stitch.Add(image); } catch (ArgumentException) { invalid = true; } check(invalid, "scrolling incompatible frame rejected");
        var audioArgs = SnapMark.Recording.RecordingService.Arguments(new(new(0, 0, 100, 100), "out.mp4", AudioDevice: "Microphone (Test)"), "temp.mp4");
        check(audioArgs.Contains("audio=Microphone (Test)") && audioArgs.Contains("aac") && !audioArgs.Contains("-an"), "recording audio selects explicit DirectShow input and AAC");
        invalid = false; try { SnapMark.Recording.VideoTools.Arguments("same.mp4", "same.mp4", 0, 2, false); } catch (ArgumentException) { invalid = true; } check(invalid, "video export rejects overwriting source");
    }
}
