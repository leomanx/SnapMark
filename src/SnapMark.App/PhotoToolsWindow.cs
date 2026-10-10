using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SnapMark.App;

public sealed class PhotoToolsWindow : Window
{
    private readonly ObservableCollection<string> photos = [];
    private readonly ListBox list;
    private readonly PhotoPreviewPane preview = new();
    private readonly ComboBox previewMode = new() { ItemsSource = new[] { "Auto · single photo / multi layout", "Photo detail", "Print layout" }, SelectedIndex = 0 };
    private readonly TextBlock pageLabel = new() { Margin = new Thickness(8) };
    private int previewPage;
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8) };
    private readonly TextBox width = new() { Text = "0", Width = 88 }, height = new() { Text = "0", Width = 88 };
    private readonly CheckBox ratio = new() { Content = "Keep ratio", IsChecked = true }, upscale = new() { Content = "Upscale" };
    private readonly ComboBox tone = new() { ItemsSource = Enum.GetValues<PhotoTone>(), SelectedItem = PhotoTone.Original };
    private readonly ComboBox format = new() { ItemsSource = new[] { "PNG", "JPEG", "BMP", "TIFF", "GIF" }, SelectedIndex = 0 };
    private readonly TextBox quality = new() { Text = "90", Width = 70 }, delay = new() { Text = "500", Width = 80 };
    private readonly CheckBox loop = new() { Content = "Loop GIF", IsChecked = true };
    private readonly ComboBox paper = new() { ItemsSource = PhotoPaper.Sizes, SelectedIndex = 4, Width = 115 };
    private readonly ComboBox orientation = new() { ItemsSource = new[] { "Portrait", "Landscape" }, SelectedIndex = 0 };
    private readonly ComboBox units = new() { ItemsSource = Enum.GetValues<PrintUnit>(), SelectedItem = PrintUnit.Millimeter };
    private readonly ComboBox captionMode = new() { ItemsSource = Enum.GetValues<PhotoCaption>(), SelectedItem = PhotoCaption.Below };
    private readonly TextBox nameInset = new() { Text = "3", Width = 70 }, nameSize = new() { Text = "10", Width = 70 };
    private readonly TextBlock paperSize = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(5) };
    private readonly ComboBox columns = new() { ItemsSource = Enumerable.Range(1, 6), SelectedItem = 2, Width = 70 }, rows = new() { ItemsSource = Enumerable.Range(1, 8), SelectedItem = 2, Width = 70 };
    private readonly TextBox margin = new() { Text = "7", Width = 70 };
    private readonly List<Button> actions = [];
    private CancellationTokenSource? work;
    private readonly Func<string>? outputFolder;
    public PhotoToolsWindow(Func<string>? outputFolder = null)
    {
        this.outputFolder = outputFolder; Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Photo Tools"; Width = 1380; Height = 880; MinWidth = 1100; MinHeight = 650; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new DockPanel { Margin = new Thickness(14) }; Content = root;
        DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
        var title = new TextBlock { Text = "PHOTO TOOLS", FontSize = 22, Margin = new Thickness(6, 0, 6, 12) }; DockPanel.SetDock(title, Dock.Top); root.Children.Add(title);
        var workspace = new Grid(); workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220), MinWidth = 160 }); workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) }); workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(350), MinWidth = 280 }); workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) }); workspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 300 }); root.Children.Add(workspace);
        var left = new DockPanel { Margin = new Thickness(0, 0, 6, 0) }; workspace.Children.Add(left);
        foreach (var index in new[] { 1, 3 }) { var splitter = new GridSplitter { Width = 6, HorizontalAlignment = HorizontalAlignment.Stretch, Background = (Brush)FindResource("ButtonBrush"), ResizeBehavior = GridResizeBehavior.PreviousAndNext, ResizeDirection = GridResizeDirection.Columns }; Grid.SetColumn(splitter, index); workspace.Children.Add(splitter); }
        var commands = new WrapPanel(); DockPanel.SetDock(commands, Dock.Top); left.Children.Add(commands);
        Button Command(string icon, string label, Action click, Panel target) { var b = new Button { Padding = new Thickness(8) }; ToolIcon.SetButton(b, icon, label, label); b.Click += (_, _) => click(); target.Children.Add(b); actions.Add(b); return b; }
        Command("open", "Add", Add, commands);
        Command("open", "Add folder", AddFolder, commands);
        list = new ListBox { ItemsSource = photos, SelectionMode = SelectionMode.Extended, AllowDrop = true };
        Command("minus", "Remove", () => { foreach (var item in list.SelectedItems.Cast<string>().ToArray()) photos.Remove(item); UpdateStatus(); }, commands);
        var up = Command("forward", "Up", () => Move(-1), commands); ToolIcon.SetButton(up, "forward", null, "Move selected photo up");
        var down = Command("backward", "Down", () => Move(1), commands); ToolIcon.SetButton(down, "backward", null, "Move selected photo down");
        var itemText = new FrameworkElementFactory(typeof(TextBlock)); itemText.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding { Converter = new PhotoNameConverter() }); itemText.SetBinding(ToolTipProperty, new System.Windows.Data.Binding()); itemText.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis); list.ItemTemplate = new DataTemplate { VisualTree = itemText };
        var hint = new TextBlock { Text = "ลากไฟล์มาเพิ่ม · Ctrl/Shift เลือกหลายภาพ\nคำสั่งใช้ภาพที่เลือก; ไม่เลือก = ทุกภาพ\nGIF / Print เรียงตามรายการนี้", FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(5, 5, 5, 10) }; DockPanel.SetDock(hint, Dock.Top); left.Children.Add(hint);
        left.Children.Add(list);
        list.SelectionChanged += (_, _) => { previewPage = 0; UpdateStatus(); QueuePreview(); };
        list.Drop += (_, e) => { if (work is null && e.Data.GetData(DataFormats.FileDrop) is string[] paths) AddPaths(paths); };
        var options = new StackPanel(); var optionsScroll = new ScrollViewer { Content = options, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; Grid.SetColumn(optionsScroll, 2); workspace.Children.Add(optionsScroll);
        var previewDock = new DockPanel { Margin = new Thickness(8, 0, 0, 0) }; Grid.SetColumn(previewDock, 4); workspace.Children.Add(previewDock);
        var modeBar = new WrapPanel(); DockPanel.SetDock(modeBar, Dock.Top); previewDock.Children.Add(modeBar); modeBar.Children.Add(previewMode);
        var previous = Command("backward", "Previous", () => { previewPage = Math.Max(0, previewPage - 1); QueuePreview(); }, modeBar);
        var next = Command("forward", "Next", () => { previewPage++; QueuePreview(); }, modeBar); modeBar.Children.Add(pageLabel); previewDock.Children.Add(preview);
        void Heading(string name) => options.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("AccentBrush"), Margin = new Thickness(5, 12, 5, 4) });
        void Row(params FrameworkElement[] controls) { var row = new WrapPanel(); foreach (var c in controls) { c.Margin = new Thickness(5); c.VerticalAlignment = VerticalAlignment.Center; row.Children.Add(c); } options.Children.Add(row); }
        TextBlock Label(string text) => new() { Text = text };
        Heading("RESIZE · single / batch"); Row(Label("W px"), width, Label("H px"), height, ratio, upscale);
        Row(Label("0 = original / automatic · fits inside W × H"));
        Heading("COLOR / CONVERT"); Row(Label("Tone"), tone, Label("Format"), format, Label("JPEG %"), quality);
        Row(Label("Grey = grayscale · Chrome = high-contrast mono"));
        var outputs = new WrapPanel(); options.Children.Add(outputs);
        Command("image", "Preview", () => _ = Preview(), outputs);
        Command("export", "Export set", () => _ = Export(), outputs);
        Heading("ANIMATED GIF · selected photos"); Row(Label("ms / frame"), delay, loop);
        var gifs = new WrapPanel(); options.Children.Add(gifs); Command("video", "Combine GIF", () => _ = Gif(), gifs);
        Heading("PRINT LAYOUT · single / batch"); Row(paper, orientation); options.Children.Add(paperSize); Row(Label("Columns"), columns, Label("Rows"), rows); Row(Label("Units"), units, Label("Margin"), margin);
        units.ToolTip = "mm / inches / feet-inches. Examples: 1/8\" or 1'-2 1/2\"; a bare foot-inch value means inches.";
        Heading("FILE NAME / ชื่อไฟล์"); Row(captionMode); Row(Label("Inset"), nameInset, Label("Font pt"), nameSize);
        Row(Label("Hidden / Below / Overlay top or bottom"));
        var print = new WrapPanel(); options.Children.Add(print); Command("grid", "Print preview", PrintPreview, print);
        var cancel = new Button { Content = "Cancel processing", IsEnabled = false }; options.Children.Add(cancel); cancel.Click += (_, _) => work?.Cancel();
        workChanged = busy => { foreach (var b in actions) b.IsEnabled = !busy; list.IsEnabled = !busy; options.IsEnabled = true; cancel.IsEnabled = busy; };
        Closing += (_, e) => { if (work is not null) { work.Cancel(); e.Cancel = true; status.Text = "Cancelling… wait until processing stops before closing."; } };
        foreach (var picker in new[] { tone, paper, orientation, columns, rows, captionMode, previewMode }) picker.SelectionChanged += (_, _) => QueuePreview();
        foreach (var text in new[] { width, height, margin, nameInset, nameSize }) text.TextChanged += (_, _) => QueuePreview();
        ratio.Click += (_, _) => QueuePreview(); upscale.Click += (_, _) => QueuePreview();
        var previousUnit = PrintUnit.Millimeter;
        units.SelectionChanged += (_, _) => { try { var newUnit = (PrintUnit)units.SelectedItem; var oldMargin = PrintUnits.ParseMm(margin.Text, previousUnit); var oldInset = PrintUnits.ParseMm(nameInset.Text, previousUnit); margin.Text = PrintUnits.FormatMm(oldMargin, newUnit); nameInset.Text = PrintUnits.FormatMm(oldInset, newUnit); previousUnit = newUnit; QueuePreview(); } catch (Exception ex) { status.Text = ex.Message; previousUnit = (PrintUnit)units.SelectedItem; } };
        previewTimer.Tick += async (_, _) => { previewTimer.Stop(); await Preview(); };
        Closed += (_, _) => { previewTimer.Stop(); previewRevision++; previewWork?.Cancel(); };
        UpdatePaperSize();
        UpdateStatus();
    }
    private sealed class PhotoNameConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => Path.GetFileName((string)value);
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }
    private readonly Action<bool> workChanged;
    private string[] Targets() => (list.SelectedItems.Count > 0 ? photos.Where(p => list.SelectedItems.Contains(p)) : photos).ToArray();
    private PhotoOptions Options() => new(int.Parse(width.Text), int.Parse(height.Text), ratio.IsChecked == true, upscale.IsChecked == true, (PhotoTone)tone.SelectedItem, int.Parse(quality.Text));
    private void Add() { var dialog = new Microsoft.Win32.OpenFileDialog { Filter = PhotoProcessing.Filter, Multiselect = true }; if (dialog.ShowDialog(this) == true) AddPaths(dialog.FileNames); }
    private void AddFolder() { var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Add photos from folder" }; if (dialog.ShowDialog(this) != true) return; try { AddPaths(PhotoProcessing.FolderPhotos(dialog.FolderName)); } catch (Exception ex) { status.Text = ex.Message; } }
    private void AddPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(File.Exists)) if (!photos.Contains(path, StringComparer.OrdinalIgnoreCase) && PhotoProcessing.IsPhoto(path)) photos.Add(path);
        UpdateStatus(); QueuePreview();
    }
    private void Move(int direction) { if (list.SelectedItems.Count != 1) return; var index = list.SelectedIndex; var target = index + direction; if (target >= 0 && target < photos.Count) { photos.Move(index, target); list.SelectedIndex = target; } }
    private void UpdateStatus() => status.Text = $"{photos.Count} photos · {Targets().Length} targets · Export saves new files · GIF conversion uses first frame of animated inputs";
    private int previewRevision;
    private readonly System.Windows.Threading.DispatcherTimer previewTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private CancellationTokenSource? previewWork;
    private void QueuePreview() { previewRevision++; previewWork?.Cancel(); previewTimer.Stop(); previewTimer.Start(); UpdatePaperSize(); }
    private void UpdatePaperSize() { var selected = (PhotoPaper)paper.SelectedItem; var unit = (PrintUnit)units.SelectedItem; paperSize.Text = $"{PrintUnits.FormatMm(orientation.SelectedIndex == 1 ? selected.HeightMm : selected.WidthMm, unit)} × {PrintUnits.FormatMm(orientation.SelectedIndex == 1 ? selected.WidthMm : selected.HeightMm, unit)} · {unit}"; }
    private PhotoPrintOptions PrintOptions()
    {
        var selected = (PhotoPaper)paper.SelectedItem; var landscape = orientation.SelectedIndex == 1; var unit = (PrintUnit)units.SelectedItem; var caption = (PhotoCaption)captionMode.SelectedItem;
        return new(PrintUnits.Dip(landscape ? selected.HeightMm : selected.WidthMm), PrintUnits.Dip(landscape ? selected.WidthMm : selected.HeightMm), (int)columns.SelectedItem, (int)rows.SelectedItem, PrintUnits.Dip(PrintUnits.ParseMm(margin.Text, unit)), caption != PhotoCaption.Hidden, caption, PrintUnits.Dip(PrintUnits.ParseMm(nameInset.Text, unit)), double.Parse(nameSize.Text) * 96 / 72);
    }
    private async Task Preview()
    {
        var revision = ++previewRevision; var targets = Targets(); if (targets.Length == 0) { preview.Clear(); pageLabel.Text = ""; return; }
        previewWork?.Cancel(); using var pending = new CancellationTokenSource(); previewWork = pending;
        try
        {
            var settings = Options(); var layout = PrintOptions(); var useLayout = previewMode.SelectedIndex == 2 || previewMode.SelectedIndex == 0 && targets.Length > 1;
            var perPage = useLayout ? layout.Columns * layout.Rows : 1; var pages = (targets.Length + perPage - 1) / perPage; previewPage = Math.Clamp(previewPage, 0, pages - 1); var pagePaths = targets.Skip(previewPage * perPage).Take(perPage).ToArray();
            var images = await Task.Run(() => pagePaths.Select(path => { pending.Token.ThrowIfCancellationRequested(); var image = PhotoProcessing.Transform(PhotoProcessing.Load(path), settings); if (useLayout) image = PhotoProcessing.Transform(image, new(1200,1200)); return (Path: path, Image: image); }).ToArray(), pending.Token);
            if (revision != previewRevision) return;
            pageLabel.Text = $"{previewPage + 1} / {pages}";
            if (useLayout) preview.ShowDocument(PhotoPrintLayout.CreateImages(images, layout), $"{paper.SelectedItem} · {layout.Columns} × {layout.Rows} · {targets.Length} photos");
            else preview.ShowImage(images[0].Image, $"{Path.GetFileName(images[0].Path)} · {images[0].Image.PixelWidth} × {images[0].Image.PixelHeight} px · Ctrl+wheel zoom");
            status.Text = $"{targets.Length} targets · preview updated";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (revision == previewRevision) status.Text = ex.Message; }
        finally { if (ReferenceEquals(previewWork, pending)) previewWork = null; }
    }
    private async Task Run(Func<CancellationToken, Task> task)
    {
        if (work is not null) return; work = new(); workChanged(true);
        try { await task(work.Token); } catch (OperationCanceledException) { status.Text = "Cancelled · completed exports remain saved."; } catch (Exception ex) { status.Text = ex.Message; }
        finally { work.Dispose(); work = null; workChanged(false); }
    }
    private async Task Export()
    {
        try
        {
            var paths = Targets(); if (paths.Length == 0) throw new ArgumentException("Add photos first."); var settings = Options(); _ = PhotoProcessing.OutputSize(1, 1, settings);
            var folder = new Microsoft.Win32.OpenFolderDialog { Title = "New photo files · output folder", InitialDirectory = outputFolder?.Invoke() }; if (folder.ShowDialog(this) != true) return;
            var extension = new[] { ".png", ".jpg", ".bmp", ".tiff", ".gif" }[format.SelectedIndex];
            await Run(async token =>
            {
                var completed = 0; var errors = new List<string>();
                foreach (var path in paths)
                {
                    token.ThrowIfCancellationRequested();
                    try { await Task.Run(() => { var image = PhotoProcessing.Transform(PhotoProcessing.Load(path), settings); var output = PhotoProcessing.NewPath(folder.FolderName, Path.GetFileNameWithoutExtension(path) + "_SnapMark", extension); PhotoProcessing.Save(image, output, settings.JpegQuality); }, token); completed++; }
                    catch (OperationCanceledException) { throw; } catch (Exception ex) { errors.Add(Path.GetFileName(path) + ": " + ex.Message); }
                    status.Text = $"Exported {completed}/{paths.Length} · {errors.Count} failed";
                }
                status.Text = $"Saved {completed} files → {folder.FolderName}" + (errors.Count > 0 ? "\n" + string.Join("\n", errors.Take(5)) : "");
            });
        }
        catch (Exception ex) { status.Text = ex.Message; }
    }
    private async Task Gif()
    {
        try
        {
            var paths = Targets(); var settings = Options(); var ms = int.Parse(delay.Text); var repeat = loop.IsChecked == true;
            if (paths.Length < 2) throw new ArgumentException("Select at least two photos for GIF.");
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Animated GIF|*.gif", FileName = "SnapMark-photos.gif", InitialDirectory = outputFolder?.Invoke() }; if (dialog.ShowDialog(this) != true) return;
            if (File.Exists(dialog.FileName)) throw new ArgumentException("Choose a new filename for GIF.");
            await Run(async token => { status.Text = "Building GIF…"; await PhotoProcessing.MakeGif(paths, dialog.FileName, settings, ms, repeat, token); status.Text = "Saved: " + dialog.FileName; });
        }
        catch (Exception ex) { status.Text = ex.Message; }
    }
    private void PrintPreview()
    {
        try
        {
            var paths = Targets(); if (paths.Length == 0) throw new ArgumentException("Add photos first.");
            var settings = Options(); var layout = PrintOptions();
            var doc = PhotoPrintLayout.Create(paths, settings, layout);
            var window = new Window { Title = "SnapMark · Print preview", Width = 960, Height = 820, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var panel = new DockPanel(); var print = new Button { Content = "Print… / Printer, copies & page range", Padding = new Thickness(12), Margin = new Thickness(8) }; DockPanel.SetDock(print, Dock.Top); panel.Children.Add(print);
            var viewer = new PhotoPreviewPane(); panel.Children.Add(viewer); window.Content = panel; viewer.ShowDocument(doc, $"{paper.SelectedItem} · {doc.Pages.Count} pages");
            print.Click += (_, _) => viewer.Print(); window.Show();
        }
        catch (Exception ex) { status.Text = ex.Message; }
    }
}
