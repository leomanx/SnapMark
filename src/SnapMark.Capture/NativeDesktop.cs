using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using SnapMark.Core;

namespace SnapMark.Capture;

public static class NativeDesktop
{
    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
    private delegate bool EnumWindowsCallback(IntPtr hwnd, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    public static void ScrollRegion(PixelRect bounds)
    {
        var previous = Cursor; var point = new POINT { X = (int)(bounds.X + bounds.Width / 2), Y = (int)(bounds.Y + bounds.Height / 2) };
        SetForegroundWindow(GetAncestor(WindowFromPoint(point), 2)); SetCursorPos(point.X, point.Y);
        mouse_event(0x0800, 0, 0, unchecked((uint)-360), UIntPtr.Zero);
        SetCursorPos((int)previous.X, (int)previous.Y);
    }
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    public static IReadOnlyList<PixelRect> VisibleWindowBounds()
    {
        var result = new List<PixelRect>();
        EnumWindows((hwnd, _) => { GetWindowThreadProcessId(hwnd, out var pid); if (!IsWindowVisible(hwnd) || pid == Environment.ProcessId) return true;
            try { var bounds = WindowBounds(hwnd); if (bounds.Width >= 20 && bounds.Height >= 20) result.Add(bounds); } catch { } return true; }, IntPtr.Zero);
        return result;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder text, int max);
    public static (string App, string Title) WindowMetadata(IntPtr hwnd)
    {
        var text = new System.Text.StringBuilder(512); GetWindowText(hwnd, text, text.Capacity);
        try { GetWindowThreadProcessId(hwnd, out var pid); using var process = System.Diagnostics.Process.GetProcessById((int)pid); return (process.ProcessName, text.ToString()); }
        catch { return ("", text.ToString()); }
    }
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out RECT rect, int size);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr handle);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    public static PixelPoint Cursor { get { GetCursorPos(out var p); return new(p.X, p.Y); } }
    public static PixelRect VirtualBounds => new(GetSystemMetrics(76), GetSystemMetrics(77), GetSystemMetrics(78), GetSystemMetrics(79));
    public static PixelRect WindowBounds(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) throw new InvalidOperationException("No active window to capture.");
        if (DwmGetWindowAttribute(hwnd, 9, out var r, Marshal.SizeOf<RECT>()) != 0 && !GetWindowRect(hwnd, out r))
            throw new InvalidOperationException("Cannot read window bounds.");
        return new PixelRect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top).Intersect(VirtualBounds);
    }
    public static IReadOnlyList<PixelRect> Monitors => System.Windows.Forms.Screen.AllScreens
        .Select(s => new PixelRect(s.Bounds.X, s.Bounds.Y, s.Bounds.Width, s.Bounds.Height)).ToArray();
    public static void Position(Window window, PixelRect bounds) => SetWindowPos(new WindowInteropHelper(window).Handle,
        new IntPtr(-1), (int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height, 0x0040);
    public static BitmapSource Snapshot(PixelRect bounds)
    {
        var width = (int)bounds.Width; var height = (int)bounds.Height;
        if (width < 1 || height < 1) throw new InvalidOperationException("Capture area is empty.");
        if (width > 16000 || height > 16000 || (long)width * height > 64_000_000) throw new InvalidOperationException("Capture exceeds 64 MP / 16000 px per side. Select a smaller monitor or area.");
        using var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            graphics.CopyFromScreen((int)bounds.X, (int)bounds.Y, 0, 0, new System.Drawing.Size(width, height), System.Drawing.CopyPixelOperation.SourceCopy);
        var handle = bitmap.GetHbitmap();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze(); return source;
        }
        finally { DeleteObject(handle); }
    }
    public static BitmapSource Crop(BitmapSource image, PixelRect bounds)
    {
        var crop = new CroppedBitmap(image, new Int32Rect((int)bounds.X, (int)bounds.Y, (int)bounds.Width, (int)bounds.Height));
        crop.Freeze(); return crop;
    }
}
