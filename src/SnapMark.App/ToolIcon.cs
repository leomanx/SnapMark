using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SnapMark.Editor;

namespace SnapMark.App;

/// <summary>Original 24-unit vector drawings, with a consistent stroke and no font/emoji dependency.</summary>
public sealed class ToolIcon : FrameworkElement
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(nameof(Kind), typeof(string), typeof(ToolIcon), new FrameworkPropertyMetadata("select", FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(ToolIcon), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));
    public string Kind { get => (string)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    public Brush? Stroke { get => (Brush?)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    private static readonly Dictionary<string, Geometry> paths = BuildPaths();
    public ToolIcon()
    {
        Width = Height = 22; IsHitTestVisible = false; UseLayoutRounding = true;
        SetBinding(StrokeProperty, new Binding("Foreground") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1) });
    }
    protected override void OnRender(DrawingContext dc)
    {
        var brush = Stroke ?? TryFindResource("TextBrush") as Brush ?? Brushes.SlateGray;
        var pen = new Pen(brush, 1.7) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        dc.PushTransform(new ScaleTransform(ActualWidth / 24, ActualHeight / 24));
        dc.DrawGeometry(Kind is "filleddot" or "plane" or "redact" ? brush : null, pen, paths.GetValueOrDefault(Kind, paths["more"]));
        dc.Pop();
    }
    private static Dictionary<string, Geometry> BuildPaths()
    {
        var data = new Dictionary<string, string>
        {
            ["capture"] = "M3,8 V3 H8 M16,3 H21 V8 M21,16 V21 H16 M8,21 H3 V16 M7,7 H17 V17 H7 Z",
            ["quick"] = "M13,2 L5,13 H11 L9,22 L19,9 H13 Z",
            ["window"] = "M3,4 H21 V20 H3 Z M3,9 H21 M6,6.5 H6.5 M9,6.5 H9.5 M13,12 L18,17 M18,13 V17 H14",
            ["monitor"] = "M3,4 H21 V16 H3 Z M8,21 H16 M12,16 V21",
            ["screens"] = "M2,5 H15 V15 H2 Z M9,10 H22 V20 H9 M5,18 H7 M17,20 V22",
            ["repeat"] = "M4,9 C6,2 18,2 20,9 M20,4 V9 H15 M20,15 C18,22 6,22 4,15 M4,20 V15 H9",
            ["scroll"] = "M5,3 H19 V21 H5 Z M12,6 V18 M9,9 L12,6 L15,9 M9,15 L12,18 L15,15",
            ["video"] = "M3,5 H16 V19 H3 Z M16,10 L22,6 V18 L16,14",
            ["trim"] = "M8,4 V20 M16,4 V20 M3,8 H21 M3,16 H21 M3,4 H5 M19,4 H21 M3,20 H5 M19,20 H21",
            ["open"] = "M3,8 V5 H10 L12,8 H21 L18,20 H2 Z M3,8 H21",
            ["paste"] = "M8,5 H4 V21 H20 V5 H16 M8,3 H16 V7 H8 Z M8,11 H16 M8,15 H14",
            ["history"] = "M4,7 A9,9 0 1 1 3,15 M3,3 V8 H8 M12,7 V12 L16,14",
            ["recover"] = "M6,3 H16 L20,7 V21 H4 V3 Z M16,3 V7 H20 M7,14 H16 M7,14 L10,11 M7,14 L10,17",
            ["team"] = "M2,17 V8 H9 L11,10 H22 V17 M7,21 V18 C7,15 17,15 17,18 V21 M12,10 A2.5,2.5 0 1 1 11.9,10",
            ["settings"] = "M10,3 H14 L15,6 L18,5 L21,9 L19,12 L21,15 L18,19 L15,18 L14,21 H10 L9,18 L6,19 L3,15 L5,12 L3,9 L6,5 L9,6 Z M12,8 A4,4 0 1 1 11.9,8",
            ["profiles"] = "M3,4 H21 M3,12 H21 M3,20 H21 M7,2 V6 M16,10 V14 M10,18 V22",
            ["diagnostics"] = "M2,12 H6 L9,5 L13,19 L16,10 L18,12 H22",
            ["plugin"] = "M8,3 V8 M16,3 V8 M5,8 H19 V11 C19,15 16,17 12,17 C8,17 5,15 5,11 Z M12,17 V22",
            ["update"] = "M4,8 V20 H20 V8 M12,2 V15 M8,11 L12,15 L16,11",
            ["select"] = "M5,3 L20,13 L13,14 L10,21 Z",
            ["copy"] = "M8,7 H21 V21 H8 Z M4,17 H3 V3 H17 V4",
            ["export"] = "M4,13 V21 H20 V13 M12,3 V16 M7,8 L12,3 L17,8",
            ["save"] = "M3,3 H18 L21,6 V21 H3 Z M7,3 V9 H17 V3 M7,21 V14 H17 V21",
            ["undo"] = "M3,10 H14 C22,10 22,21 13,21 M3,10 L9,4 M3,10 L9,16",
            ["redo"] = "M21,10 H10 C2,10 2,21 11,21 M21,10 L15,4 M21,10 L15,16",
            ["fit"] = "M3,8 V3 H8 M16,3 H21 V8 M21,16 V21 H16 M8,21 H3 V16 M8,8 H16 V16 H8 Z",
            ["actual"] = "M3,3 H21 V21 H3 Z M8,9 L10,7 V17 M15,9 L17,7 V17",
            ["minus"] = "M5,12 H19", ["plus"] = "M5,12 H19 M12,5 V19",
            ["grid"] = "M3,3 H21 V21 H3 Z M9,3 V21 M15,3 V21 M3,9 H21 M3,15 H21",
            ["image"] = "M3,3 H21 V21 H3 Z M3,18 L9,11 L14,16 L18,12 L21,15 M15,7 A2,2 0 1 1 14.9,7",
            ["crop"] = "M7,2 V17 H22 M2,7 H17 V22 M10,7 H17 V14",
            ["canvas"] = "M5,5 H19 V19 H5 Z M2,2 L7,7 M2,6 V2 H6 M22,18 V22 H18 M17,17 L22,22",
            ["ocr"] = "M3,4 H15 M9,4 V15 M5,15 H11 M16,11 A5,5 0 1 1 15.9,11 M19,19 L23,23",
            ["compare"] = "M3,4 H21 V20 H3 Z M12,2 V22 M6,9 L9,12 L6,15 M18,9 L15,12 L18,15",
            ["arrow"] = "M4,20 L20,4 M10,4 H20 V14",
            ["line"] = "M3,20 L21,4", ["rectangle"] = "M3,5 H21 V19 H3 Z",
            ["roundedrectangle"] = "M7,5 H17 Q21,5 21,9 V15 Q21,19 17,19 H7 Q3,19 3,15 V9 Q3,5 7,5 Z",
            ["ellipse"] = "M2,12 A10,7 0 1 1 22,12 A10,7 0 1 1 2,12",
            ["circle"] = "M3,12 A9,9 0 1 1 21,12 A9,9 0 1 1 3,12",
            ["triangle"] = "M12,3 L22,21 H2 Z",
            ["polygon"] = "M3,9 L9,3 L20,6 L22,16 L11,22 L3,17 Z",
            ["regularpolygon"] = "M12,2 L21,7 V17 L12,22 L3,17 V7 Z",
            ["polyline"] = "M3,19 L8,5 L16,16 L21,3",
            ["freehand"] = "M2,17 C7,3 7,3 9,12 C11,23 14,3 17,6 C20,7 17,17 22,12",
            ["opendot"] = "M6,12 A6,6 0 1 1 18,12 A6,6 0 1 1 6,12",
            ["filleddot"] = "M6,12 A6,6 0 1 1 18,12 A6,6 0 1 1 6,12",
            ["plane"] = "M4,5 H20 V19 H4 Z",
            ["highlight"] = "M5,16 L14,3 L21,8 L12,20 Z M3,22 H16 M5,16 L12,20",
            ["blur"] = "M12,2 V4 M12,20 V22 M2,12 H4 M20,12 H22 M5,5 L7,7 M17,17 L19,19 M5,19 L7,17 M17,7 L19,5 M7,12 A5,5 0 1 1 17,12 A5,5 0 1 1 7,12",
            ["pixelate"] = "M3,3 H9 V9 H3 Z M15,3 H21 V9 H15 Z M3,15 H9 V21 H3 Z M15,15 H21 V21 H15 Z",
            ["redact"] = "M3,7 H21 V17 H3 Z",
            ["text"] = "M4,4 H20 M12,4 V21 M8,21 H16",
            ["number"] = "M3,12 A9,9 0 1 1 21,12 A9,9 0 1 1 3,12 M10,9 L12,7 V17 M10,17 H14",
            ["callout"] = "M2,7 A5,5 0 1 1 12,7 A5,5 0 1 1 2,7 M11,11 L21,21 M16,21 H21 V16",
            ["note"] = "M3,3 H21 V16 L16,21 H3 Z M16,21 V16 H21 M7,8 H17 M7,12 H14",
            ["stamp"] = "M7,16 V12 H10 V6 A2,2 0 1 1 14,6 V12 H17 V16 M4,16 H20 V20 H4 Z M3,23 H21",
            ["revisioncloud"] = "M5,7 C1,3 11,1 12,5 C15,0 23,5 19,9 C24,10 23,19 18,18 C17,23 10,23 9,19 C2,23 -1,13 5,12 C1,10 2,7 5,7 Z",
            ["cloudfreeform"] = "M4,10 C1,5 7,2 10,6 C13,0 21,6 18,10 C23,13 19,21 14,17 C8,24 2,18 5,14 C1,15 0,11 4,10 Z M8,12 L16,9 L15,16 Z",
            ["dimension"] = "M4,4 V20 M20,4 V20 M4,12 H20 M4,12 L8,8 M4,12 L8,16 M20,12 L16,8 M20,12 L16,16",
            ["calibrate"] = "M3,9 L15,2 L22,15 L10,22 Z M7,7 L9,10 M11,5 L13,8 M16,7 L18,10 M18,11 L20,14",
            ["distance"] = "M3,17 L19,5 M3,12 V17 H8 M14,5 H19 V10",
            ["length"] = "M2,19 L8,4 L15,15 L22,3 M2,22 H22",
            ["area"] = "M3,7 L13,2 L22,11 L18,22 L4,19 Z M6,9 L13,6 L18,12 L15,18 L8,16 Z",
            ["angle"] = "M21,4 L4,20 H22 M9,15 Q15,15 15,20",
            ["symbol"] = "M12,2 V22 M2,12 H22 M5,12 A7,7 0 1 1 19,12 A7,7 0 1 1 5,12",
            ["eyedropper"] = "M5,15 L15,5 L19,9 L9,19 L4,20 Z M13,3 L21,11 M18,2 L22,6 L19,9",
            ["eraser"] = "M3,15 L14,3 L22,10 L12,21 H9 Z M9,9 L17,16 M12,21 H22",
            ["forward"] = "M4,8 H16 V20 H4 Z M8,4 H20 V16 M12,14 V7 M9,10 L12,7 L15,10",
            ["backward"] = "M4,8 H16 V20 H4 Z M8,4 H20 V16 M10,11 V18 M7,15 L10,18 L13,15",
            ["delete"] = "M4,6 H20 M9,6 V3 H15 V6 M6,6 L7,21 H17 L18,6 M10,10 V17 M14,10 V17",
            ["more"] = "M4,12 A1,1 0 1 1 3.9,12 M12,12 A1,1 0 1 1 11.9,12 M20,12 A1,1 0 1 1 19.9,12"
        };
        return data.ToDictionary(x => x.Key, x => { var geometry = Geometry.Parse(x.Value); geometry.Freeze(); return geometry; });
    }
    public static void SetButton(Button button, string kind, string? label, string description, bool vertical = false)
    {
        var panel = new StackPanel { Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(new ToolIcon { Kind = kind, Margin = new Thickness(0, 0, !vertical && label is not null ? 7 : 0, vertical ? 4 : 0), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        if (label is not null) panel.Children.Add(new TextBlock { Text = label, FontSize = vertical ? 10 : 12, HorizontalAlignment = vertical ? HorizontalAlignment.Center : HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center });
        button.Content = panel; button.ToolTip = description;
        AutomationProperties.SetName(button, description); ToolTipService.SetInitialShowDelay(button, 250);
    }
    public static string ShortLabel(EditorTool tool) => tool switch
    {
        EditorTool.RoundedRectangle => "Rounded", EditorTool.RegularPolygon => "Regular", EditorTool.Freehand => "Pen",
        EditorTool.OpenDot => "Ring", EditorTool.FilledDot => "Dot", EditorTool.Redact => "Cover",
        EditorTool.RevisionCloud => "Cloud", EditorTool.CloudFreeform => "Free cloud", EditorTool.Calibrate => "Scale",
        EditorTool.Symbol => "Symbol", EditorTool.Eyedropper => "Pick color", _ => tool.ToString()
    };
}
