using System.Windows.Input;
using System.Windows.Interop;
using WpfKey = System.Windows.Input.Key;

namespace SnapMark.Capture;
public sealed record HotkeyGesture(uint Modifiers, uint Key, string Label)
{
    public static HotkeyGesture Parse(string text)
    {
        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) throw new ArgumentException("Enter a shortcut, for example Ctrl+Shift+F8.");
        uint modifiers = 0;
        foreach (var part in parts[..^1])
        {
            var bit = part.ToLowerInvariant() switch { "ctrl" or "control" => 2u, "alt" => 1u, "shift" => 4u, _ => throw new ArgumentException("Modifiers: Ctrl, Alt, Shift.") };
            if ((modifiers & bit) != 0) throw new ArgumentException("Repeated shortcut modifier."); modifiers |= bit;
        }
        var keyText = parts[^1]; if (keyText.Equals("PrintScreen", StringComparison.OrdinalIgnoreCase)) keyText = "Snapshot";
        if (keyText.Length == 1 && char.IsDigit(keyText[0])) keyText = "D" + keyText;
        if (!Enum.TryParse<WpfKey>(keyText, true, out var key) || key is WpfKey.None or WpfKey.LeftCtrl or WpfKey.RightCtrl or WpfKey.LeftAlt or WpfKey.RightAlt or WpfKey.LeftShift or WpfKey.RightShift || KeyInterop.VirtualKeyFromKey(key) == 0) throw new ArgumentException("Unknown shortcut key: " + parts[^1]);
        var label = string.Concat((modifiers & 2) != 0 ? "Ctrl+" : "", (modifiers & 1) != 0 ? "Alt+" : "", (modifiers & 4) != 0 ? "Shift+" : "") + (key == System.Windows.Input.Key.Snapshot ? "PrintScreen" : parts[^1].ToUpperInvariant());
        return new(modifiers, (uint)KeyInterop.VirtualKeyFromKey(key), label);
    }
}
