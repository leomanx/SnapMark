using SnapMark.Core;
namespace SnapMark.Capture;
public static class SelectionGeometry
{
    public static PixelRect Adjust(PixelRect selection, PixelRect desktop, int dx, int dy, bool resize)
    {
        var width = resize ? Math.Clamp(selection.Width + dx, 2, desktop.Right - selection.X) : selection.Width;
        var height = resize ? Math.Clamp(selection.Height + dy, 2, desktop.Bottom - selection.Y) : selection.Height;
        return new(Math.Clamp(selection.X + (resize ? 0 : dx), desktop.X, desktop.Right - width),
            Math.Clamp(selection.Y + (resize ? 0 : dy), desktop.Y, desktop.Bottom - height), width, height);
    }
}
