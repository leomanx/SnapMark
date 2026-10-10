using System.Windows;
using System.Windows.Media;
using SnapMark.Core;

namespace SnapMark.Editor;

/// <summary>Window selection encloses object bounds; crossing selection touches them.</summary>
public static class MarqueeHitTest
{
    public static bool Matches(Annotation annotation, PixelPoint start, PixelPoint end)
    {
        var region = PixelRect.Between(start, end);
        if (region.Width < 1 || region.Height < 1) return false;
        var bounds = AnnotationRenderer.GetBounds(annotation);
        var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        var rotation = AnnotationRenderer.ObjectTransform(annotation);
        var corners = new[] { new Point(bounds.X, bounds.Y), new Point(bounds.Right, bounds.Y), new Point(bounds.Right, bounds.Bottom), new Point(bounds.X, bounds.Bottom) };
        if (end.X >= start.X)
            return corners.Select(rotation.Transform).All(p => region.Contains(new(p.X, p.Y)));

        Geometry geometry;
        if (annotation.Kind == AnnotationKind.Line)
            geometry = new LineGeometry(new(annotation.Start.X, annotation.Start.Y), new(annotation.End.X, annotation.End.Y))
                .GetWidenedPathGeometry(new Pen(Brushes.Black, Math.Max(1, annotation.Thickness)));
        else
            geometry = new RectangleGeometry(new(bounds.X, bounds.Y, Math.Max(1, bounds.Width), Math.Max(1, bounds.Height)));
        geometry.Transform = rotation;
        return new RectangleGeometry(new(region.X, region.Y, region.Width, region.Height))
            .FillContainsWithDetail(geometry) != IntersectionDetail.Empty;
    }
}
