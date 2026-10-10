namespace SnapMark.Core;

public enum RegionHandle { None, Move, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }
public static class RecordingRegionGeometry
{
    public static IReadOnlyList<(RegionHandle Handle, PixelPoint Point)> Handles(PixelRect r) =>
    [ (RegionHandle.TopLeft, new(r.X, r.Y)), (RegionHandle.Top, new(r.X + r.Width / 2, r.Y)),
      (RegionHandle.TopRight, new(r.Right, r.Y)), (RegionHandle.Right, new(r.Right, r.Y + r.Height / 2)),
      (RegionHandle.BottomRight, new(r.Right, r.Bottom)), (RegionHandle.Bottom, new(r.X + r.Width / 2, r.Bottom)),
      (RegionHandle.BottomLeft, new(r.X, r.Bottom)), (RegionHandle.Left, new(r.X, r.Y + r.Height / 2)) ];
    public static RegionHandle Hit(PixelRect r, PixelPoint p, double radius = 10)
    {
        foreach (var item in Handles(r)) if (Math.Abs(p.X - item.Point.X) <= radius && Math.Abs(p.Y - item.Point.Y) <= radius) return item.Handle;
        return r.Contains(p) ? RegionHandle.Move : RegionHandle.None;
    }
    public static PixelRect Drag(PixelRect original, RegionHandle handle, PixelPoint start, PixelPoint end, PixelRect desktop)
    {
        var dx = Math.Round(end.X - start.X); var dy = Math.Round(end.Y - start.Y);
        if (handle == RegionHandle.Move) return new(Math.Clamp(original.X + dx, desktop.X, desktop.Right - original.Width), Math.Clamp(original.Y + dy, desktop.Y, desktop.Bottom - original.Height), original.Width, original.Height);
        var left = original.X; var right = original.Right; var top = original.Y; var bottom = original.Bottom;
        if (handle is RegionHandle.Left or RegionHandle.TopLeft or RegionHandle.BottomLeft) left = Math.Clamp(left + dx, desktop.X, right - 2);
        if (handle is RegionHandle.Right or RegionHandle.TopRight or RegionHandle.BottomRight) right = Math.Clamp(right + dx, left + 2, desktop.Right);
        if (handle is RegionHandle.Top or RegionHandle.TopLeft or RegionHandle.TopRight) top = Math.Clamp(top + dy, desktop.Y, bottom - 2);
        if (handle is RegionHandle.Bottom or RegionHandle.BottomLeft or RegionHandle.BottomRight) bottom = Math.Clamp(bottom + dy, top + 2, desktop.Bottom);
        return new(left, top, right - left, bottom - top);
    }
}
