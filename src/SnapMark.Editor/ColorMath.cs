using System.Globalization;
using System.Windows.Media;

namespace SnapMark.Editor;
public static class ColorMath
{
    public static Color FromHsv(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360; saturation = Math.Clamp(saturation, 0, 1); value = Math.Clamp(value, 0, 1);
        var c = value * saturation; var x = c * (1 - Math.Abs(hue / 60 % 2 - 1)); var m = value - c;
        var (r, g, b) = hue switch { < 60 => (c, x, 0d), < 120 => (x, c, 0d), < 180 => (0d, c, x), < 240 => (0d, x, c), < 300 => (x, 0d, c), _ => (c, 0d, x) };
        return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }
    public static (double Hue, double Saturation, double Value) ToHsv(Color color)
    {
        var r = color.R / 255d; var g = color.G / 255d; var b = color.B / 255d;
        var max = Math.Max(r, Math.Max(g, b)); var min = Math.Min(r, Math.Min(g, b)); var d = max - min;
        var h = d == 0 ? 0 : max == r ? 60 * ((g - b) / d % 6) : max == g ? 60 * ((b - r) / d + 2) : 60 * ((r - g) / d + 4);
        return ((h + 360) % 360, max == 0 ? 0 : d / max, max);
    }
    public static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    public static bool TryHex(string? input, out Color color)
    {
        color = Colors.Black; var s = input?.Trim().TrimStart('#');
        if (s is { Length: 3 }) s = string.Concat(s.Select(c => new string(c, 2)));
        if (s is not { Length: 6 } || !uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb)) return false;
        color = Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb); return true;
    }
}
