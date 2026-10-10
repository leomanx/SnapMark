using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SnapMark.Core;
using SnapMark.Editor;

namespace SnapMark.App;

/// <summary>Uses the same symbol geometry as the editor and exported image.</summary>
public sealed class SymbolToolIcon : FrameworkElement
{
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(SymbolToolIcon), new FrameworkPropertyMetadata(Brushes.SlateGray, FrameworkPropertyMetadataOptions.AffectsRender));
    public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    private readonly string symbol;
    public SymbolToolIcon(string symbol)
    {
        this.symbol = symbol; Width = Height = 28; IsHitTestVisible = false;
        SetBinding(StrokeProperty, new Binding("Foreground") { RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1) });
    }
    protected override void OnRender(DrawingContext dc)
    {
        var color = (Stroke as SolidColorBrush)?.Color.ToString() ?? "#64748B";
        AnnotationRenderer.Draw(dc, new Annotation(Guid.Empty, AnnotationKind.Symbol, new(3, 3), new(25, 25), Color: color, Thickness: 1.5, FontSize: 9, Variant: symbol, FillOpacity: 0));
    }
}
