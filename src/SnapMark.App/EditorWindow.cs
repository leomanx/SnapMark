using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Editor;
using SnapMark.Export;
using SnapMark.Core;

namespace SnapMark.App;
public sealed class EditorWindow : Window
{
    private readonly EditorSurface surface;
    private readonly ScrollViewer scroll;
    private readonly EditorHistoryPanel? inlineHistory;
    public Task RefreshInlineHistory() => inlineHistory?.RefreshAsync() ?? Task.CompletedTask;
    private readonly TextBlock status;
    private readonly IClipboardService clipboard;
    private readonly IExportService export;
    private readonly IAnnotationRenderer renderer;
    private readonly Dictionary<EditorTool, Button> toolButtons = [];
    private readonly Dictionary<string, Button> symbolButtons = [];
    private readonly Dictionary<string, Button> categoryButtons = [];
    private readonly Dictionary<string, EditorTool> categoryTools = [];
    private string activeCategory = "";
    private readonly EditorLayoutStore layout;
    private ColorPicker? pickingColor;
    private readonly WrapPanel toolList;
    private readonly FrameworkElement stampOptions, effectOptions;
    private readonly TextBlock toolHint;
    private readonly Button selectButton;
    private readonly ObjectPropertiesPanel properties;
    private readonly List<(FrameworkElement Panel, Func<EditorTool, bool> Show)> contextOptions = [];
    private static EditorTool lastTool = EditorTool.Arrow;
    private double zoom = 1;
    private bool dirty, panning;
    private Point panStart;
    private double panX, panY;
    private readonly RecoveryStore? recovery;
    private readonly Guid recoveryId;
    private readonly Func<string, string> nameFile;
    private readonly Func<string, string> outputFolder;
    private readonly System.Windows.Threading.DispatcherTimer autosaveTimer = new() { Interval = TimeSpan.FromSeconds(5) };
    private Task recoveryWrite = Task.CompletedTask;
    private int revision;
    private volatile int savedRevision;
    public Guid RecoveryId => recoveryId;
    private bool closingEditor, allowClose;
    private ComboBox personalStylePicker = null!;
    public void ApplyPersonalPreset(string name) { if (personalStylePicker.Items.OfType<UserStylePreset>().FirstOrDefault(p => p.Name == name) is { } preset) personalStylePicker.SelectedItem = preset; }
    public EditorWindow(BitmapSource image, string color, IClipboardService clipboard, IExportService export, IAnnotationRenderer renderer, string exportFolder, Func<Window, BitmapSource?>? pickHistory = null, EditorDocument? document = null, RecoveryStore? recovery = null, Guid? recoveryId = null, Func<string, string>? nameFile = null, PresetStore? presetStore = null, SymbolLibrary? symbolLibrary = null, Func<string, string>? outputFolder = null, PluginCatalog? plugins = null, Func<AppSettings>? teamSettings = null, Action? persistTeamSettings = null, Func<IReadOnlyList<HistoryEntry>>? historyEntries = null, Func<HistoryEntry, Task>? recycleHistory = null, Action<HistoryEntry, string>? renameHistory = null, EditorLayoutStore? layout = null)
    {
        this.layout = layout ?? new EditorLayoutStore();
        this.clipboard = clipboard; this.export = export; this.renderer = renderer;
        this.recovery = recovery; this.recoveryId = recoveryId ?? Guid.NewGuid();
        this.nameFile = nameFile ?? (kind => OutputNaming.Create("SnapMark_{date}_{time}", kind, DateTime.Now));
        this.outputFolder = outputFolder ?? (_ => exportFolder);
        Style = (Style)FindResource(typeof(Window));
        Title = "SnapMark · Editor"; Width = 1160; Height = 800; MinWidth = 900; MinHeight = 580; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        surface = new EditorSurface(image, document) { Color = color, RequestText = PromptText };
        surface.RequestCalibration = PromptCalibration;
        var root = new DockPanel(); Content = root;
        var toolbar = new WrapPanel { Background = (Brush)FindResource("PanelBrush"), Margin = new Thickness(0, 0, 0, 1) };
        DockPanel.SetDock(toolbar, Dock.Top); root.Children.Add(toolbar);
        var categoryBar = new WrapPanel { Margin = new Thickness(4), Background = (Brush)FindResource("PanelBrush") };
        var ribbon = new DockPanel { Background = (Brush)FindResource("PanelBrush") };
        var brand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 6, 4, 6), ToolTip = "SnapMark · Capture & markup" };
        brand.Children.Add(new Image { Source = new BitmapImage(new Uri("pack://application:,,,/SnapMark;component/Assets/SnapMark.png")), Width = 30, Height = 30, Margin = new Thickness(0, 0, 7, 0) });
        brand.Children.Add(new TextBlock { Text = "SnapMark", FontSize = 12, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        DockPanel.SetDock(brand, Dock.Left); ribbon.Children.Add(brand); ribbon.Children.Add(categoryBar);
        DockPanel.SetDock(ribbon, Dock.Top); root.Children.Add(ribbon);
        foreach (var categoryId in this.layout.Ordered("categories", ToolCatalog.Categories.Select(c => c.Id)))
        {
            var category = ToolCatalog.Categories.First(c => c.Id == categoryId);
            var names = new Dictionary<string, string> { ["edit"] = "Edit", ["draw"] = "Draw", ["redact"] = "Blur", ["label"] = "Text", ["review"] = "Review", ["measure"] = "Measure", ["symbols"] = "Symbols" };
            var icons = new Dictionary<string, string> { ["edit"] = "select", ["draw"] = "freehand", ["redact"] = "blur", ["label"] = "text", ["review"] = "stamp", ["measure"] = "dimension", ["symbols"] = "symbol" };
            var button = new Button { Content = names[category.Id], ToolTip = category.Name + " · " + category.Description, FontWeight = FontWeights.SemiBold, Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(2), Tag = category.Id, Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 45, 65)), Foreground = Brushes.White };
            ToolIcon.SetButton(button, icons[category.Id], names[category.Id], category.Name + " · " + category.Description);
            button.Click += (_, _) => SetTool(categoryTools.GetValueOrDefault(category.Id, ToolCatalog.Tools.First(t => t.CategoryId == category.Id).Tool));
            categoryButtons.Add(category.Id, button); categoryBar.Children.Add(button);
            ReorderButtons.Attach(categoryBar, button, () => SaveOrder("categories", categoryBar));
        }
        WrapPanel actionGroup = null!;
        void ToolbarGroup(string label)
        {
            var panel = new StackPanel(); panel.Children.Add(new TextBlock { Text = label, Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush"), FontSize = 10, Margin = new Thickness(8, 3, 8, 0) });
            actionGroup = new WrapPanel(); panel.Children.Add(actionGroup);
            toolbar.Children.Add(new Border { Child = panel, BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(54, 72, 96)), BorderThickness = new Thickness(0, 0, 1, 0), Margin = new Thickness(2) });
        }
        void ActionButton(string label, Action action)
        {
            var icon = label switch { "Copy · Ctrl+C" => "copy", "Export image · Ctrl+S" => "export", "Save editable project" => "save", "Team folders…" => "team", "Plugin export…" => "plugin", "Fit" => "fit", "100%" => "actual", "−" => "minus", "+" => "plus", "Grid / rulers" => "grid", "History" => "history", "Paste image · Ctrl+V" => "paste", "Insert file" => "image", "Reset crop" => "crop", "Canvas size" => "canvas", "OCR text…" => "ocr", "Before / after" => "compare", _ => "image" };
            var caption = label switch { "Copy · Ctrl+C" => "Copy", "Export image · Ctrl+S" => "Export", "Save editable project" => "Project", _ => (string?)null };
            var b = new Button { Padding = new Thickness(6) };
            ToolIcon.SetButton(b, icon, caption, label);
            b.Click += (_, _) => action(); actionGroup.Children.Add(b);
        }
        ToolbarGroup("SELECT");
        selectButton = new Button { Content = "Select วัตถุ · V", ToolTip = "Select and move objects. Ctrl+click selects multiple objects; drag corner or vertex handles to reshape." };
        selectButton.Padding = new Thickness(6);
        ToolIcon.SetButton(selectButton, "select", "Select", "Select · V · Ctrl+click selects multiple objects; drag the handle to resize.");
        selectButton.Click += (_, _) => SetTool(EditorTool.Select); actionGroup.Children.Add(selectButton);
        ToolbarGroup("OUTPUT");
        ActionButton("Copy · Ctrl+C", () => _ = Copy());
        ActionButton("Export image · Ctrl+S", Save);
        ActionButton("Save editable project", SaveProject);
        ActionButton("Team folders…", () => new TeamFolderWindow(teamSettings?.Invoke() ?? new AppSettings(), persistTeamSettings ?? (() => { }), (path, editable) => { if (editable) ProjectStorage.Save(path, surface.Image, surface.Document); else export.SavePng(renderer.Render(surface.Image, surface.Document), path); UpdateStatus("Team copy saved: " + path); }) { Owner = this }.ShowDialog());
        ActionButton("Plugin export…", () =>
        {
            if (plugins?.Exporters.Count is not > 0) { MessageBox.Show(this, "Load a trusted exporter from the main window's Plugins button first.", "Plugin export"); return; }
            var picker = new Window { Title = "Choose exporter", Owner = this, Width = 400, Height = 200, WindowStartupLocation = WindowStartupLocation.CenterOwner, Style = (Style)FindResource(typeof(Window)) };
            var panel = new StackPanel { Margin = new Thickness(16) }; var choice = new ComboBox { ItemsSource = plugins.Exporters, DisplayMemberPath = "Name", SelectedIndex = 0 }; panel.Children.Add(choice); var go = new Button { Content = "Export…" }; panel.Children.Add(go); picker.Content = panel; go.Click += (_, _) => picker.DialogResult = true;
            if (picker.ShowDialog() != true || choice.SelectedItem is not ISnapMarkExporter plugin) return;
            try { var dialog = new Microsoft.Win32.SaveFileDialog { Filter = plugin.Name + "|*" + plugin.Extension, DefaultExt = plugin.Extension, FileName = this.nameFile("image") + plugin.Extension, InitialDirectory = this.outputFolder("image") }; if (dialog.ShowDialog(this) == true) { var bitmap = renderer.Render(surface.Image, surface.Document); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var bytes = new System.IO.MemoryStream(); encoder.Save(bytes); plugin.Export(new(bytes.ToArray(), bitmap.PixelWidth, bitmap.PixelHeight), dialog.FileName); UpdateStatus("Plugin exported " + dialog.FileName); } }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Plugin export failed"); }
        });
        ToolbarGroup("UNDO");
        var undo = new Button { Content = "Undo", IsEnabled = false }; undo.Click += (_, _) => surface.History.Undo(); actionGroup.Children.Add(undo);
        var redo = new Button { Content = "Redo", IsEnabled = false }; redo.Click += (_, _) => surface.History.Redo(); actionGroup.Children.Add(redo);
        undo.Padding = redo.Padding = new Thickness(6);
        ToolIcon.SetButton(undo, "undo", null, "Undo · Ctrl+Z"); ToolIcon.SetButton(redo, "redo", null, "Redo · Ctrl+Y");
        ToolbarGroup("VIEW");
        ActionButton("Fit", Fit); ActionButton("100%", () => SetZoom(1)); ActionButton("−", () => SetZoom(zoom / 1.2)); ActionButton("+", () => SetZoom(zoom * 1.2));
        ActionButton("Grid / rulers", () => { surface.ShowGuides = !surface.ShowGuides; surface.InvalidateVisual(); });
        ToolbarGroup("INSERT");
        ActionButton("History", () => { if (inlineHistory is not null) _ = RefreshInlineHistory(); else if (pickHistory?.Invoke(this) is { } item) InsertImage(item); });
        ActionButton("Paste image · Ctrl+V", PasteImage);
        ActionButton("Insert file", InsertFile);
        ToolbarGroup("CANVAS");
        ActionButton("Reset crop", () => surface.History.Execute(new SnapMark.Core.CropCommand(surface.Document, null)));
        ActionButton("Canvas size", () =>
        {
            var value = PromptText($"{surface.Document.Width} x {surface.Document.Height}"); if (value is null) return;
            var parts = value.ToLowerInvariant().Split(new[] { 'x', '×', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            try { if (parts.Length != 2 || !int.TryParse(parts[0], out var w) || !int.TryParse(parts[1], out var h)) throw new ArgumentException("Enter width x height in pixels."); surface.ResizeCanvas(w, h); Fit(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Canvas size"); }
        });
        ToolbarGroup("ANALYZE");
        ActionButton("OCR text…", () => new OcrWindow(renderer.Render(surface.Image, surface.Document)) { Owner = this }.Show());
        ActionButton("Before / after", () => {
            var reference = new EditorDocument(surface.Document.Width, surface.Document.Height) { Crop = surface.Document.Crop };
            new CompareWindow(renderer.Render(surface.Image, reference), renderer.Render(surface.Image, surface.Document)) { Owner = this }.Show();
        });
        ActionButton("Compare image…", () =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Reference image|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff" }; if (dialog.ShowDialog(this) != true) return;
            try { new CompareWindow(HistoryService.LoadImage(dialog.FileName), renderer.Render(surface.Image, surface.Document)) { Owner = this }.Show(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Compare"); }
        });
        status = new TextBlock { Margin = new Thickness(16, 10, 16, 10), Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush") };
        DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
        if (historyEntries is not null)
        {
            inlineHistory = new EditorHistoryPanel(historyEntries, InsertImage, recycleHistory, renameHistory);
            DockPanel.SetDock(inlineHistory, Dock.Bottom); root.Children.Add(inlineHistory);
        }
        properties = new ObjectPropertiesPanel(surface, RequestColorPick); DockPanel.SetDock(properties, Dock.Right); root.Children.Add(properties);
        var tools = new StackPanel { Background = (Brush)FindResource("PanelBrush") };
        var toolScroll = new ScrollViewer { Content = tools, Width = this.layout.ToolWidth, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var toolsPane = new DockPanel();
        var widthGrip = new Thumb { Width = 7, Cursor = Cursors.SizeWE, ToolTip = "ลากเพื่อปรับความกว้างเครื่องมือ / Resize tool panel" }; widthGrip.SetResourceReference(BackgroundProperty, "AccentBrush");
        DockPanel.SetDock(widthGrip, Dock.Right); toolsPane.Children.Add(widthGrip); toolsPane.Children.Add(toolScroll);
        widthGrip.DragDelta += (_, e) => { toolScroll.Width = Math.Clamp(toolScroll.Width + e.HorizontalChange, 220, Math.Max(220, Math.Min(600, ActualWidth - 450))); };
        widthGrip.DragCompleted += (_, _) => { this.layout.ToolWidth = toolScroll.Width; SaveLayout(); };
        DockPanel.SetDock(toolsPane, Dock.Left); root.Children.Add(toolsPane);
        void Heading(string text) => tools.Children.Add(new TextBlock { Text = text, Margin = new Thickness(12, 12, 0, 4), Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush") });
        Heading("TOOLS");
        toolList = new WrapPanel();
        var toolPicker = new ScrollViewer { Content = toolList, Height = this.layout.ToolHeight, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        tools.Children.Add(toolPicker);
        var heightGrip = new Thumb { Height = 7, Cursor = Cursors.SizeNS, ToolTip = "ลากเพื่อแสดงเครื่องมือเพิ่ม / Resize tool list" }; heightGrip.SetResourceReference(BackgroundProperty, "AccentBrush"); tools.Children.Add(heightGrip);
        heightGrip.DragDelta += (_, e) => toolPicker.Height = Math.Clamp(toolPicker.Height + e.VerticalChange, 100, 1600);
        heightGrip.DragCompleted += (_, _) => { this.layout.ToolHeight = toolPicker.Height; SaveLayout(); };
        toolHint = new TextBlock { Margin = new Thickness(12, 6, 12, 8), FontSize = 11, Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush"), TextWrapping = TextWrapping.Wrap };
        tools.Children.Add(toolHint);
        var stampPanel = new StackPanel(); stampOptions = stampPanel; tools.Children.Add(stampPanel);
        stampPanel.Children.Add(new TextBlock { Text = "DESIGN STAMP", Margin = new Thickness(12, 12, 0, 4), Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush") });
        var stamps = new ComboBox { ItemsSource = ToolCatalog.Stamps, DisplayMemberPath = "Caption", SelectedIndex = 0, MaxDropDownHeight = 350 };
        stamps.SelectionChanged += (_, _) => { if (stamps.SelectedItem is StampDefinition stamp) surface.StampText = stamp.Text; };
        stampPanel.Children.Add(stamps);
        var customStamp = new Button { Content = "Custom stamp text…" };
        customStamp.Click += (_, _) => { var text = PromptText(surface.StampText); if (!string.IsNullOrWhiteSpace(text)) { surface.StampText = text; stamps.SelectedIndex = -1; toolHint.Text = "Click to place: " + text; } };
        stampPanel.Children.Add(customStamp);
        var effectPanel = new StackPanel(); effectOptions = effectPanel; tools.Children.Add(effectPanel);
        effectPanel.Children.Add(new TextBlock { Text = "BLUR RADIUS / BLOCK SIZE (px)", Margin = new Thickness(12, 12, 0, 4), Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush"), FontSize = 11 });
        var strength = new ComboBox { ItemsSource = new double[] { 4, 8, 12, 20, 32, 48 }, SelectedItem = 12d };
        strength.SelectionChanged += (_, _) => { if (strength.SelectedItem is double size) surface.EffectSize = size; }; effectPanel.Children.Add(strength);
        Heading("STYLE PRESET");
        var presetPanel = new StackPanel(); tools.Children.Add(presetPanel);
        var presets = new ComboBox { ItemsSource = ToolCatalog.Presets, DisplayMemberPath = "Name" }; presetPanel.Children.Add(presets);
        var customPresets = new ComboBox { DisplayMemberPath = "Name", ToolTip = "Saved personal styles" }; presetPanel.Children.Add(customPresets); personalStylePicker = customPresets;
        var noPresets = new TextBlock { Text = "ยังไม่มี style ส่วนตัวที่บันทึก", FontSize = 11, Margin = new Thickness(8, 2, 8, 4), TextWrapping = TextWrapping.Wrap }; presetPanel.Children.Add(noPresets);
        var savePreset = new Button { Content = "Save current style / stamp…", IsEnabled = presetStore is not null }; presetPanel.Children.Add(savePreset);
        try { customPresets.ItemsSource = presetStore?.Load(); } catch (Exception ex) { savePreset.IsEnabled = false; toolHint.Text = "Could not load custom presets: " + ex.Message; }
        void RefreshPresetEmpty() { customPresets.IsEnabled = customPresets.Items.Count > 0; noPresets.Visibility = customPresets.IsEnabled ? Visibility.Collapsed : Visibility.Visible; }
        RefreshPresetEmpty();
        Heading("COLOR");
        var colors = new ColorPicker(color, "Primary color", RequestColorPick);
        colors.Changed += value => surface.Color = value; tools.Children.Add(colors);
        surface.ColorPickEnded += () => { if (pickingColor is { } picker) { picker.IsPicking = false; UpdateStatus(picker.Label + " · " + picker.Value); pickingColor = null; } };
        Heading("STROKE");
        var thickness = new ComboBox { ItemsSource = new double[] { 1, 2, 3, 4, 6, 8 }, SelectedItem = 3d };
        thickness.SelectionChanged += (_, _) => { if (thickness.SelectedItem is double value) surface.Thickness = value; }; tools.Children.Add(thickness);
        Heading("TEXT / STAMP SIZE");
        var font = new ComboBox { ItemsSource = new double[] { 14, 18, 24, 32, 48, 64 }, SelectedItem = 24d };
        font.SelectionChanged += (_, _) => { if (font.SelectedItem is double value) surface.FontSize = value; }; tools.Children.Add(font);
        var textFont = new ComboBox { ItemsSource = Fonts.SystemFontFamilies.Select(f => f.Source).Distinct().Order().ToArray(), SelectedItem = "Segoe UI", ToolTip = "Text font family" }; tools.Children.Add(textFont);
        textFont.SelectionChanged += (_, _) => { if (textFont.SelectedItem is string family) surface.TextFont = family; };
        var boldText = new CheckBox { Content = "Bold text", Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"), Margin = new Thickness(12, 4, 12, 4) }; tools.Children.Add(boldText);
        boldText.Checked += (_, _) => surface.BoldText = true; boldText.Unchecked += (_, _) => surface.BoldText = false;
        var align = new ComboBox { ItemsSource = Enum.GetValues<TextAlign>(), SelectedItem = TextAlign.Left, ToolTip = "Text alignment · ใช้กับกรอบข้อความและวัตถุที่เลือก" }; tools.Children.Add(align); align.SelectionChanged += (_, _) => { if (align.SelectedItem is TextAlign value) { surface.TextAlignment = value; surface.TextWidth = Math.Max(surface.TextWidth, 160); if (surface.Tool == EditorTool.Select) surface.ChangeSelectedProperty(a => a with { TextAlign = value, TextWidth = a.TextWidth > 0 ? a.TextWidth : Math.Max(80, AnnotationRenderer.GetBounds(a).Width + a.FontSize * 2) }, AnnotationProperties.Font); } };
        var textBackgroundPanel = new StackPanel(); tools.Children.Add(textBackgroundPanel);
        textBackgroundPanel.Children.Add(new TextBlock { Text = "TEXT BACKGROUND / พื้นหลังข้อความ", Margin = new Thickness(12, 8, 12, 4) });
        var textBackground = new CheckBox { Content = "Enable text background", Foreground = (Brush)FindResource("TextBrush"), Margin = new Thickness(12, 4, 12, 4) };
        textBackground.Checked += (_, _) => surface.TextBackground = true; textBackground.Unchecked += (_, _) => surface.TextBackground = false; textBackgroundPanel.Children.Add(textBackground);
        var backgroundColor = new ColorPicker("#FFFFFF", "Text background color", RequestColorPick); backgroundColor.Changed += value => surface.TextBackgroundColor = value; textBackgroundPanel.Children.Add(backgroundColor);
        var backgroundOpacity = new Slider { Minimum = 0, Maximum = 100, Value = 100, Margin = new Thickness(12, 4, 12, 4) };
        var backgroundPercent = new TextBlock { Text = "Background opacity: 100%", Margin = new Thickness(12, 4, 12, 4) };
        backgroundOpacity.ValueChanged += (_, _) => { surface.TextBackgroundOpacity = backgroundOpacity.Value / 100; backgroundPercent.Text = $"Background opacity: {backgroundOpacity.Value:0}%"; }; textBackgroundPanel.Children.Add(backgroundPercent); textBackgroundPanel.Children.Add(backgroundOpacity);
        var backgroundPadding = new ComboBox { ItemsSource = new double[] { 0, 4, 8, 12, 16, 24 }, SelectedItem = 8d, ToolTip = "Text background padding (px)" };
        backgroundPadding.SelectionChanged += (_, _) => { if (backgroundPadding.SelectedItem is double value) surface.TextPadding = value; }; textBackgroundPanel.Children.Add(backgroundPadding);
        var applyBackground = new Button { Content = "Apply text background to selection" }; applyBackground.Click += (_, _) => surface.ApplyTextBackgroundToSelection(); textBackgroundPanel.Children.Add(applyBackground);
        contextOptions.Add((textBackgroundPanel, t => t is EditorTool.Text or EditorTool.Select));
        presets.SelectionChanged += (_, _) =>
        {
            if (presets.SelectedItem is not StylePreset preset) return;
            colors.Value = preset.Color; thickness.SelectedItem = preset.Thickness; font.SelectedItem = preset.FontSize;
        };
        Heading("LINE TYPE / WEIGHT / ENDPOINTS");
        ComboBox Choice<T>(IEnumerable<T> items, T selected, Action<T> change)
        {
            var combo = new ComboBox { ItemsSource = items, SelectedItem = selected };
            combo.SelectionChanged += (_, _) => { if (combo.SelectedItem is T item) change(item); }; tools.Children.Add(combo); return combo;
        }
        var lineType = Choice(Enum.GetValues<LinePattern>(), LinePattern.Solid, value => surface.LinePattern = value);
        var endpoints = new StackPanel { Orientation = Orientation.Horizontal }; tools.Children.Add(endpoints);
        var startCap = new ComboBox { ItemsSource = Enum.GetValues<EndpointMarker>(), SelectedIndex = 0, Width = 108 };
        var endCap = new ComboBox { ItemsSource = Enum.GetValues<EndpointMarker>(), SelectedIndex = 0, Width = 108 };
        startCap.SelectionChanged += (_, _) => { if (startCap.SelectedItem is EndpointMarker value) surface.StartMarker = value; };
        endCap.SelectionChanged += (_, _) => { if (endCap.SelectedItem is EndpointMarker value) surface.EndMarker = value; };
        endpoints.Children.Add(startCap); endpoints.Children.Add(endCap);
        Heading("FILL / GRADIENT");
        var fillMode = Choice(Enum.GetValues<ShapeFill>(), ShapeFill.None, value => surface.Fill = value);
        var secondary = new ColorPicker("#38BDF8", "Gradient second color", RequestColorPick); secondary.Changed += value => surface.SecondColor = value; tools.Children.Add(secondary);
        Heading("OPACITY / ความทึบ");
        var opacityPanel = new StackPanel(); tools.Children.Add(opacityPanel);
        var opacityRow = new DockPanel(); opacityPanel.Children.Add(opacityRow);
        var percent = new TextBox { Text = "100", Width = 64, ToolTip = "Opacity percent 0–100" }; DockPanel.SetDock(percent, Dock.Right); opacityRow.Children.Add(percent);
        opacityRow.Children.Add(new TextBlock { Text = "% Opacity", Margin = new Thickness(12, 10, 0, 4), Foreground = (System.Windows.Media.Brush)FindResource("TextBrush") });
        var opacity = new Slider { Minimum = 0, Maximum = 1, Value = 1, TickFrequency = .01, IsSnapToTickEnabled = true, Margin = new Thickness(12, 4, 12, 8), ToolTip = "Opacity 0–100%" };
        opacityPanel.Children.Add(opacity); var opacitySync = false;
        opacity.ValueChanged += (_, _) => { surface.ToolOpacity = opacity.Value; if (opacitySync) return; opacitySync = true; percent.Text = (opacity.Value * 100).ToString("0"); percent.BorderBrush = Brushes.Gray; opacitySync = false; };
        percent.TextChanged += (_, _) =>
        {
            if (opacitySync) return;
            if (double.TryParse(percent.Text, out var value) && double.IsFinite(value) && value >= 0 && value <= 100)
            { opacitySync = true; opacity.Value = Math.Round(value) / 100; surface.ToolOpacity = opacity.Value; percent.BorderBrush = Brushes.Gray; opacitySync = false; }
            else percent.BorderBrush = Brushes.OrangeRed;
        };
        var applyOpacity = new Button { Content = "Apply style to selection", IsEnabled = false, ToolTip = "Apply these color, opacity and style settings to selected objects. Ctrl+Z restores their previous styles." };
        applyOpacity.Click += (_, _) => surface.ApplyStyleToSelection(); opacityPanel.Children.Add(applyOpacity);
        Heading("ROTATION (degrees)");
        var rotation = new TextBox { Text = "0" }; rotation.TextChanged += (_, _) => { if (double.TryParse(rotation.Text, out var value) && double.IsFinite(value)) surface.Rotation = value; }; tools.Children.Add(rotation);
        Heading("POLYGON / CONTINUOUS LINE");
        var sides = Choice(Enumerable.Range(3, 30), 5, value => surface.PolygonSides = value);
        var closed = new CheckBox { Content = "Close loop / fill free form", Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"), Margin = new Thickness(12, 6, 12, 6) };
        closed.Checked += (_, _) => surface.ClosedPath = true; closed.Unchecked += (_, _) => surface.ClosedPath = false; tools.Children.Add(closed);
        var finish = new Button { Content = "Finish path · Enter" }; finish.Click += (_, _) => surface.FinishPath(); tools.Children.Add(finish);
        Heading("STEP / CALLOUT SEQUENCE");
        var alpha = new CheckBox { Content = "Alphabetic A, B, C…", Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"), Margin = new Thickness(12, 6, 12, 6) }; tools.Children.Add(alpha);
        var next = new TextBox { Text = "1", ToolTip = "Next label, for example 12 or AA" }; tools.Children.Add(next);
        alpha.Checked += (_, _) => { surface.AlphabeticSteps = true; next.Text = GeometryMath.StepLabel(surface.NextStep, true); };
        alpha.Unchecked += (_, _) => { surface.AlphabeticSteps = false; next.Text = GeometryMath.StepLabel(surface.NextStep, false); };
        next.TextChanged += (_, _) => surface.NextStep = GeometryMath.ParseStep(next.Text, surface.AlphabeticSteps);
        surface.StepChanged += () => next.Text = GeometryMath.StepLabel(surface.NextStep, surface.AlphabeticSteps);
        Heading("DRAWING SYMBOL / LABEL");
        var symbolLabel = new TextBox { Text = "", ToolTip = "Optional drawing symbol label" }; symbolLabel.TextChanged += (_, _) => surface.SymbolText = symbolLabel.Text; tools.Children.Add(symbolLabel);
        var symbolSequence = new StackPanel(); tools.Children.Add(symbolSequence);
        symbolSequence.Children.Add(new TextBlock { Text = "LABEL MODE / ลำดับสัญลักษณ์", Margin = new Thickness(12, 8, 12, 4) });
        var symbolMode = new ComboBox { ItemsSource = Enum.GetValues<SymbolLabelMode>(), SelectedItem = SymbolLabelMode.Custom, ToolTip = "Custom / Alphabet / Numeric / Prefix" }; symbolSequence.Children.Add(symbolMode);
        var symbolStart = new TextBox { Text = "1", ToolTip = "Start / next sequence number (1–1000000)" }; symbolSequence.Children.Add(new TextBlock { Text = "Start / next number", Margin = new Thickness(12, 4, 12, 0) }); symbolSequence.Children.Add(symbolStart);
        var symbolPrefix = new TextBox { Text = "A-", ToolTip = "Prefix, e.g. RFI- / D- / GRID-" }; symbolSequence.Children.Add(new TextBlock { Text = "Prefix", Margin = new Thickness(12, 4, 12, 0) }); symbolSequence.Children.Add(symbolPrefix);
        var symbolAlphabet = new CheckBox { Content = "Prefix + Alphabet (A, B, C…)", Foreground = (Brush)FindResource("TextBrush"), Margin = new Thickness(12, 4, 12, 4) }; symbolSequence.Children.Add(symbolAlphabet);
        var symbolSize = new ComboBox { ItemsSource = new double[] { 12, 14, 18, 24, 32, 48, 64, 96 }, IsEditable = true, Text = "24", ToolTip = "Symbol font size (px)" }; symbolSequence.Children.Add(new TextBlock { Text = "Symbol font size (px)", Margin = new Thickness(12, 4, 12, 0) }); symbolSequence.Children.Add(symbolSize);
        symbolSize.LostKeyboardFocus += (_, _) => { if (double.TryParse(symbolSize.Text, out var value) && double.IsFinite(value) && value >= 1 && value <= 1000) { surface.FontSize = value; font.ItemsSource = ((IEnumerable<double>)font.ItemsSource).Append(value).Distinct().Order().ToArray(); font.SelectedItem = value; } };
        font.SelectionChanged += (_, _) => symbolSize.Text = surface.FontSize.ToString("0.##");
        var symbolPreview = new TextBlock { Margin = new Thickness(12, 4, 12, 8) }; symbolSequence.Children.Add(symbolPreview);
        var symbolSizeLabel = symbolSequence.Children.OfType<TextBlock>().First(t => t.Text == "Symbol font size (px)"); symbolSequence.Children.Remove(symbolSizeLabel); symbolSequence.Children.Remove(symbolSize); symbolSequence.Children.Insert(0, symbolSizeLabel); symbolSequence.Children.Insert(1, symbolSize);
        void UpdateSymbolSequence() { surface.SymbolLabelMode = symbolMode.SelectedItem is SymbolLabelMode m ? m : SymbolLabelMode.Custom; surface.SymbolPrefix = symbolPrefix.Text; surface.AlphabeticSymbols = symbolAlphabet.IsChecked == true; if (int.TryParse(symbolStart.Text, out var n) && n is >= 1 and <= 1000000) surface.NextSymbol = n; symbolPreview.Text = "Next: " + SymbolSequence.Label(surface.SymbolLabelMode, surface.NextSymbol, surface.SymbolPrefix, surface.AlphabeticSymbols, surface.SymbolText); symbolPrefix.IsEnabled = symbolAlphabet.IsEnabled = surface.SymbolLabelMode == SymbolLabelMode.Prefix; symbolStart.IsEnabled = surface.SymbolLabelMode != SymbolLabelMode.Custom; }
        symbolMode.SelectionChanged += (_, _) => UpdateSymbolSequence(); symbolStart.TextChanged += (_, _) => UpdateSymbolSequence(); symbolPrefix.TextChanged += (_, _) => UpdateSymbolSequence(); symbolAlphabet.Click += (_, _) => UpdateSymbolSequence(); symbolLabel.TextChanged += (_, _) => UpdateSymbolSequence(); surface.SymbolSequenceChanged += () => symbolStart.Text = surface.NextSymbol.ToString(); UpdateSymbolSequence();
        contextOptions.Add((symbolSequence, t => t == EditorTool.Symbol));
        var libraryPanel = new StackPanel(); tools.Children.Add(libraryPanel);
        var savedSymbols = new ComboBox { DisplayMemberPath = "Name", ItemsSource = symbolLibrary?.Entries(), ToolTip = "Your imported SVG symbols" }; libraryPanel.Children.Add(savedSymbols);
        var noSymbols = new TextBlock { Text = "ยังไม่มี SVG symbol ที่ Import ไว้", FontSize = 11, Margin = new Thickness(8, 2, 8, 4), TextWrapping = TextWrapping.Wrap }; libraryPanel.Children.Add(noSymbols);
        var importSymbol = new Button { Content = "Import SVG symbol…", IsEnabled = symbolLibrary is not null }; libraryPanel.Children.Add(importSymbol);
        var insertSymbol = new Button { Content = "Insert saved symbol", IsEnabled = symbolLibrary is not null }; libraryPanel.Children.Add(insertSymbol);
        void RefreshSymbolEmpty() { savedSymbols.IsEnabled = insertSymbol.IsEnabled = savedSymbols.Items.Count > 0; noSymbols.Visibility = savedSymbols.IsEnabled ? Visibility.Collapsed : Visibility.Visible; }
        RefreshSymbolEmpty();
        importSymbol.Click += (_, _) =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Basic SVG symbol|*.svg" }; if (dialog.ShowDialog(this) != true || symbolLibrary is null) return;
            try { var saved = symbolLibrary.ImportSvg(dialog.FileName, surface.Color); savedSymbols.ItemsSource = symbolLibrary.Entries(); RefreshSymbolEmpty(); savedSymbols.SelectedItem = savedSymbols.Items.Cast<SavedSymbol>().First(s => s.Path == saved.Path); InsertImage(SymbolLibrary.Load(saved)); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Import SVG symbol"); }
        };
        insertSymbol.Click += (_, _) => { if (savedSymbols.SelectedItem is SavedSymbol symbol) { try { InsertImage(SymbolLibrary.Load(symbol)); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Insert symbol"); } } };
        customPresets.SelectionChanged += (_, _) =>
        {
            if (customPresets.SelectedItem is not UserStylePreset p) return;
            surface.LoadStyle(p); colors.Value = p.Color; secondary.Value = p.SecondColor; opacity.Value = p.Opacity; rotation.Text = p.Rotation.ToString("0.##");
            textFont.SelectedItem = p.FontFamily; boldText.IsChecked = p.Bold; align.SelectedItem = p.TextAlign;
            textBackground.IsChecked = p.TextBackground; backgroundColor.Value = p.TextBackgroundColor; backgroundOpacity.Value = p.TextBackgroundOpacity * 100;
            backgroundPadding.ItemsSource = new double[] { 0, 4, 8, 12, 16, 24, p.TextPadding }.Distinct().Order().ToArray(); backgroundPadding.SelectedItem = p.TextPadding;
            thickness.ItemsSource = ((IEnumerable<double>)thickness.ItemsSource).Append(p.Thickness).Distinct().Order().ToArray(); thickness.SelectedItem = p.Thickness;
            font.ItemsSource = ((IEnumerable<double>)font.ItemsSource).Append(p.FontSize).Distinct().Order().ToArray(); font.SelectedItem = p.FontSize;
            strength.ItemsSource = ((IEnumerable<double>)strength.ItemsSource).Append(p.EffectSize).Distinct().Order().ToArray(); strength.SelectedItem = p.EffectSize;
            lineType.SelectedItem = p.LinePattern; fillMode.SelectedItem = p.Fill; startCap.SelectedItem = p.StartMarker; endCap.SelectedItem = p.EndMarker; sides.SelectedItem = p.PolygonSides; closed.IsChecked = p.ClosedPath;
            stamps.SelectedItem = ToolCatalog.Stamps.FirstOrDefault(s => s.Text == p.StampText); customStamp.ToolTip = p.StampText;
        };
        savePreset.Click += (_, _) =>
        {
            var name = PromptText("My markup style"); if (string.IsNullOrWhiteSpace(name) || presetStore is null) return;
            try
            {
                if (presetStore.Load().Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) && MessageBox.Show(this, "Replace saved style " + name + "?", "SnapMark", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
                presetStore.Save(surface.SaveStyle(name)); customPresets.ItemsSource = presetStore.Load(); RefreshPresetEmpty(); UpdateStatus("Saved personal style: " + name);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Save style"); }
        };
        Heading("OBJECT ACTIONS / จัดการวัตถุ");
        var vertices = new CheckBox { Content = "Edit path vertices · drag orange handles", Margin = new Thickness(8), Foreground = (System.Windows.Media.Brush)FindResource("TextBrush") };
        vertices.Checked += (_, _) => { surface.EditVertices = true; surface.InvalidateVisual(); }; vertices.Unchecked += (_, _) => { surface.EditVertices = false; surface.InvalidateVisual(); }; tools.Children.Add(vertices);
        var apply = new Button { Content = "Apply style to selection" }; apply.Click += (_, _) => surface.ApplyStyleToSelection(); tools.Children.Add(apply);
        var editLabel = new Button { Content = "Edit selected text / stamp · F2" }; editLabel.Click += (_, _) => surface.EditSelectedText(); tools.Children.Add(editLabel);
        var forward = new Button { Content = "Bring selection forward" }; forward.Click += (_, _) => surface.ReorderSelection(true); tools.Children.Add(forward);
        var backward = new Button { Content = "Send selection backward" }; backward.Click += (_, _) => surface.ReorderSelection(false); tools.Children.Add(backward);
        var remove = new Button { Content = "Delete selection" }; remove.Click += (_, _) => surface.DeleteSelection(); tools.Children.Add(remove);
        tools.Children.Add(new TextBlock { Text = "Select: move / Delete / Ctrl+D\nDouble-click text or stamp to edit\nCtrl+wheel: zoom · Middle-drag: pan", Margin = new Thickness(12, 12, 8, 16), FontSize = 11, Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush"), TextWrapping = TextWrapping.Wrap });
        // Keep options for the active tool close to its buttons.
        void GroupOptions(string heading, int count, Func<EditorTool, bool> show, bool nearTool = false)
        {
            var first = tools.Children.OfType<TextBlock>().First(t => t.Text == heading);
            var index = tools.Children.IndexOf(first); var panel = new StackPanel();
            for (var i = 0; i < count; i++) { var child = tools.Children[index]; tools.Children.RemoveAt(index); panel.Children.Add(child); }
            first.Foreground = (Brush)FindResource("AccentBrush"); first.FontWeight = FontWeights.SemiBold;
            var card = new Border { Child = panel, Background = (Brush)FindResource("PanelBrush"), BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(54, 72, 96)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(7), Margin = new Thickness(6, 5, 6, 5), Padding = new Thickness(2, 0, 2, 6) };
            tools.Children.Insert(nearTool ? tools.Children.IndexOf(toolHint) + 1 : index, card);
            contextOptions.Add((card, show));
        }
        GroupOptions("DRAWING SYMBOL / LABEL", 4, t => t == EditorTool.Symbol, true);
        GroupOptions("STEP / CALLOUT SEQUENCE", 3, t => t is EditorTool.Number or EditorTool.Callout, true);
        GroupOptions("POLYGON / CONTINUOUS LINE", 4, t => t is EditorTool.Polygon or EditorTool.RegularPolygon or EditorTool.Polyline or EditorTool.Freehand or EditorTool.CloudFreeform or EditorTool.Area or EditorTool.Length or EditorTool.Angle);
        GroupOptions("LINE TYPE / WEIGHT / ENDPOINTS", 3, t => t is EditorTool.Select or EditorTool.Arrow or EditorTool.Line or EditorTool.Polyline or EditorTool.Freehand or EditorTool.Dimension or EditorTool.Distance or EditorTool.Length);
        GroupOptions("FILL / GRADIENT", 3, t => t is EditorTool.Select or EditorTool.Rectangle or EditorTool.RoundedRectangle or EditorTool.Ellipse or EditorTool.Circle or EditorTool.Triangle or EditorTool.Polygon or EditorTool.RegularPolygon or EditorTool.Freehand or EditorTool.Plane or EditorTool.Highlight);
        GroupOptions("STYLE PRESET", 6, _ => true);
        GroupOptions("TEXT / STAMP SIZE", 5, t => t is EditorTool.Select or EditorTool.Text or EditorTool.Note or EditorTool.Stamp or EditorTool.Number or EditorTool.Callout);
        GroupOptions("ROTATION (degrees)", 2, _ => true);
        GroupOptions("OBJECT ACTIONS / จัดการวัตถุ", 7, t => t == EditorTool.Select);
        GroupOptions("OPACITY / ความทึบ", 2, _ => true, true);
        var measurePanel = new StackPanel { Margin = new Thickness(8) };
        measurePanel.Children.Add(new TextBlock { Text = "MEASURE · Scale → Length / Area", Foreground = (Brush)FindResource("AccentBrush"), TextWrapping = TextWrapping.Wrap });
        measurePanel.Children.Add(new TextBlock { Text = "คลิกจุดเริ่มเพื่อปิดลูป · P = รอบรูป · A = พื้นที่\nScale ใช้เส้นอ้างอิงที่ทราบระยะ ทุกเส้นวัดจะคำนวณใหม่", FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 5) });
        var calibrate = new Button(); ToolIcon.SetButton(calibrate, "calibrate", "Scale", "ลากเส้นอ้างอิง แล้วใส่ระยะจริงและหน่วย"); calibrate.Click += (_, _) => SetTool(EditorTool.Calibrate); measurePanel.Children.Add(calibrate);
        var perimeter = new Button { Content = "Selection → Length / Perimeter", IsEnabled = false, ToolTip = "Select a continuous line, polygon or freeform path first" }; perimeter.Click += (_, _) => surface.MeasureSelectedPath(false); measurePanel.Children.Add(perimeter);
        var measureArea = new Button { Content = "Selection → Area + Perimeter", IsEnabled = false }; measureArea.Click += (_, _) => surface.MeasureSelectedPath(true); measurePanel.Children.Add(measureArea);
        tools.Children.Insert(tools.Children.IndexOf(toolHint) + 1, measurePanel);
        contextOptions.Add((measurePanel, t => t is EditorTool.Select or EditorTool.Length or EditorTool.Area or EditorTool.Calibrate or EditorTool.Distance));
        surface.SelectionChanged += () => { var a = surface.Document.Annotations.Find(a => a.Id == surface.SelectedId); perimeter.IsEnabled = a?.Points is { Count: >= 2 }; measureArea.IsEnabled = a?.Points is { Count: >= 3 }; };
        tools.Children.Remove(textBackgroundPanel); tools.Children.Insert(tools.Children.IndexOf(toolHint) + 1, textBackgroundPanel);
        surface.SelectionChanged += () =>
        {
            if (surface.Tool == EditorTool.Select && lastTool != EditorTool.Select) SetTool(EditorTool.Select);
            applyOpacity.IsEnabled = surface.Selection.Count > 0;
            foreach (var options in contextOptions) options.Panel.Visibility = surface.Tool == EditorTool.Select && surface.Selection.Count > 0 && !options.Show(EditorTool.Eraser) ? Visibility.Collapsed : options.Show(surface.Tool) ? Visibility.Visible : Visibility.Collapsed;
            measurePanel.Visibility = surface.Tool is EditorTool.Select or EditorTool.Length or EditorTool.Area or EditorTool.Calibrate or EditorTool.Distance ? Visibility.Visible : Visibility.Collapsed;
            if (surface.Document.Annotations.Find(a => a.Id == surface.SelectedId) is not { } selected) return;
            colors.Value = selected.Color; secondary.Value = selected.SecondColor; opacity.Value = selected.Opacity;
            surface.FillColor = selected.FillColor; surface.FillOpacity = selected.FillOpacity; surface.StrokeOpacity = selected.StrokeOpacity; surface.TextColor = selected.TextColor;
            percent.Text = (selected.Opacity * 100).ToString("0"); rotation.Text = selected.Rotation.ToString("0.##");
            if (thickness.ItemsSource is IEnumerable<double> weights && !weights.Contains(selected.Thickness)) thickness.ItemsSource = weights.Append(selected.Thickness).Order().ToArray();
            if (font.ItemsSource is IEnumerable<double> sizes && !sizes.Contains(selected.FontSize)) font.ItemsSource = sizes.Append(selected.FontSize).Order().ToArray();
            thickness.SelectedItem = selected.Thickness; font.SelectedItem = selected.FontSize;
            textFont.SelectedItem = selected.FontFamily; boldText.IsChecked = selected.Bold; align.SelectedItem = selected.TextAlign; lineType.SelectedItem = selected.LinePattern; fillMode.SelectedItem = selected.Fill; startCap.SelectedItem = selected.StartMarker; endCap.SelectedItem = selected.EndMarker;
            textBackground.IsChecked = selected.TextBackground; backgroundColor.Value = selected.TextBackgroundColor; backgroundOpacity.Value = selected.TextBackgroundOpacity * 100;
            backgroundPadding.ItemsSource = new double[] { 0, 4, 8, 12, 16, 24, selected.TextPadding }.Distinct().Order().ToArray(); backgroundPadding.SelectedItem = selected.TextPadding;
            if (strength.ItemsSource is IEnumerable<double> strengths && !strengths.Contains(selected.EffectSize)) strength.ItemsSource = strengths.Append(selected.EffectSize).Order().ToArray();
            strength.SelectedItem = selected.EffectSize;
        };
        scroll = new ScrollViewer { HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Background = (Brush)FindResource("CanvasBrush"), Content = surface, Padding = new Thickness(24) };
        surface.HorizontalAlignment = HorizontalAlignment.Left; surface.VerticalAlignment = VerticalAlignment.Top;
        var viewport = new Grid(); viewport.Children.Add(scroll);
        if (inlineHistory is not null)
        {
            viewport.Children.Add(inlineHistory.Preview);
            inlineHistory.PreviewChanged += preview =>
            {
                if (preview) surface.CancelGesture();
                toolbar.IsEnabled = categoryBar.IsEnabled = toolScroll.IsEnabled = properties.IsEnabled = !preview;
                if (!preview) surface.Focus();
            };
        }
        root.Children.Add(viewport);
        surface.History.Changed += () => { dirty = true; revision++; undo.IsEnabled = surface.History.CanUndo; redo.IsEnabled = surface.History.CanRedo; UpdateStatus(); };
        autosaveTimer.Tick += async (_, _) => { if (!closingEditor && dirty && savedRevision != revision && recoveryWrite.IsCompleted) { try { await FlushRecovery(); } catch (Exception ex) { UpdateStatus("Autosave failed: " + ex.Message); } } };
        if (recovery is not null) autosaveTimer.Start();
        PreviewKeyDown += KeyPressed;
        scroll.PreviewMouseWheel += (_, e) => { if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return; SetZoom(zoom * (e.Delta > 0 ? 1.1 : 1 / 1.1)); e.Handled = true; };
        scroll.PreviewMouseDown += (_, e) => { if (e.ChangedButton != MouseButton.Middle) return; panning = true; panStart = e.GetPosition(scroll); panX = scroll.HorizontalOffset; panY = scroll.VerticalOffset; scroll.CaptureMouse(); e.Handled = true; };
        scroll.PreviewMouseMove += (_, e) => { if (!panning) return; var p = e.GetPosition(scroll); scroll.ScrollToHorizontalOffset(panX - p.X + panStart.X); scroll.ScrollToVerticalOffset(panY - p.Y + panStart.Y); e.Handled = true; };
        scroll.PreviewMouseUp += (_, e) => { if (e.ChangedButton != MouseButton.Middle) return; panning = false; scroll.ReleaseMouseCapture(); e.Handled = true; };
        scroll.LostMouseCapture += (_, _) => panning = false;
        Loaded += async (_, _) => { Fit(); surface.Focus(); await RefreshInlineHistory(); if (!closingEditor) { UpdateLayout(); Fit(); } };
        AllowDrop = true;
        DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        Drop += (_, e) => { if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) foreach (var path in paths.Take(8)) { try { InsertImage(HistoryService.LoadImage(path)); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Insert dropped image"); } } };
        Closing += (_, e) =>
        {
            if (allowClose) return; e.Cancel = true; if (closingEditor) return;
            closingEditor = true;
            // Even an awaited task can finish synchronously. Leave WPF's Closing event
            // before displaying Save dialogs or calling Close again.
            Dispatcher.BeginInvoke(new Action(async () => await FinishClose()));
        };
        Closed += (_, _) => { autosaveTimer.Stop(); inlineHistory?.Dispose(); };
        SetTool(lastTool); UpdateStatus();
        if (recoveryId is not null) { dirty = true; revision = 1; UpdateStatus("Recovered editable work · Save project when ready."); }
    }
    private async Task FinishClose()
    {
        var wasEnabled = IsEnabled;
        try
        {
            var choice = dirty ? MessageBox.Show(this, recovery is null ? "Save an editable project before closing?\nYes: Save project · No: Discard · Cancel: Keep editing" : "Keep editable work in local recovery before closing?\nYes: Keep recovery · No: Discard · Cancel: Keep editing", "SnapMark", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) : MessageBoxResult.No;
            if (choice == MessageBoxResult.Cancel) return;
            if (choice == MessageBoxResult.Yes && recovery is null) { SaveProject(); if (dirty) return; }
            // Keep the document stable while its final recovery write is in progress.
            IsEnabled = false;
            if (choice == MessageBoxResult.Yes && recovery is not null) await FlushRecovery();
            else { await recoveryWrite; recovery?.Remove(recoveryId); }
            autosaveTimer.Stop(); allowClose = true; Close();
        }
        catch (Exception ex) { allowClose = false; MessageBox.Show(this, ex.Message, "Could not close safely"); }
        finally { if (!allowClose) IsEnabled = wasEnabled; closingEditor = false; }
    }
    private void SaveLayout()
    {
        try { layout.Save(); } catch (Exception ex) { UpdateStatus("Could not save tool layout: " + ex.Message); }
    }
    private void SaveOrder(string group, Panel panel)
    {
        layout.Orders[group] = panel.Children.OfType<Button>().Select(b => (string)b.Tag).ToList(); SaveLayout();
    }
    private void ShowCategory()
    {
        var category = ToolCatalog.Categories.First(c => c.Id == activeCategory);
        toolList.Children.Clear(); toolButtons.Clear(); symbolButtons.Clear();
        if (category.Id == "symbols")
        {
            foreach (var symbol in layout.Ordered("symbol-variants", ToolCatalog.Symbols))
            {
                var caption = char.ToUpperInvariant(symbol[0]) + symbol[1..];
                var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                content.Children.Add(new SymbolToolIcon(symbol));
                content.Children.Add(new TextBlock { Text = caption, FontSize = 9, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, MaxWidth = 65, TextTrimming = TextTrimming.CharacterEllipsis });
                var button = new Button { Tag = symbol, Content = content, Width = 72, Height = 62, Margin = new Thickness(2), Padding = new Thickness(2), ToolTip = caption + " · เลือกแล้วลากบนภาพ · แก้ข้อความ สี ขนาด ด้านล่าง หรือ Select (V) เพื่อแก้วัตถุเดิม" };
                System.Windows.Automation.AutomationProperties.SetName(button, caption + " symbol");
                button.Click += (_, _) => { surface.SymbolId = symbol; SetTool(EditorTool.Symbol); };
                symbolButtons.Add(symbol, button); toolList.Children.Add(button);
                ReorderButtons.Attach(toolList, button, () => SaveOrder("symbol-variants", toolList));
            }
            return;
        }
        foreach (var id in layout.Ordered(category.Id, ToolCatalog.Tools.Where(t => t.CategoryId == category.Id && t.Tool != EditorTool.Eyedropper).Select(t => t.Tool.ToString())))
        {
            var definition = ToolCatalog.Tools.First(t => t.Tool.ToString() == id);
            var button = new Button { Tag = id, Width = 60, Height = 58, Margin = new Thickness(2), Padding = new Thickness(3), HorizontalContentAlignment = HorizontalAlignment.Center };
            ToolIcon.SetButton(button, definition.Tool.ToString().ToLowerInvariant(), ToolIcon.ShortLabel(definition.Tool), definition.Name + " · " + definition.Hint, vertical: true);
            button.Click += (_, _) => SetTool(definition.Tool); toolList.Children.Add(button); toolButtons.Add(definition.Tool, button);
            ReorderButtons.Attach(toolList, button, () => SaveOrder(category.Id, toolList));
        }
        if (toolButtons.Count == 0)
        {
            surface.CancelGesture(); surface.Tool = EditorTool.Select; surface.Cursor = Cursors.Arrow;
            toolList.Children.Add(new TextBlock { Text = "Coming next / ยังไม่เปิดใช้", Margin = new Thickness(12, 12, 12, 4), Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush") });
            toolHint.Text = category.Description; stampOptions.Visibility = effectOptions.Visibility = Visibility.Collapsed;
        }
    }
    private void SetTool(EditorTool tool)
    {
        surface.CancelGesture(); surface.Tool = tool;
        lastTool = tool;
        var definition = ToolCatalog.Find(tool);
        categoryTools[definition.CategoryId] = tool;
        if (activeCategory != definition.CategoryId) { activeCategory = definition.CategoryId; ShowCategory(); }
        foreach (var pair in categoryButtons) { pair.Value.Background = new SolidColorBrush(pair.Key == activeCategory ? System.Windows.Media.Color.FromRgb(10, 89, 111) : System.Windows.Media.Color.FromRgb(14, 45, 65)); pair.Value.BorderBrush = pair.Key == activeCategory ? (Brush)FindResource("AccentBrush") : Brushes.Transparent; pair.Value.BorderThickness = new Thickness(2); }
        stampOptions.Visibility = tool == EditorTool.Stamp ? Visibility.Visible : Visibility.Collapsed;
        effectOptions.Visibility = tool is EditorTool.Blur or EditorTool.Pixelate ? Visibility.Visible : Visibility.Collapsed;
        foreach (var options in contextOptions) options.Panel.Visibility = tool == EditorTool.Select && surface.Selection.Count > 0 && !options.Show(EditorTool.Eraser) ? Visibility.Collapsed : options.Show(tool) ? Visibility.Visible : Visibility.Collapsed;
        properties.Refresh();
        toolHint.Text = definition.Hint;
        foreach (var pair in toolButtons) { pair.Value.Background = pair.Key == tool ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 107, 133)) : (Brush)FindResource("ButtonBrush"); pair.Value.Foreground = pair.Key == tool ? Brushes.White : (Brush)FindResource("TextBrush"); }
        foreach (var pair in symbolButtons) { var selected = tool == EditorTool.Symbol && pair.Key == surface.SymbolId; pair.Value.Background = selected ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 107, 133)) : (Brush)FindResource("ButtonBrush"); pair.Value.Foreground = selected ? Brushes.White : (Brush)FindResource("TextBrush"); }
        selectButton.Background = tool == EditorTool.Select ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 107, 133)) : (Brush)FindResource("ButtonBrush");
        selectButton.Foreground = tool == EditorTool.Select ? Brushes.White : (Brush)FindResource("TextBrush");
        surface.Cursor = tool == EditorTool.Select ? Cursors.Arrow : Cursors.Cross; surface.InvalidateVisual(); surface.Focus();
    }
    private void SetZoom(double value) { zoom = Math.Clamp(value, .05, 8); surface.LayoutTransform = new ScaleTransform(zoom, zoom); UpdateStatus(); }
    private void Fit() => SetZoom(Math.Min(1, Math.Min(Math.Max(1, scroll.ViewportWidth - 48) / surface.Width, Math.Max(1, scroll.ViewportHeight - 48) / surface.Height)));
    private void UpdateStatus(string? message = null)
    {
        var crop = surface.Document.Crop;
        status.Text = message ?? $"{crop?.Width ?? surface.Width:0} × {crop?.Height ?? surface.Height:0} px  ·  Zoom {zoom:P0}  ·  {surface.Document.Annotations.Count} annotations  ·  Scale: {surface.Document.UnitsPerPixel:0.####} {surface.Document.Unit}/px";
        Title = "SnapMark · Editor" + (dirty ? " *" : "");
    }
    private async Task Copy()
    {
        try { await clipboard.CopyAsync(inlineHistory?.SelectedImage ?? renderer.Render(surface.Image, surface.Document)); UpdateStatus("Copied to clipboard."); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Copy failed"); }
    }
    private void Save()
    {
        string folder, filename; try { folder = outputFolder("image"); System.IO.Directory.CreateDirectory(folder); filename = nameFile("image"); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Export folder / filename"); return; }
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "PNG image|*.png|JPEG image|*.jpg|BMP image|*.bmp|TIFF image|*.tif|Flattened PDF|*.pdf", DefaultExt = ".png", FileName = filename, InitialDirectory = folder };
        if (dialog.ShowDialog(this) != true) return;
        try { ImageFormats.Save(renderer.Render(surface.Image, surface.Document), dialog.FileName); UpdateStatus("Exported " + dialog.FileName + " · Save editable project to preserve edits."); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Save failed"); }
    }
    private void SaveProject()
    {
        string folder, filename; try { folder = outputFolder("project"); System.IO.Directory.CreateDirectory(folder); filename = nameFile("project"); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Project folder / filename"); return; }
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Editable SnapMark project|*.snapmark", DefaultExt = ".snapmark", FileName = filename + ".snapmark", InitialDirectory = folder };
        if (dialog.ShowDialog(this) != true) return;
        try { ProjectStorage.Save(dialog.FileName, surface.Image, surface.Document); recoveryWrite.GetAwaiter().GetResult(); recovery?.Remove(recoveryId); dirty = false; savedRevision = revision; UpdateStatus("Editable project saved (includes original image): " + dialog.FileName); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Save project failed"); }
    }
    public void InsertImage(BitmapSource bitmap) { inlineHistory?.Back(); surface.InsertImage(bitmap); SetTool(EditorTool.Select); }
    private Task FlushRecovery()
    {
        if (recovery is null) return Task.CompletedTask;
        var snapshot = RecoveryStore.Snapshot(surface.Document); var currentRevision = revision;
        var previous = recoveryWrite;
        recoveryWrite = Task.Run(async () => { try { await previous.ConfigureAwait(false); } catch { } recovery.Save(recoveryId, surface.Image, snapshot); savedRevision = currentRevision; });
        return recoveryWrite;
    }
    public void FlushRecoveryOnExit() { if (dirty && recovery is not null) { try { FlushRecovery().GetAwaiter().GetResult(); } catch { } } }
    public void PrepareForExit()
    {
        if (dirty && recovery is null) throw new InvalidOperationException("Save or close editors first when autosave is disabled.");
        if (dirty) FlushRecovery().GetAwaiter().GetResult();
    }
    public void CommitExit()
    {
        allowClose = true; autosaveTimer.Stop();
    }
    private void PasteImage()
    {
        try { if (System.Windows.Clipboard.GetImage() is { } image) InsertImage(image); else UpdateStatus("Clipboard has no image."); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Paste image"); }
    }
    private void InsertFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff" };
        if (dialog.ShowDialog(this) == true) { try { InsertImage(HistoryService.LoadImage(dialog.FileName)); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Insert image"); } }
    }
    private (double Length, string Unit)? PromptCalibration(double pixels)
    {
        var dialog = new Window { Style = (Style)FindResource(typeof(Window)), Title = "SnapMark · Calibrate scale", Width = 440, Height = 300, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize };
        var panel = new StackPanel { Margin = new Thickness(16) }; dialog.Content = panel;
        panel.Children.Add(new TextBlock { Text = $"Selected reference: {pixels:0.##} px\nEnter its real length and unit.", TextWrapping = TextWrapping.Wrap });
        var row = new StackPanel { Orientation = Orientation.Horizontal }; panel.Children.Add(row);
        var input = new TextBox { Text = pixels.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture), Width = 230, ToolTip = "Known real length" };
        var unit = new ComboBox { ItemsSource = new[] { "mm", "cm", "m", "px", "in", "ft" }, IsEditable = true, Text = "mm", Width = 105, ToolTip = "Measurement unit" }; row.Children.Add(input); row.Children.Add(unit);
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4), Foreground = Brushes.OrangeRed }; panel.Children.Add(error);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; panel.Children.Add(actions);
        var cancel = new Button { Content = "Cancel", IsCancel = true }; var ok = new Button { Content = "Apply calibration", IsDefault = true }; actions.Children.Add(cancel); actions.Children.Add(ok);
        double length = 0;
        ok.Click += (_, _) => { if (!CalibrationInput.TryRead(input.Text, unit.Text, pixels, out length)) { error.Text = "Enter a finite positive length and a unit, for example 3500 mm."; input.Focus(); return; } dialog.DialogResult = true; };
        dialog.Loaded += (_, _) => { input.Focus(); input.SelectAll(); };
        return dialog.ShowDialog() == true ? (length, unit.Text.Trim()) : null;
    }
    private string? PromptText(string initial)
    {
        var dialog = new Window { Style = (Style)FindResource(typeof(Window)), Title = "SnapMark · Text", Width = 430, Height = 245, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize };
        var panel = new StackPanel { Margin = new Thickness(16) }; var input = new TextBox { Text = initial, AcceptsReturn = true, Height = 115, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var ok = new Button { Content = "Apply text", IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) => dialog.DialogResult = true; panel.Children.Add(input); panel.Children.Add(ok); dialog.Content = panel;
        dialog.Loaded += (_, _) => { input.Focus(); input.SelectAll(); };
        return dialog.ShowDialog() == true ? input.Text.Trim() : null;
    }
    private void RequestColorPick(ColorPicker picker)
    {
        surface.CancelColorPick(); pickingColor = picker; picker.IsPicking = true;
        surface.BeginColorPick(value => picker.Value = value);
        UpdateStatus("Pick color → " + picker.Label + " · คลิกสีบนภาพ · Esc ยกเลิก");
    }
    private void KeyPressed(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape && surface.IsPickingColor) { surface.CancelColorPick(); e.Handled = true; return; }
        if (e.Key == Key.Delete && inlineHistory?.SelectedEntry is not null && (inlineHistory.IsPreviewing || inlineHistory.IsKeyboardFocusWithin))
        {
            _ = inlineHistory.RecycleAsync(); e.Handled = true; return;
        }
        if (inlineHistory?.IsPreviewing == true)
        {
            if (e.Key == Key.Escape) { inlineHistory.Back(); e.Handled = true; }
            else if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { if (inlineHistory.SelectedImage is not null) _ = Copy(); e.Handled = true; }
            else if (e.Key != Key.Tab && e.Key != Key.Enter && e.Key != Key.Space && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) e.Handled = true;
            return;
        }
        // Let focused inputs keep their text editing / navigation keys.
        for (var target = e.OriginalSource as DependencyObject; target is not null; target = VisualTreeHelper.GetParent(target))
            if (target is System.Windows.Controls.Primitives.TextBoxBase or ComboBox) return;
        var ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        if (ctrl)
        {
            switch (e.Key)
            {
                case Key.K: new CommandPalette(this, ToolCatalog.Tools.Where(t => t.Tool != EditorTool.Eyedropper).Select(t => new PaletteCommand(t.Name, () => SetTool(t.Tool))).Concat(new[] { new PaletteCommand("Copy image", () => _ = Copy()), new PaletteCommand("Export image", Save), new PaletteCommand("Save editable project", SaveProject), new PaletteCommand("Undo", () => surface.History.Undo()), new PaletteCommand("Redo", () => surface.History.Redo()) })).Show(); break;
                case Key.C: _ = Copy(); break;
                case Key.S: Save(); break;
                case Key.Z: surface.CancelGesture(); surface.History.Undo(); break;
                case Key.Y: surface.CancelGesture(); surface.History.Redo(); break;
                case Key.D: surface.DuplicateSelection(); break;
                case Key.V: PasteImage(); break;
                default: return;
            }
            e.Handled = true; return;
        }
        var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
        switch (e.Key)
        {
            case Key.Escape: surface.CancelGesture(); SetTool(EditorTool.Select); break;
            case Key.Delete: surface.DeleteSelection(); break;
            case Key.Left: surface.MoveSelection(-step, 0); break;
            case Key.Right: surface.MoveSelection(step, 0); break;
            case Key.Up: surface.MoveSelection(0, -step); break;
            case Key.Down: surface.MoveSelection(0, step); break;
            case Key.V: SetTool(EditorTool.Select); break;
            case Key.A: SetTool(EditorTool.Arrow); break;
            case Key.R: SetTool(EditorTool.Rectangle); break;
            case Key.T: SetTool(EditorTool.Text); break;
            case Key.H: SetTool(EditorTool.Highlight); break;
            case Key.N: SetTool(EditorTool.Number); break;
            case Key.B: SetTool(EditorTool.Blur); break;
            case Key.P: SetTool(EditorTool.Pixelate); break;
            case Key.X: SetTool(EditorTool.Redact); break;
            case Key.S: SetTool(EditorTool.Stamp); break;
            case Key.F2: surface.EditSelectedText(); break;
            case Key.L: SetTool(EditorTool.Line); break;
            case Key.Enter: surface.FinishPath(); break;
            default: return;
        }
        e.Handled = true;
    }
}
