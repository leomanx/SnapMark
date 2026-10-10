using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.App;
using SnapMark.Editor;
using SnapMark.Export;
using SnapMark.Capture;
using SnapMark.Core;
using SnapMark.Recording;
using System.Windows.Interop;

internal static class UiLayoutTests
{
    public static void Run(Action<bool, string> check)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        using (var theme = typeof(UiLayoutTests).Assembly.GetManifestResourceStream("SnapMark.TestTheme.xaml")!)
        {
            var markup = System.Xml.Linq.XDocument.Load(theme);
            System.Xml.Linq.XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            var dictionary = new System.Xml.Linq.XElement(ns + "ResourceDictionary", new System.Xml.Linq.XAttribute("xmlns", ns), new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns + "x", "http://schemas.microsoft.com/winfx/2006/xaml"), markup.Root!.Element(ns + "Application.Resources")!.Elements());
            app.Resources = (ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dictionary.ToString());
        }
        var pixels = new byte[100 * 80 * 4]; var image = BitmapSource.Create(100, 80, 96, 96, PixelFormats.Bgra32, null, pixels, 400); image.Freeze();
        var selectionSurface = new EditorSurface(image) { Tool = EditorTool.Select };
        var firstShape = new Annotation(Guid.NewGuid(), AnnotationKind.Rectangle, new(10, 10), new(30, 30)); var secondShape = firstShape with { Id = Guid.NewGuid(), Start = new(40, 10), End = new(60, 30) }; var crossingShape = firstShape with { Id = Guid.NewGuid(), Start = new(65, 10), End = new(85, 30) };
        selectionSurface.Document.Annotations.AddRange([firstShape, secondShape, crossingShape]);
        selectionSurface.SelectRegion(new(5, 5), new(70, 35));
        check(selectionSurface.Selection.Count == 2 && !selectionSurface.Selection.Contains(crossingShape.Id), "Select marquee left-to-right selects enclosed single or multiple objects");
        selectionSurface.SelectRegion(new(70, 35), new(5, 5));
        check(selectionSurface.Selection.Count == 3 && selectionSurface.Selection.Contains(crossingShape.Id), "Select marquee right-to-left includes crossing objects");
        selectionSurface.SelectRegion(new(5, 5), new(35, 35)); selectionSurface.SelectRegion(new(35, 5), new(62, 35), additive: true);
        check(selectionSurface.Selection.Count == 2, "additive Select marquee retains previous objects");
        selectionSurface.DeleteSelection(); check(selectionSurface.Document.Annotations.Single().Id == crossingShape.Id, "Delete removes the selected marquee batch without erasing unrelated objects");
        selectionSurface.History.Undo(); check(selectionSurface.Document.Annotations.Count == 3, "one Undo restores a multi-object selection deletion");
        selectionSurface.SelectRegion(new(90, 60), new(95, 70)); check(selectionSurface.Selection.Count == 0, "empty selection region clears previous selection");
        var pickSurface = new EditorSurface(image) { Tool = EditorTool.Dimension };
        var primaryPicker = new ColorPicker("#EF4444", "Primary", p => pickSurface.BeginColorPick(c => p.Value = c)); primaryPicker.Changed += v => pickSurface.Color = v;
        var backgroundPicker = new ColorPicker("#FFFFFF", "Background", p => pickSurface.BeginColorPick(c => p.Value = c)); backgroundPicker.Changed += v => pickSurface.TextBackgroundColor = v;
        var pickButton = (Button)typeof(ColorPicker).GetField("pick", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(backgroundPicker)!;
        pickButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); var picked = pickSurface.SampleColor(new(5, 5)); pickSurface.PickColorAt(new(5, 5));
        check(backgroundPicker.Value == picked && pickSurface.TextBackgroundColor == picked && primaryPicker.Value == "#EF4444" && pickSurface.Color == "#EF4444", "color picker sampling updates its own swatch/HEX and destination rather than the primary color");
        check(pickSurface.Tool == EditorTool.Dimension && !pickSurface.IsPickingColor, "color picking is transient and retains the active drawing tool");
        var beforeCancel = backgroundPicker.Value; pickButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); pickSurface.CancelGesture(); pickSurface.PickColorAt(new(5, 5));
        check(!pickSurface.IsPickingColor && backgroundPicker.Value == beforeCancel, "canceling a color pick cannot apply a stale target callback");
        var distance = new Annotation(Guid.NewGuid(), AnnotationKind.Distance, new(100, 100), new(300, 100), TextBackground: true, TextPadding: 5);
        DrawingGroup LabelDrawing(Annotation annotation, string label)
        {
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen()) typeof(AnnotationRenderer).GetMethod("DrawLabel", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [dc, label, new Point(200, 80), annotation, Brushes.Black]);
            return visual.Drawing;
        }
        var measureDocument = new EditorDocument(400, 200);
        foreach (var scale in new[] { 1d, 17.5 })
        {
            measureDocument.UnitsPerPixel = scale; measureDocument.Unit = scale == 1 ? "px" : "mm";
            var drawing = LabelDrawing(distance, AnnotationRenderer.MeasurementLabel(distance, measureDocument));
            var background = drawing.Children.OfType<GeometryDrawing>().Single().Bounds;
            check(Math.Abs(background.X + background.Width / 2 - 200) < .001 && Math.Abs(background.Y + background.Height / 2 - 80) < .001, "measurement label stays centered as calibration changes text width");
        }
        check(!LabelDrawing(distance with { TextBackground = false }, "3500 mm").Children.OfType<GeometryDrawing>().Any(), "disabled dimension background draws no white rectangle");
        var formatted = distance with { MeasureDecimals = 3, MeasureShowUnit = false, MeasurePrefix = "≈ ", MeasureSuffix = " checked", Italic = true, Underline = true };
        var flipped = formatted with { MirrorHorizontal = true, MirrorVertical = true, Rotation = 45, TextWidth = 160 };
        foreach (var angle in new[] { 0d, 45, 90 })
        foreach (var mirrored in new[] { false, true })
        {
            var rectangle = new Annotation(Guid.NewGuid(), AnnotationKind.Rectangle, new(20, 30), new(140, 100), Rotation: angle, MirrorHorizontal: mirrored);
            var handles = EditorSurface.SelectionHandles(rectangle); check(handles.Count == 4, "rectangle exposes four corners at rotation " + angle + " mirror " + mirrored);
            for (var corner = 0; corner < 4; corner++)
            {
                var b = rectangle.Bounds; var x = corner is 0 or 3 ? b.X - 20 : b.Right + 20; var y = corner is 0 or 1 ? b.Y - 15 : b.Bottom + 15;
                var destination = AnnotationRenderer.ObjectTransform(rectangle).Transform(new Point(x, y));
                var changed = EditorSurface.ResizeCorner(rectangle, corner, new(destination.X, destination.Y)); var after = EditorSurface.SelectionHandles(changed);
                check(GeometryMath.Distance(after[corner], new(destination.X, destination.Y)) < .001 && GeometryMath.Distance(after[(corner + 2) % 4], handles[(corner + 2) % 4]) < .001, "dragging corner " + corner + " moves that corner and holds its opposite for rotated/mirrored rectangles");
            }
        }
        var triangle = new Annotation(Guid.NewGuid(), AnnotationKind.Triangle, new(20, 30), new(140, 100), Rotation: 45, MirrorVertical: true);
        var triangleHandles = EditorSurface.SelectionHandles(triangle); var editedTriangle = EditorSurface.MoveVertex(triangle, 1, new(160, 140)); var newTriangle = EditorSurface.SelectionHandles(editedTriangle);
        check(triangleHandles.Count == 3 && newTriangle[1] == new PixelPoint(160, 140) && GeometryMath.Distance(newTriangle[0], triangleHandles[0]) < .001 && GeometryMath.Distance(newTriangle[2], triangleHandles[2]) < .001, "triangle exposes three editable vertices and preserves the untouched transformed vertices");
        check(EditorSurface.SelectionHandles(distance).Count == 2, "dimension endpoints are editable handles instead of one bounding-box corner");
        var cornerDocument = new EditorDocument(200, 200); cornerDocument.Annotations.Add(triangle); var cornerHistory = new CommandHistory(); cornerHistory.Execute(new ChangeAnnotationCommand(cornerDocument, triangle, editedTriangle)); cornerHistory.Undo();
        check(cornerDocument.Annotations.Single() == triangle, "vertex drag undo restores original shape, rotation and mirror");
        var transform = AnnotationRenderer.ObjectTransform(flipped); var pivot = new Point(200, 100); var fixedPivot = transform.Transform(pivot);
        check((fixedPivot - pivot).Length < .001 && (transform.Inverse!.Transform(transform.Transform(new Point(110, 105))) - new Point(110, 105)).Length < .001, "combined mirror and rotation keep the pivot fixed and invert for selection");
        foreach (var kind in new[] { AnnotationKind.Text, AnnotationKind.Note, AnnotationKind.Stamp, AnnotationKind.Symbol, AnnotationKind.Number, AnnotationKind.Callout, AnnotationKind.Dimension })
        {
            double InkCenter(TextAlign alignment)
            {
                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen()) AnnotationRenderer.Draw(dc, new Annotation(Guid.NewGuid(), kind, new(150, 100), new(280, 140), Color: "#38BDF8", Text: "A", FontSize: 18, TextColor: "#000000", TextWidth: 140, TextAlign: alignment, Variant: "grid"));
                var bitmap = new RenderTargetBitmap(450, 220, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); var data = new byte[450 * 220 * 4]; bitmap.CopyPixels(data, 450 * 4, 0);
                var positions = Enumerable.Range(0, 450 * 220).Where(i => data[i * 4 + 3] > 100 && data[i * 4] < 20 && data[i * 4 + 1] < 20 && data[i * 4 + 2] < 20).Select(i => i % 450).ToArray();
                return positions.Average();
            }
            var left = InkCenter(TextAlign.Left); var center = InkCenter(TextAlign.Center); var right = InkCenter(TextAlign.Right);
            check(left < center - 20 && center < right - 20, "rendered text alignment visibly moves single-line " + kind + " labels within their frame");
        }
        check(AnnotationRenderer.MeasurementLabel(formatted, measureDocument) == "≈ " + (3500d).ToString("F3", System.Globalization.CultureInfo.CurrentCulture) + " checked", "automatic measurement format supports decimal precision, hidden units and affixes");
        check(AnnotationRenderer.MeasurementLabel(formatted with { Kind = AnnotationKind.Dimension, Text = "manual 3500 mm" }, measureDocument) == "≈ manual 3500 mm checked", "manual dimension label remains editable and is not recalculated during calibration");
        var editor = new EditorWindow(image, "#EF4444", new ClipboardService(), new ExportService(), new AnnotationRenderer(), System.IO.Path.GetTempPath());
        object Field(string name) => typeof(EditorWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
        var categories = (Dictionary<string, Button>)Field("categoryButtons"); var surface = (EditorSurface)Field("surface");
        var categoryPanel = (Panel)categories["edit"].Parent;
        ReorderButtons.Move(categoryPanel, categories["symbols"], categories["edit"]);
        check(categoryPanel.Children[0] == categories["symbols"] && categories.Values.All(b => b.ContextMenu?.Items.Count == 2), "category buttons support moving and an accessible reorder menu");
        var layoutPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SnapMark-layout-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var custom = new EditorLayoutStore(layoutPath) { ToolWidth = 430, ToolHeight = 500 }; custom.Orders["categories"] = ["symbols", "draw", "obsolete", "symbols"]; custom.Orders["draw"] = ["Circle", "Line"]; custom.Save();
            var loaded = EditorLayoutStore.Load(layoutPath);
            check(loaded.ToolWidth == 430 && loaded.ToolHeight == 500 && loaded.Ordered("categories", ToolCatalog.Categories.Select(c => c.Id)).Count() == 7, "layout restores panel sizes and keeps new categories without duplicate or obsolete entries");
            var arranged = new EditorWindow(image, "#EF4444", new ClipboardService(), new ExportService(), new AnnotationRenderer(), System.IO.Path.GetTempPath(), layout: loaded);
            var buttons = (Dictionary<string, Button>)typeof(EditorWindow).GetField("categoryButtons", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(arranged)!;
            check(((Panel)buttons["symbols"].Parent).Children[0] == buttons["symbols"], "saved category order is applied to a new editor");
            buttons["draw"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var tools = (WrapPanel)typeof(EditorWindow).GetField("toolList", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(arranged)!;
            check((string)((Button)tools.Children[0]).Tag == "Circle" && (string)((Button)tools.Children[1]).Tag == "Line", "saved subtool order preserves tool identity and click selection");
            var listScroll = (ScrollViewer)tools.Parent; var toolStack = (StackPanel)listScroll.Parent;
            check(listScroll.Height == 500 && toolStack.Children.OfType<System.Windows.Controls.Primitives.Thumb>().Any(), "tool list restores its expanded height and exposes a resize grip");
        }
        finally { if (System.IO.File.Exists(layoutPath)) System.IO.File.Delete(layoutPath); }
        check(categories.Count == 7, "editor constructs all seven direct category buttons");
        categories["measure"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(surface.Tool == EditorTool.Dimension, "category click immediately selects its default tool");
        typeof(EditorWindow).GetMethod("SetTool", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, [EditorTool.Distance]);
        categories["label"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); categories["measure"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(surface.Tool == EditorTool.Distance, "switching categories remembers the last subtool without resetting hotkey choice");
        categories["draw"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(((Dictionary<EditorTool, Button>)Field("toolButtons")).Count == ToolCatalog.Tools.Count(t => t.CategoryId == "draw"), "category buttons populate every drawing subtool");
        foreach (var category in categories.Values) category.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(surface.Tool == EditorTool.Symbol, "every main category is usable without a dropdown");
        var symbolButtons = (Dictionary<string, Button>)Field("symbolButtons");
        check(symbolButtons.Keys.Order().SequenceEqual(ToolCatalog.Symbols.Order()) && ((WrapPanel)Field("toolList")).Children.Count == ToolCatalog.Symbols.Count, "Symbols Tools exposes every drawing variant directly instead of one generic button");
        foreach (var variant in ToolCatalog.Symbols) { symbolButtons[variant].RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); check(surface.SymbolId == variant && surface.Tool == EditorTool.Symbol, "direct symbol button selects " + variant); }
        categories["draw"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); categories["symbols"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(surface.SymbolId == "issue" && ((Dictionary<string, Button>)Field("symbolButtons"))["issue"].Foreground == Brushes.White, "returning to Symbols preserves and highlights the selected variant");
        var symbol = new Annotation(Guid.NewGuid(), AnnotationKind.Symbol, new(10, 10), new(50, 50), Text: "RFI-3", FontSize: 32, Variant: "grid"); surface.Document.Annotations.Add(symbol); surface.SelectObject(symbol.Id);
        var inspector = (ObjectPropertiesPanel)Field("properties"); check(inspector.Visibility == Visibility.Visible, "Select reveals properties for a symbol from another category");
        var content = (StackPanel)inspector.Content; var fontSize = content.Children.OfType<TextBox>().First(x => (string?)x.Tag == "Font size (px)");
        check(fontSize.Text == "32", "symbol inspector reads actual selected font size");
        fontSize.Text = "48"; fontSize.RaiseEvent(new System.Windows.Input.KeyboardFocusChangedEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, 0, fontSize, null) { RoutedEvent = System.Windows.Input.Keyboard.LostKeyboardFocusEvent });
        check(surface.Document.Annotations.Single().FontSize == 48 && surface.Document.Annotations.Single().Text == "RFI-3", "symbol inspector font edit immediately changes only font size");
        surface.History.Undo(); check(surface.Document.Annotations.Single().FontSize == 32, "undo refreshes selected symbol inspector without applying stale values");
        Button TransformButton(string caption) => ((StackPanel)inspector.Content).Children.OfType<WrapPanel>().SelectMany(p => p.Children.OfType<Button>()).First(b => (string?)b.Content == caption);
        TransformButton("Mirror ↔ / ซ้าย–ขวา").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(surface.Document.Annotations.Single().MirrorHorizontal, "selected object mirror action updates the document through undo history");
        TransformButton("90°").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        check(surface.Document.Annotations.Single().Rotation == 90 && ((StackPanel)inspector.Content).Children.OfType<TextBox>().Single(t => (string?)t.Tag == "Rotation (degrees)").Text == "90", "rotation guide sets the actual angle and refreshes the custom-degree field");
        surface.History.Undo(); surface.History.Undo(); check(!surface.Document.Annotations.Single().MirrorHorizontal && surface.Document.Annotations.Single().Rotation == 0, "undo independently restores selected object rotation and mirror");
        var historyFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SnapMark-inline-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var history = new HistoryService(historyFolder, new ExportService()); history.Add(image, 10);
            var measurementProject = System.IO.Path.Combine(historyFolder, "measurement.snapmark");
            var formatDocument = new EditorDocument(100, 80); formatDocument.Annotations.Add(flipped); ProjectStorage.Save(measurementProject, image, formatDocument);
            check(ProjectStorage.Load(measurementProject).Document.Annotations.Single() == flipped, "measurement text format, mirror, rotation and text width survive editable project save and reopen");
            var entry = history.Entries().Single();
            var bytes = System.IO.File.ReadAllBytes(entry.Path); var modified = System.IO.File.GetLastWriteTimeUtc(entry.Path);
            history.Rename(entry, "  RFI ห้องประชุม ชั้น 3  ");
            var named = new HistoryService(historyFolder, new ExportService()).Entries().Single();
            check(named.Name == "RFI ห้องประชุม ชั้น 3" && named.DisplayName == named.Name && named.Path == entry.Path && System.IO.File.ReadAllBytes(entry.Path).SequenceEqual(bytes) && System.IO.File.GetLastWriteTimeUtc(entry.Path) == modified, "history rename persists Unicode names without changing image bytes, date or path");
            var historySearch = new HistoryPickerWindow([named], new ClipboardService());
            var historyRoot = (DockPanel)historySearch.Content; var search = historyRoot.Children.OfType<StackPanel>().First().Children.OfType<TextBox>().Single(); var results = historyRoot.Children.OfType<ListBox>().Single();
            search.Text = "ห้องประชุม"; check(((ListBoxItem)results.Items[0]).Visibility == Visibility.Visible, "history search finds a user-defined image name without OCR indexing");
            search.Text = "not-present"; check(((ListBoxItem)results.Items[0]).Visibility == Visibility.Collapsed, "history search still filters unrelated names");
            history.Rename(entry, ""); check(history.Entries().Single().DisplayName == entry.DisplayName, "clearing a custom history name restores its original date label");
            var inlineEditor = new EditorWindow(image, "#EF4444", new ClipboardService(), new ExportService(), new AnnotationRenderer(), historyFolder, historyEntries: history.Entries, recycleHistory: entry => history.RecycleAsync(entry));
            var panel = (EditorHistoryPanel)typeof(EditorWindow).GetField("inlineHistory", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(inlineEditor)!;
            var current = (EditorSurface)typeof(EditorWindow).GetField("surface", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(inlineEditor)!;
            current.Document.Annotations.Add(symbol);
            var thumbnail = EditorHistoryPanel.TryThumbnail(entry.Path);
            check(thumbnail is { IsFrozen: true } && thumbnail.PixelWidth <= 480, "history thumbnail is bounded and detached from its file");
            Pump(() => inlineEditor.RefreshInlineHistory());
            Pump(() => panel.PreviewAsync(entry));
            check(panel.IsPreviewing && panel.SelectedImage?.PixelWidth == 100, "history preview loads in the editor viewport");
            check(current.Document.Annotations.Single() == symbol && !current.History.CanUndo && ReferenceEquals(current.Image, image), "preview leaves the current source, annotations and undo stack intact");
            panel.Back(); check(!panel.IsPreviewing && panel.SelectedImage is null, "back releases preview and restores the existing edit");
            Pump(() => panel.PreviewAsync(entry)); panel.InsertSelected();
            check(!panel.IsPreviewing && current.Document.Annotations.Count == 2 && current.History.CanUndo, "insert from preview returns to edit and adds an undoable image object");
            current.History.Undo(); check(current.Document.Annotations.Single() == symbol, "undo insertion retains the original markup");
            Pump(() => panel.PreviewAsync(entry with { Path = entry.Path + ".missing" }));
            check(panel.IsPreviewing && panel.SelectedImage is null, "missing history clears any previous preview and cannot insert stale content");
            Pump(async () => { var pending = panel.PreviewAsync(entry); panel.Back(); await pending; });
            check(!panel.IsPreviewing && panel.SelectedImage is null, "back invalidates an in-flight preview before it can reopen");
            var tiles = (StackPanel)typeof(EditorHistoryPanel).GetField("thumbnails", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!;
            var tile = tiles.Children.OfType<Grid>().Single(); var thumbnailImage = ((StackPanel)((Button)tile.Children[0]).Content).Children.OfType<Image>().Single();
            var initialSize = thumbnailImage.Height;
            panel.ResizeHistory(320); panel.Measure(new Size(1100, 320)); panel.Arrange(new Rect(0, 0, 1100, 320));
            check(panel.Height == 320 && thumbnailImage.Height > initialSize && tile.Width > 100, "resizing the history divider scales thumbnails and their aspect-ratio layout");
            panel.ResizeHistory(-100); check(panel.Height == panel.MinHeight, "history resizing preserves accessible minimum height");
            check(tile.Children.OfType<Button>().Any(b => b.Content as string == "×" && b.IsEnabled), "each history thumbnail exposes its own Recycle Bin button");
            System.IO.File.WriteAllText(entry.Path + ".ocr.json", "{}");
            IReadOnlyList<string>? queued = null;
            Pump(() => history.RecycleAsync(entry, paths => { queued = paths; return Task.CompletedTask; }));
            check(queued?.Count == 3 && queued.Contains(entry.Path) && queued.Contains(entry.Path + ".json") && queued.Contains(entry.Path + ".ocr.json"), "history recycling includes image, metadata and OCR cache");
            var denied = false;
            try { history.RecycleAsync(entry with { Path = System.IO.Path.Combine(historyFolder, "..", System.IO.Path.GetFileName(entry.Path)) }, _ => Task.CompletedTask).GetAwaiter().GetResult(); } catch (ArgumentException) { denied = true; }
            check(denied && System.IO.File.Exists(entry.Path), "history recycling rejects paths outside the configured history folder");
            using (var failingPanel = new EditorHistoryPanel(history.Entries, _ => { }, _ => Task.FromException(new System.IO.IOException("Test recycle failure"))))
            {
                Pump(() => failingPanel.PreviewAsync(entry)); Pump(() => failingPanel.RecycleAsync());
                check(failingPanel.IsEnabled && failingPanel.IsPreviewing && failingPanel.SelectedImage is not null && System.IO.File.Exists(entry.Path), "recycle failures preserve the image and restore panel interaction");
            }
            Pump(() => panel.PreviewAsync(entry)); Pump(() => panel.RecycleAsync());
            check(!System.IO.File.Exists(entry.Path) && !System.IO.File.Exists(entry.Path + ".json") && !System.IO.File.Exists(entry.Path + ".ocr.json") && tiles.Children.Count == 0, "real Windows recycling removes the capture bundle and refreshes history thumbnails");
            check(!panel.IsPreviewing && current.Document.Annotations.Single() == symbol && ReferenceEquals(current.Image, image), "recycling the preview preserves the editable canvas and its original markup");
            var sid = System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value;
            var bin = System.IO.Path.Combine(System.IO.Path.GetPathRoot(entry.Path)!, "$Recycle.Bin", sid);
            var recordedPaths = System.IO.Directory.EnumerateFiles(bin, "$I*").Select(path => { try { return System.Text.Encoding.Unicode.GetString(System.IO.File.ReadAllBytes(path)); } catch { return ""; } }).ToArray();
            check(new[] { entry.Path, entry.Path + ".json", entry.Path + ".ocr.json" }.All(path => recordedPaths.Any(record => record.Contains(path + "\0", StringComparison.OrdinalIgnoreCase))), "Windows Recycle Bin contains restorable records for all recycled history files");
            Pump(async () => { var pending = panel.PreviewAsync(entry); panel.Dispose(); await pending; });
            check(!panel.IsPreviewing && panel.SelectedImage is null, "closing releases preview and ignores pending image loads");
        }
        finally { if (System.IO.Directory.Exists(historyFolder)) System.IO.Directory.Delete(historyFolder, true); }
        var desktop = NativeDesktop.Monitors.First(); var region = new PixelRect(desktop.X + 100, desktop.Y + 100, 320, 240);
        Pump(async () =>
        {
            var bounds = NativeDesktop.VirtualBounds;
            var selection = RegionOverlay.SelectAsync(NativeDesktop.Snapshot(bounds), bounds, allowFreeform: false, confirmRegion: true, initialBounds: region);
            var overlays = app.Windows.OfType<RegionOverlay>().ToArray();
            check(!selection.IsCompleted && overlays.Length > 0, "recording region remains open for adjustment before explicit confirmation");
            var next = ((Grid)overlays[0].Content).Children.OfType<Button>().Single();
            check(next.Visibility == Visibility.Visible, "adjustable recording frame offers an explicit Next button");
            next.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var chosen = await selection;
            check(chosen?.Bounds == region && !app.Windows.OfType<RegionOverlay>().Any(), "confirming recording area returns final physical bounds and removes every overlay");
        });
        using (var frame = new RecordingFrame())
        {
            frame.Show(region); var windows = app.Windows.OfType<Window>().Where(w => w.Title == "SnapMark · Recording boundary").ToArray();
            check(windows.Length == 4 && windows.All(w => w.Topmost && !w.IsHitTestVisible && !w.ShowActivated), "recording boundary opens four non-activating click-through topmost windows");
            check(windows.Select(w => NativeDesktop.WindowBounds(new WindowInteropHelper(w).Handle)).All(b => b.Intersect(region).Width * b.Intersect(region).Height == 0), "actual recording boundary HWNDs stay outside captured physical pixels");
        }
        check(!app.Windows.OfType<Window>().Any(w => w.Title == "SnapMark · Recording boundary"), "recording boundary windows are removed on completion");
        var recordingEditor = new EditorWindow(image, "#EF4444", new ClipboardService(), new ExportService(), new AnnotationRenderer(), System.IO.Path.GetTempPath());
        var spare = new Window { Width = 100, Height = 100, ShowActivated = false };
        recordingEditor.Show(); spare.Show();
        using (var visibility = new CaptureWindowVisibility([recordingEditor, spare]))
        {
            check(!recordingEditor.IsVisible && !spare.IsVisible, "recording region selection temporarily hides existing windows");
            spare.Close(); visibility.Restore();
            check(recordingEditor.IsVisible && recordingEditor.IsEnabled, "recording setup restores the original editor before recording begins");
            recordingEditor.Hide(); visibility.Restore();
            check(!recordingEditor.IsVisible, "stopping or disposing cannot reopen a window deliberately hidden after restore");
            recordingEditor.Show();
        }
        recordingEditor.Hide(); CaptureWindowVisibility.ReturnToEditor(recordingEditor, recordingEditor);
        check(recordingEditor.IsVisible && recordingEditor.IsEnabled, "recording completion explicitly returns to the existing editor");
        var returnMain = new Window { Width = 100, Height = 100, ShowActivated = false };
        CaptureWindowVisibility.ReturnToEditor(null, returnMain);
        check(returnMain.IsVisible, "recording completion opens the main window if no editor exists"); returnMain.Close();
        if (RecordingService.FindEncoder() is not null)
        {
            var videoPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SnapMark-editor-recording-" + Guid.NewGuid().ToString("N") + ".mp4");
            var service = new RecordingService(); var control = new RecordingWindow(service);
            var recordingSurface = (EditorSurface)typeof(EditorWindow).GetField("surface", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(recordingEditor)!;
            var recordingCategories = (Dictionary<string, Button>)typeof(EditorWindow).GetField("categoryButtons", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(recordingEditor)!;
            Pump(async () =>
            {
                try
                {
                    control.Show(); await control.Begin(new(region, videoPath, 10, false));
                    check(service.IsRecording && recordingEditor.IsVisible && recordingEditor.IsEnabled, "real video recording keeps the original editor visible and enabled");
                    recordingCategories["symbols"].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    check(recordingSurface.Tool == EditorTool.Symbol, "original editor category buttons work while the encoder is recording");
                    recordingEditor.InsertImage(image);
                    check(recordingSurface.Document.Annotations.Count == 1 && service.IsRecording, "original editor can modify its document without interrupting recording");
                    recordingSurface.History.Undo();
                    check(recordingSurface.Document.Annotations.Count == 0 && service.IsRecording, "undo remains available while video recording continues");
                }
                finally { await control.StopRecording(); }
            });
            check(System.IO.File.Exists(videoPath) && new System.IO.FileInfo(videoPath).Length > 100 && recordingEditor.IsVisible, "stop finalizes MP4 and leaves the same editor available");
            System.IO.File.Delete(videoPath);
        }
        typeof(EditorWindow).GetField("allowClose", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(recordingEditor, true);
        recordingEditor.Close();
    }
    private static void Pump(Func<Task> start)
    {
        var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        var previous = System.Threading.SynchronizationContext.Current;
        System.Threading.SynchronizationContext.SetSynchronizationContext(new System.Windows.Threading.DispatcherSynchronizationContext(dispatcher));
        try
        {
            var task = start();
            if (!task.IsCompleted)
            {
                var frame = new System.Windows.Threading.DispatcherFrame();
                _ = task.ContinueWith(_ => dispatcher.BeginInvoke(new Action(() => frame.Continue = false)), TaskScheduler.Default);
                System.Windows.Threading.Dispatcher.PushFrame(frame);
            }
            task.GetAwaiter().GetResult();
        }
        finally { System.Threading.SynchronizationContext.SetSynchronizationContext(previous); }
    }
}
