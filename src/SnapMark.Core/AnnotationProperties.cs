namespace SnapMark.Core;

public static class AnnotationProperties
{
    public static bool Text(AnnotationKind k) => k is AnnotationKind.Text or AnnotationKind.Note or AnnotationKind.Stamp or AnnotationKind.Number or AnnotationKind.Callout or AnnotationKind.Symbol or AnnotationKind.Dimension;
    public static bool Font(AnnotationKind k) => Text(k) || k is AnnotationKind.Distance or AnnotationKind.Length or AnnotationKind.Area or AnnotationKind.Angle or AnnotationKind.OpenDot or AnnotationKind.FilledDot;
    public static bool Stroke(AnnotationKind k) => k is not (AnnotationKind.Text or AnnotationKind.Highlight or AnnotationKind.Plane or AnnotationKind.FilledDot or AnnotationKind.Image or AnnotationKind.Blur or AnnotationKind.Pixelate or AnnotationKind.Redact);
    public static bool Fill(AnnotationKind k) => k is AnnotationKind.Rectangle or AnnotationKind.RoundedRectangle or AnnotationKind.Ellipse or AnnotationKind.Circle or AnnotationKind.Triangle or AnnotationKind.Polygon or AnnotationKind.Freehand or AnnotationKind.Polyline or AnnotationKind.Plane or AnnotationKind.Highlight or AnnotationKind.Symbol or AnnotationKind.Note or AnnotationKind.Stamp or AnnotationKind.Number or AnnotationKind.Callout or AnnotationKind.FilledDot or AnnotationKind.Area;
    public static bool Pattern(AnnotationKind k) => Fill(k) && k is not (AnnotationKind.Highlight or AnnotationKind.Symbol or AnnotationKind.Note or AnnotationKind.Stamp or AnnotationKind.Number or AnnotationKind.Callout or AnnotationKind.FilledDot);
    public static bool Color(AnnotationKind k) => k is not (AnnotationKind.Image or AnnotationKind.Blur or AnnotationKind.Pixelate or AnnotationKind.Redact);
    public static bool Effect(AnnotationKind k) => k is AnnotationKind.Blur or AnnotationKind.Pixelate;
}
