using System.Windows.Media.Imaging;
using SnapMark.Core;

namespace SnapMark.Capture;

public sealed record CaptureRequest(CaptureMode Mode, IntPtr Window = default, bool AllowFreeform = true, bool ConfirmRegion = false);
public sealed record CaptureResult(BitmapSource Image, DateTimeOffset Timestamp, CaptureMode Mode, PixelRect SourceBounds);
public interface ICaptureService { Task<CaptureResult?> CaptureAsync(CaptureRequest request); }
public sealed class CaptureService : ICaptureService
{
    private PixelRect? lastRegion;
    public async Task<CaptureResult?> CaptureAsync(CaptureRequest request)
    {
        var desktop = NativeDesktop.VirtualBounds;
        PixelRect bounds;
        BitmapSource image;
        if (request.Mode is CaptureMode.Region or CaptureMode.WindowPick || request.ConfirmRegion)
        {
            var frozen = NativeDesktop.Snapshot(desktop);
            PixelRect? initial = request.ConfirmRegion ? request.Mode switch { CaptureMode.Monitor => NativeDesktop.Monitors.FirstOrDefault(m => m.Contains(NativeDesktop.Cursor)), CaptureMode.AllScreens => desktop, _ => null } : null;
            var region = await RegionOverlay.SelectAsync(frozen, desktop, request.Mode == CaptureMode.WindowPick, request.AllowFreeform, request.ConfirmRegion, initial);
            if (region is null) return null;
            bounds = region.Bounds.Intersect(desktop);
            image = NativeDesktop.Crop(frozen, new(bounds.X - desktop.X, bounds.Y - desktop.Y, bounds.Width, bounds.Height));
            if (region.Polygon is { } polygon) image = FreeformMask.Apply(image, polygon.Select(p => new PixelPoint(p.X - bounds.X, p.Y - bounds.Y)).ToArray());
            lastRegion = bounds;
        }
        else
        {
            bounds = request.Mode switch
            {
                CaptureMode.ActiveWindow => NativeDesktop.WindowBounds(request.Window),
                CaptureMode.Monitor => NativeDesktop.Monitors.FirstOrDefault(m => m.Contains(NativeDesktop.Cursor)),
                CaptureMode.RepeatRegion => lastRegion?.Intersect(desktop) ?? throw new InvalidOperationException("Capture a region first."),
                _ => desktop
            };
            image = NativeDesktop.Snapshot(bounds);
        }
        return new(image, DateTimeOffset.Now, request.Mode, bounds);
    }
}
