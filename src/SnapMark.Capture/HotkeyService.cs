using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SnapMark.Capture;

public interface IHotkeyService : IDisposable { bool Register(int id, uint modifiers, uint key, Action action); }
public sealed class HotkeyService : IHotkeyService
{
    [DllImport("user32.dll", SetLastError = true)] private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    private readonly HwndSource source = new(new HwndSourceParameters("SnapMark.Hotkeys") { ParentWindow = new IntPtr(-3) });
    private readonly Dictionary<int, Action> actions = [];
    public HotkeyService() => source.AddHook(Hook);
    public bool Register(int id, uint modifiers, uint key, Action action)
    {
        if (!RegisterHotKey(source.Handle, id, modifiers | 0x4000, key)) return false;
        actions[id] = action; return true;
    }
    private IntPtr Hook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x0312 && actions.TryGetValue(wParam.ToInt32(), out var action)) { handled = true; action(); }
        return IntPtr.Zero;
    }
    public void Clear() { foreach (var id in actions.Keys) UnregisterHotKey(source.Handle, id); actions.Clear(); }
    public void Dispose() { Clear(); source.Dispose(); }
}
