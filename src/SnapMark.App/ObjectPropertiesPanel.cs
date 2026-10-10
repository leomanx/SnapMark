using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SnapMark.Core;
using SnapMark.Editor;

namespace SnapMark.App;

public sealed class ObjectPropertiesPanel : ScrollViewer
{
    private readonly EditorSurface surface;
    private bool refreshing, editing;
    private readonly Action<ColorPicker>? requestPick;
    public ObjectPropertiesPanel(EditorSurface surface, Action<ColorPicker>? requestPick = null)
    {
        this.requestPick = requestPick;
        this.surface = surface; Width = 278; VerticalScrollBarVisibility = ScrollBarVisibility.Auto; HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        Background = (Brush)Application.Current.FindResource("PanelBrush");
        surface.SelectionChanged += Refresh; Refresh();
    }
    public void Refresh()
    {
        if (editing) return;
        var selected = surface.Document.Annotations.Where(a => surface.Selection.Contains(a.Id)).ToArray();
        Visibility = surface.Tool == EditorTool.Select && selected.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (Visibility != Visibility.Visible) return;
        refreshing = true;
        try
        {
            var a = selected.FirstOrDefault(x => x.Id == surface.SelectedId) ?? selected[0];
            var panel = new StackPanel { Margin = new Thickness(10) }; Content = panel;
            void Heading(string text) => panel.Children.Add(new TextBlock { Text = text, Foreground = (Brush)Application.Current.FindResource("AccentBrush"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 14, 4, 6), TextWrapping = TextWrapping.Wrap });
            void Change(Func<Annotation, Annotation> change, Func<AnnotationKind, bool>? supports = null)
            {
                if (refreshing) return; editing = true;
                try { surface.ChangeSelectedProperty(change, supports); }
                finally { editing = false; }
            }
            bool All(Func<AnnotationKind, bool> supports) => selected.All(x => supports(x.Kind));
            bool Measurement(AnnotationKind k) => k is AnnotationKind.Dimension or AnnotationKind.Distance or AnnotationKind.Length or AnnotationKind.Area or AnnotationKind.Angle;
            void Color(string label, string value, Func<Annotation, string, Annotation> change, Func<AnnotationKind, bool> supports)
            {
                Heading(label); var picker = new ColorPicker(value, label, requestPick); picker.Changed += v => Change(x => change(x, v), supports); panel.Children.Add(picker);
            }
            void Numeric(string label, double value, double minimum, double maximum, Func<Annotation, double, Annotation> change, Func<AnnotationKind, bool>? supports = null)
            {
                panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(4, 8, 4, 0) });
                var input = new TextBox { Text = value.ToString("0.##", CultureInfo.InvariantCulture), Tag = label, ToolTip = $"{label} · {minimum}–{maximum} · Enter or leave field to apply" }; panel.Children.Add(input);
                input.PreviewKeyDown += (_, e) => { if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control) && e.Key is System.Windows.Input.Key.Z or System.Windows.Input.Key.Y) { if (e.Key == System.Windows.Input.Key.Z) surface.History.Undo(); else surface.History.Redo(); e.Handled = true; } };
                void Apply() { if (refreshing) return; if (double.TryParse(input.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var v) || double.TryParse(input.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) { if (double.IsFinite(v) && v >= minimum && v <= maximum) { input.BorderBrush = Brushes.Gray; Change(x => change(x, v), supports); return; } } input.BorderBrush = Brushes.OrangeRed; }
                input.LostKeyboardFocus += (_, _) => Apply(); input.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) { Apply(); e.Handled = true; } };
            }
            void Percent(string label, double value, Func<Annotation, double, Annotation> change, Func<AnnotationKind, bool>? supports = null) => Numeric(label + " (%)", value * 100, 0, 100, (x, v) => change(x, v / 100), supports);
            void Choice<T>(string label, T value, Func<Annotation, T, Annotation> change, Func<AnnotationKind, bool>? supports = null) where T : struct, Enum
            {
                panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(4, 8, 4, 0) }); var combo = new ComboBox { ItemsSource = Enum.GetValues<T>(), SelectedItem = value }; panel.Children.Add(combo); combo.SelectionChanged += (_, _) => { if (combo.SelectedItem is T v) Change(x => change(x, v), supports); };
            }
            Heading("OBJECT PROPERTIES / แก้ไขวัตถุ");
            panel.Children.Add(new TextBlock { Text = selected.Length == 1 ? a.Kind.ToString() : $"{selected.Length} objects · shared properties", Margin = new Thickness(4), TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(new TextBlock { Text = "เปลี่ยนแล้วใช้ทันที · Ctrl+Z ย้อนกลับ\nช่องตัวเลข: Enter หรือคลิกช่องถัดไป\nหลายวัตถุ: แสดงค่าของวัตถุล่าสุด", FontSize = 11, Margin = new Thickness(4), TextWrapping = TextWrapping.Wrap });
            if (All(AnnotationProperties.Font)) Numeric("Font size (px)", a.FontSize, 1, 1000, (x, v) => x with { FontSize = v }, AnnotationProperties.Font);
            if (All(AnnotationProperties.Color)) Color("STROKE / TEXT COLOR", a.Color, (x, v) => x with { Color = v }, AnnotationProperties.Color);
            if (selected.All(x => x.Kind != AnnotationKind.Redact && !AnnotationProperties.Effect(x.Kind))) Percent("Object opacity", a.Opacity, (x, v) => x with { Opacity = v });
            if (All(AnnotationProperties.Stroke))
            {
                Heading("BORDER / LINE"); Numeric("Line weight (px)", a.Thickness, .1, 100, (x, v) => x with { Thickness = v }, AnnotationProperties.Stroke);
                Percent("Border opacity", a.StrokeOpacity, (x, v) => x with { StrokeOpacity = v }, AnnotationProperties.Stroke);
                Choice("Line type", a.LinePattern, (x, v) => x with { LinePattern = v }, AnnotationProperties.Stroke);
                if (selected.All(x => x.Kind == AnnotationKind.Line)) { Choice("Start marker", a.StartMarker, (x, v) => x with { StartMarker = v }); Choice("End marker", a.EndMarker, (x, v) => x with { EndMarker = v }); }
            }
            if (selected.All(x => AnnotationProperties.Fill(x.Kind) && !(x.Kind == AnnotationKind.Symbol && x.Variant is "north" or "slope" or "check" or "cross" or "centerline" or "axis" or "break")))
            {
                Color("FILL / BACKGROUND COLOR", a.FillColor ?? ((a.Kind == AnnotationKind.Symbol && a.Variant != "level") || a.Kind is AnnotationKind.Note or AnnotationKind.Stamp ? "#FFFFFF" : a.Color), (x, v) => x with { FillColor = v }, AnnotationProperties.Fill);
                Percent("Fill opacity", a.FillOpacity ?? (a.Kind == AnnotationKind.Highlight ? .35 : 1), (x, v) => x with { FillOpacity = v }, AnnotationProperties.Fill);
                if (All(AnnotationProperties.Pattern)) { Choice("Fill type", a.Fill, (x, v) => x with { Fill = v }, AnnotationProperties.Pattern); Color("GRADIENT SECOND COLOR", a.SecondColor, (x, v) => x with { SecondColor = v }, AnnotationProperties.Pattern); }
                if (selected.All(x => x.Kind is AnnotationKind.Polyline or AnnotationKind.Freehand)) { var loop = new CheckBox { Content = "Closed loop", IsChecked = a.Closed, Foreground = (Brush)Application.Current.FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(loop); loop.Click += (_, _) => Change(x => x with { Closed = loop.IsChecked == true }); }
            }
            if (All(AnnotationProperties.Font))
            {
                Heading("FONT / LABEL STYLE");
                if (selected.All(x => x.Kind is not (AnnotationKind.OpenDot or AnnotationKind.FilledDot))) Color("LABEL COLOR", a.TextColor ?? (a.Kind is AnnotationKind.Number or AnnotationKind.Callout ? "#FFFFFF" : a.Color), (x, v) => x with { TextColor = v }, AnnotationProperties.Font);
                var family = new ComboBox { ItemsSource = Fonts.SystemFontFamilies.Select(f => f.Source).Append(a.FontFamily).Distinct().Order().ToArray(), SelectedItem = a.FontFamily, ToolTip = "Font family" }; panel.Children.Add(family); family.SelectionChanged += (_, _) => { if (family.SelectedItem is string v) Change(x => x with { FontFamily = v }, AnnotationProperties.Font); };
                var bold = new CheckBox { Content = "Bold", IsChecked = a.Bold, Foreground = (Brush)Application.Current.FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(bold); bold.Click += (_, _) => Change(x => x with { Bold = bold.IsChecked == true }, AnnotationProperties.Font);
                var italic = new CheckBox { Content = "Italic / ตัวเอียง", IsChecked = a.Italic, Foreground = (Brush)Application.Current.FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(italic); italic.Click += (_, _) => Change(x => x with { Italic = italic.IsChecked == true }, AnnotationProperties.Font);
                var underline = new CheckBox { Content = "Underline / ขีดเส้นใต้", IsChecked = a.Underline, Foreground = (Brush)Application.Current.FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(underline); underline.Click += (_, _) => Change(x => x with { Underline = underline.IsChecked == true }, AnnotationProperties.Font);
                if (selected.All(x => x.Kind is not (AnnotationKind.OpenDot or AnnotationKind.FilledDot)))
                {
                    Numeric("Text box width (px) · 0 = Auto", a.TextWidth, 0, 100000, (x, v) => x with { TextWidth = v }, AnnotationProperties.Font);
                    var actualAlignment = a.TextWidth == 0 && a.Kind is AnnotationKind.Symbol or AnnotationKind.Number or AnnotationKind.Callout ? TextAlign.Center : a.TextAlign;
                    Choice("Text alignment", actualAlignment, (x, v) => x with { TextAlign = v, TextWidth = x.TextWidth > 0 ? x.TextWidth : Math.Max(80, AnnotationRenderer.GetBounds(x).Width + x.FontSize * 2) }, AnnotationProperties.Font);
                }
            }
            if (All(AnnotationProperties.Text))
            {
                if (selected.All(x => x.Kind == AnnotationKind.Symbol)) { panel.Children.Add(new TextBlock { Text = "Symbol type", Margin = new Thickness(4, 8, 4, 0) }); var variant = new ComboBox { ItemsSource = ToolCatalog.Symbols, SelectedItem = a.Variant }; panel.Children.Add(variant); variant.SelectionChanged += (_, _) => { if (variant.SelectedItem is string v) Change(x => x with { Variant = v }); }; }
                panel.Children.Add(new TextBlock { Text = "Text / label / prefix", Margin = new Thickness(4, 8, 4, 0) }); var label = new TextBox { Text = a.Text, AcceptsReturn = true, MinHeight = 60, TextWrapping = TextWrapping.Wrap }; panel.Children.Add(label);
                var apply = new Button { Content = "Apply label" }; panel.Children.Add(apply); apply.Click += (_, _) => Change(x => x with { Text = label.Text }, AnnotationProperties.Text);
            }
            if (selected.All(x => x.Kind == AnnotationKind.Text || Measurement(x.Kind)))
            {
                Heading("TEXT BACKGROUND"); var enabled = new CheckBox { Content = "Enable background", IsChecked = a.TextBackground, Foreground = (Brush)Application.Current.FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(enabled); enabled.Click += (_, _) => Change(x => x with { TextBackground = enabled.IsChecked == true });
                Color("Background color", a.TextBackgroundColor, (x, v) => x with { TextBackgroundColor = v }, k => k == AnnotationKind.Text || Measurement(k));
                Percent("Background opacity", a.TextBackgroundOpacity, (x, v) => x with { TextBackgroundOpacity = v }); Numeric("Padding (px)", a.TextPadding, 0, 64, (x, v) => x with { TextPadding = v });
            }
            if (All(Measurement))
            {
                Heading("MEASUREMENT FORMAT / รูปแบบตัวเลข");
                var decimals = new ComboBox { ItemsSource = Enumerable.Range(0, 7), SelectedItem = a.MeasureDecimals, ToolTip = "จำนวนตำแหน่งทศนิยม 0–6" }; panel.Children.Add(new TextBlock { Text = "Decimal places / ทศนิยม", Margin = new Thickness(4) }); panel.Children.Add(decimals);
                decimals.SelectionChanged += (_, _) => { if (decimals.SelectedItem is int n) Change(x => x with { MeasureDecimals = n }, Measurement); };
                var unit = new CheckBox { Content = "Show unit / แสดงหน่วย", IsChecked = a.MeasureShowUnit, Foreground = (Brush)Application.Current.FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(unit); unit.Click += (_, _) => Change(x => x with { MeasureShowUnit = unit.IsChecked == true }, Measurement);
                void Affix(string heading, string value, bool prefix)
                {
                    panel.Children.Add(new TextBlock { Text = heading, Margin = new Thickness(4) }); var input = new TextBox { Text = value, MaxLength = 160 }; panel.Children.Add(input);
                    var apply = new Button { Content = "Apply / ใช้ข้อความ" }; panel.Children.Add(apply); apply.Click += (_, _) => Change(x => prefix ? x with { MeasurePrefix = input.Text } : x with { MeasureSuffix = input.Text }, Measurement);
                }
                Affix("Prefix / คำหน้า เช่น ≈ ", a.MeasurePrefix, true); Affix("Suffix / คำท้าย เช่น  (ตรวจหน้างาน)", a.MeasureSuffix, false);
                panel.Children.Add(new TextBlock { Text = "Dimension ที่ใส่ข้อความเอง: แก้ที่ Text / label · ทศนิยมและหน่วยใช้กับค่าคำนวณอัตโนมัติ (ล้าง label เพื่อใช้ค่าที่วัด)", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4), FontSize = 11 });
            }
            if (All(AnnotationProperties.Effect)) { Heading("REDACTION EFFECT"); Numeric("Blur radius / pixel block (px)", a.EffectSize, 1, 128, (x, v) => x with { EffectSize = v }, AnnotationProperties.Effect); }
            if (selected.All(x => x.Kind != AnnotationKind.Redact && !AnnotationProperties.Effect(x.Kind)))
            {
                Heading("TRANSFORM / กลับด้านและหมุน");
                void Transform(Func<Annotation, Annotation> update) { Change(update); Refresh(); }
                var mirror = new WrapPanel(); panel.Children.Add(mirror);
                var horizontal = new Button { Content = "Mirror ↔ / ซ้าย–ขวา", ToolTip = "กลับด้านตามแกนนอนของวัตถุ · กดซ้ำเพื่อคืน" }; var vertical = new Button { Content = "Mirror ↕ / บน–ล่าง", ToolTip = "กลับด้านตามแกนตั้งของวัตถุ · กดซ้ำเพื่อคืน" }; mirror.Children.Add(horizontal); mirror.Children.Add(vertical);
                horizontal.Click += (_, _) => Transform(x => x with { MirrorHorizontal = !x.MirrorHorizontal }); vertical.Click += (_, _) => Transform(x => x with { MirrorVertical = !x.MirrorVertical });
                var guides = new WrapPanel(); panel.Children.Add(guides);
                foreach (var degrees in new[] { 0, 45, 90, 180, 270 }) { var guide = new Button { Content = degrees + "°", ToolTip = "ตั้งองศาเป็น " + degrees + "°" }; guides.Children.Add(guide); guide.Click += (_, _) => Transform(x => x with { Rotation = degrees }); }
                var steps = new WrapPanel(); panel.Children.Add(steps);
                foreach (var delta in new[] { -45, 45 }) { var step = new Button { Content = (delta > 0 ? "+" : "") + delta + "°", ToolTip = "หมุนเพิ่มจากองศาปัจจุบัน" }; steps.Children.Add(step); step.Click += (_, _) => Transform(x => x with { Rotation = (x.Rotation + delta + 360) % 360 }); }
                Numeric("Rotation (degrees)", a.Rotation, -360, 360, (x, v) => x with { Rotation = v });
            }
            if (selected.Any(x => x.Kind == AnnotationKind.Redact)) panel.Children.Add(new TextBlock { Text = "Solid cover stays opaque black.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) });
        }
        finally { refreshing = false; }
    }
}
