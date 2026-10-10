using System.Windows;

namespace SnapMark.App;

/// <summary>Hide existing windows only while selecting a recording region, then restore once.</summary>
public sealed class CaptureWindowVisibility : IDisposable
{
    private readonly Window[] windows;
    private readonly HashSet<Window> closed = [];
    private bool restored;
    public CaptureWindowVisibility(IEnumerable<Window> windows)
    {
        this.windows = windows.Where(w => w.IsVisible).Distinct().ToArray();
        foreach (var window in this.windows) { window.Closed += WindowClosed; window.Hide(); }
    }
    private void WindowClosed(object? sender, EventArgs e) { if (sender is Window window) closed.Add(window); }
    public void Restore()
    {
        if (restored) return;
        restored = true;
        foreach (var window in windows)
        {
            window.Closed -= WindowClosed;
            if (closed.Contains(window)) continue;
            var activate = window.ShowActivated;
            try { window.ShowActivated = false; window.Show(); }
            finally { window.ShowActivated = activate; }
        }
    }
    public void Dispose() => Restore();
    public static void ReturnToEditor(Window? editor, Window main)
    {
        var target = editor ?? main;
        if (target.WindowState == WindowState.Minimized) target.WindowState = WindowState.Normal;
        target.Show(); target.Activate();
    }
}
