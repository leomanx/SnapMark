using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Editor;

namespace SnapMark.App;

public sealed class ColorPicker : StackPanel
{
    private readonly Border swatch;
    private readonly TextBox hex;
    private readonly Button wheel;
    private readonly Button pick;
    public string Label { get; }
    public bool IsPicking { set { pick.Background = value ? Brushes.DarkCyan : (Brush)FindResource("ButtonBrush"); } }
    private bool updating;
    private string value = "#EF4444";
    private static readonly List<string> recent = [];
    public event Action<string>? Changed;
    public string Value
    {
        get => value;
        set { if (!ColorMath.TryHex(value, out var c)) return; this.value = ColorMath.Hex(c); updating = true; hex.Text = this.value; swatch.Background = new SolidColorBrush(c); hex.BorderBrush = Brushes.Gray; updating = false; Changed?.Invoke(this.value); }
    }
    public ColorPicker(string initial, string label, Action<ColorPicker>? requestPick = null)
    {
        Label = label;
        var row = new DockPanel(); Children.Add(row);
        pick = new Button { Width = 34, Padding = new Thickness(5), IsEnabled = requestPick is not null };
        ToolIcon.SetButton(pick, "eyedropper", null, "Pick color from image → " + label + " · Esc ยกเลิก");
        System.Windows.Automation.AutomationProperties.SetName(pick, "Pick color: " + label);
        DockPanel.SetDock(pick, Dock.Right); row.Children.Add(pick);
        pick.Click += (_, _) => requestPick?.Invoke(this);
        wheel = new Button { Content = "◉", ToolTip = "Open color wheel · " + label, Padding = new Thickness(8), Width = 38 };
        DockPanel.SetDock(wheel, Dock.Right); row.Children.Add(wheel);
        swatch = new Border { Width = 24, Height = 24, Margin = new Thickness(6), BorderBrush = Brushes.White, BorderThickness = new Thickness(1) };
        DockPanel.SetDock(swatch, Dock.Left); row.Children.Add(swatch);
        hex = new TextBox { ToolTip = label + " HEX: #RRGGBB or #RGB", MaxLength = 7 }; row.Children.Add(hex);
        hex.TextChanged += (_, _) => { if (updating) return; if (ColorMath.TryHex(hex.Text, out var c)) { value = ColorMath.Hex(c); swatch.Background = new SolidColorBrush(c); hex.BorderBrush = Brushes.Gray; Changed?.Invoke(value); } else hex.BorderBrush = Brushes.OrangeRed; };
        wheel.Click += (_, _) =>
        {
            var dialog = new ColorWheelWindow(Value, recent) { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true) { Value = dialog.SelectedHex; recent.Remove(Value); recent.Insert(0, Value); if (recent.Count > 10) recent.RemoveAt(10); }
        };
        var samples = new WrapPanel { Margin = new Thickness(4, 0, 4, 4) }; Children.Add(samples);
        foreach (var sample in new[] { "#EF4444", "#F59E0B", "#FACC15", "#22C55E", "#38BDF8", "#A855F7", "#FFFFFF", "#000000" })
        {
            ColorMath.TryHex(sample, out var c);
            var button = new Button { Content = new Border { Width = 17, Height = 17, Background = new SolidColorBrush(c), BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) }, Padding = new Thickness(2), Margin = new Thickness(2), ToolTip = sample };
            button.Click += (_, _) => Value = sample; samples.Children.Add(button);
        }
        Value = initial;
    }
}

public sealed class ColorWheelWindow : Window
{
    public string SelectedHex { get; private set; }
    public ColorWheelWindow(string initial, IReadOnlyList<string> recent)
    {
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Color wheel"; Width = 380; Height = 590;
        ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SelectedHex = initial; ColorMath.TryHex(initial, out var initialColor);
        var root = new StackPanel { Margin = new Thickness(16) }; Content = root;
        root.Children.Add(new TextBlock { Text = "Color wheel · Hue / Saturation", Margin = new Thickness(4), FontSize = 16 });
        var disc = new ColorDisc(initialColor) { HorizontalAlignment = HorizontalAlignment.Center }; root.Children.Add(disc);
        var brightnessText = new TextBlock { Margin = new Thickness(4) }; root.Children.Add(brightnessText);
        var brightness = new Slider { Minimum = 0, Maximum = 1, Value = ColorMath.ToHsv(initialColor).Value, Margin = new Thickness(8), ToolTip = "Brightness" }; root.Children.Add(brightness);
        var row = new DockPanel(); root.Children.Add(row);
        var preview = new Border { Width = 58, Height = 38, Margin = new Thickness(4), BorderBrush = Brushes.White, BorderThickness = new Thickness(1) }; DockPanel.SetDock(preview, Dock.Left); row.Children.Add(preview);
        var hex = new TextBox { Text = initial, ToolTip = "HEX #RRGGBB", MaxLength = 7 }; row.Children.Add(hex);
        var rgb = new TextBlock { Margin = new Thickness(4), Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush") }; root.Children.Add(rgb);
        var apply = new Button { Content = "Use color", IsDefault = true };
        var syncing = false;
        void Refresh()
        {
            var color = disc.SelectedColor; SelectedHex = ColorMath.Hex(color); preview.Background = new SolidColorBrush(color);
            syncing = true; hex.Text = SelectedHex; brightness.Value = disc.Brightness; syncing = false;
            brightnessText.Text = $"Brightness · {disc.Brightness * 100:0}%"; rgb.Text = $"RGB  {color.R}, {color.G}, {color.B}";
            apply.IsEnabled = true; hex.BorderBrush = Brushes.Gray;
        }
        disc.Changed += Refresh;
        brightness.ValueChanged += (_, _) => { if (!syncing) { disc.Brightness = brightness.Value; Refresh(); } };
        hex.TextChanged += (_, _) => { if (syncing) return; if (ColorMath.TryHex(hex.Text, out var c)) { disc.SetColor(c); SelectedHex = ColorMath.Hex(c); preview.Background = new SolidColorBrush(c); syncing = true; brightness.Value = disc.Brightness; syncing = false; brightnessText.Text = $"Brightness · {disc.Brightness * 100:0}%"; rgb.Text = $"RGB  {c.R}, {c.G}, {c.B}"; apply.IsEnabled = true; hex.BorderBrush = Brushes.Gray; } else { apply.IsEnabled = false; hex.BorderBrush = Brushes.OrangeRed; } };
        var recentRow = new WrapPanel(); root.Children.Add(new TextBlock { Text = "Recent colors · this session", Margin = new Thickness(4), Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush") }); root.Children.Add(recentRow);
        foreach (var item in recent)
        {
            ColorMath.TryHex(item, out var c); var b = new Button { Content = new Border { Width = 18, Height = 18, Background = new SolidColorBrush(c) }, Padding = new Thickness(3), Margin = new Thickness(2), ToolTip = item };
            b.Click += (_, _) => { disc.SetColor(c); Refresh(); }; recentRow.Children.Add(b);
        }
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right }; root.Children.Add(buttons);
        var cancel = new Button { Content = "Cancel", IsCancel = true }; buttons.Children.Add(cancel); buttons.Children.Add(apply);
        apply.Click += (_, _) => DialogResult = true;
        Refresh();
    }
}

public sealed class ColorDisc : FrameworkElement
{
    private double hue, saturation, brightness;
    private readonly BitmapSource bitmap;
    public event Action? Changed;
    public Color SelectedColor => ColorMath.FromHsv(hue, saturation, brightness);
    public double Brightness { get => brightness; set { brightness = Math.Clamp(value, 0, 1); InvalidateVisual(); } }
    public ColorDisc(Color initial)
    {
        Width = Height = 240; Cursor = Cursors.Cross; Focusable = true;
        const int size = 240; var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
        {
            var dx = x - 120d; var dy = y - 120d; var s = Math.Sqrt(dx * dx + dy * dy) / 118;
            if (s > 1) continue; var c = ColorMath.FromHsv(Math.Atan2(dy, dx) * 180 / Math.PI, s, 1); var i = (y * size + x) * 4;
            pixels[i] = c.B; pixels[i + 1] = c.G; pixels[i + 2] = c.R; pixels[i + 3] = 255;
        }
        bitmap = BitmapSource.Create(size, size, 96, 96, PixelFormats.Bgra32, null, pixels, size * 4); bitmap.Freeze(); SetColor(initial);
    }
    public void SetColor(Color color) { (hue, saturation, brightness) = ColorMath.ToHsv(color); InvalidateVisual(); }
    private void Pick(Point p)
    {
        var dx = p.X - 120; var dy = p.Y - 120;
        hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360; saturation = Math.Clamp(Math.Sqrt(dx * dx + dy * dy) / 118, 0, 1);
        InvalidateVisual(); Changed?.Invoke();
    }
    protected override void OnMouseDown(MouseButtonEventArgs e) { if (e.ChangedButton != MouseButton.Left) return; Focus(); CaptureMouse(); Pick(e.GetPosition(this)); e.Handled = true; }
    protected override void OnMouseMove(MouseEventArgs e) { if (IsMouseCaptured) Pick(e.GetPosition(this)); }
    protected override void OnMouseUp(MouseButtonEventArgs e) { if (e.ChangedButton == MouseButton.Left && IsMouseCaptured) { Pick(e.GetPosition(this)); ReleaseMouseCapture(); e.Handled = true; } }
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawEllipse(Brushes.Black, null, new Point(120, 120), 118, 118);
        dc.PushOpacity(brightness); dc.DrawImage(bitmap, new Rect(0, 0, 240, 240)); dc.Pop();
        var p = new Point(120 + Math.Cos(hue * Math.PI / 180) * saturation * 118, 120 + Math.Sin(hue * Math.PI / 180) * saturation * 118);
        dc.DrawEllipse(null, new Pen(Brushes.Black, 4), p, 5, 5); dc.DrawEllipse(null, new Pen(Brushes.White, 2), p, 5, 5);
    }
}
