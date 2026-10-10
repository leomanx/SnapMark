using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SnapMark.Capture;
using SnapMark.Core;
namespace SnapMark.App;
public sealed class CursorOverlayWindow : Window
{
    [DllImport("user32.dll")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll")] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly List<(PixelPoint Point, DateTime Time)> clicks = [];
    private bool wasDown;
    private readonly PixelRect bounds;
    private readonly bool highlight, ripple;
    public CursorOverlayWindow(PixelRect bounds, bool highlight, bool ripple)
    {
        this.bounds = bounds; this.highlight = highlight; this.ripple = ripple;
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent; ShowInTaskbar = false; ShowActivated = false; Topmost = true; IsHitTestVisible = false;
        SourceInitialized += (_, _) => { var hwnd = new WindowInteropHelper(this).Handle; var style = GetWindowLongPtr(hwnd, -20).ToInt64(); SetWindowLongPtr(hwnd, -20, new IntPtr(style | 0x20 | 0x80 | 0x08000000)); NativeDesktop.Position(this, bounds); };
        timer.Tick += (_, _) => { var down = (GetAsyncKeyState(1) & 0x8000) != 0; if (ripple && down && !wasDown) clicks.Add((NativeDesktop.Cursor, DateTime.UtcNow)); wasDown = down; clicks.RemoveAll(c => (DateTime.UtcNow - c.Time).TotalSeconds > .6); InvalidateVisual(); };
        Loaded += (_, _) => timer.Start(); Closed += (_, _) => timer.Stop();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc); var cursor = NativeDesktop.Cursor; var sx = ActualWidth / bounds.Width; var sy = ActualHeight / bounds.Height;
        Point Local(PixelPoint p) => new((p.X - bounds.X) * sx, (p.Y - bounds.Y) * sy);
        if (highlight && bounds.Contains(cursor)) dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80, 255, 220, 0)), null, Local(cursor), 18 * sx, 18 * sy);
        foreach (var click in clicks) { var progress = Math.Clamp((DateTime.UtcNow - click.Time).TotalSeconds / .6, 0, 1); dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb((byte)(180 * (1 - progress)), 255, 90, 40)), 2), Local(click.Point), (12 + progress * 30) * sx, (12 + progress * 30) * sy); }
    }
}
