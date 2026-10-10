using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using SnapMark.Capture;
using SnapMark.Core;

namespace SnapMark.App;

// Four non-activating strips sit outside the captured pixels, so GDI capture
// never records the frame. They remain visible above ordinary desktop windows.
public sealed class RecordingFrame : IDisposable
{
    [DllImport("user32.dll")] private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll")] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    private readonly List<Window> strips = [];
    public static IReadOnlyList<PixelRect> Strips(PixelRect b, PixelRect desktop, int thickness = 3) => new[]
    {
        new PixelRect(b.X - thickness, b.Y - thickness, b.Width + thickness * 2, thickness),
        new PixelRect(b.X - thickness, b.Bottom, b.Width + thickness * 2, thickness),
        new PixelRect(b.X - thickness, b.Y, thickness, b.Height),
        new PixelRect(b.Right, b.Y, thickness, b.Height)
    }.Select(r => r.Intersect(desktop)).Where(r => r.Width > 0 && r.Height > 0).ToArray();
    public void Show(PixelRect bounds)
    {
        foreach (var rect in Strips(bounds, NativeDesktop.VirtualBounds))
        {
            var window = new Window { Title = "SnapMark · Recording boundary", WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, AllowsTransparency = true, Background = new SolidColorBrush(Color.FromArgb(155, 34, 211, 238)), ShowInTaskbar = false, ShowActivated = false, Topmost = true, IsHitTestVisible = false, Focusable = false };
            window.SourceInitialized += (_, _) => { var hwnd = new WindowInteropHelper(window).Handle; SetWindowLongPtr(hwnd, -20, new IntPtr(GetWindowLongPtr(hwnd, -20).ToInt64() | 0x20 | 0x80 | 0x08000000)); NativeDesktop.Position(window, rect); };
            strips.Add(window); window.Show();
        }
    }
    public void Dispose() { foreach (var window in strips) window.Close(); strips.Clear(); }
}
