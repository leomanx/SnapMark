using System.Globalization;
using System.Text.RegularExpressions;

namespace SnapMark.App;

public enum PrintUnit { Millimeter, Inch, FootInch }
public sealed record PhotoPaper(string Name, double WidthMm, double HeightMm)
{
    public override string ToString() => Name;
    public static readonly IReadOnlyList<PhotoPaper> Sizes = new PhotoPaper[]
    {
        new("A0",841,1189), new("A1",594,841), new("A2",420,594), new("A3",297,420), new("A4",210,297), new("A5",148,210), new("A6",105,148),
        new("ISO B0",1000,1414), new("ISO B1",707,1000), new("ISO B2",500,707), new("ISO B3",353,500), new("ISO B4",250,353), new("ISO B5",176,250), new("ISO B6",125,176),
        new("Letter",215.9,279.4), new("Legal",215.9,355.6), new("Tabloid",279.4,431.8)
    };
}
public static class PrintUnits
{
    public static double ParseMm(string value, PrintUnit unit)
    {
        value = value.Trim(); double result;
        if (unit == PrintUnit.FootInch && value.Contains('\''))
        {
            var parts = value.Split('\''); if (parts.Length != 2) throw new ArgumentException("Use feet and inches, for example 1' 2.5\".");
            var inches = parts[1].Trim().TrimStart('-').Trim().TrimEnd('"').Trim();
            result = Number(parts[0].Trim()) * 304.8 + (inches.Length > 0 ? Number(inches) * 25.4 : 0);
        }
        else { result = Number(value.TrimEnd('"')); if (unit != PrintUnit.Millimeter) result *= 25.4; }
        if (!double.IsFinite(result) || result < 0) throw new ArgumentException("Length must be finite and nonnegative."); return result;
    }
    private static double Number(string value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return number;
        var fraction = Regex.Match(value.Trim(), "^(?:(\\d+)\\s+)?(\\d+)/(\\d+)$");
        if (!fraction.Success || double.Parse(fraction.Groups[3].Value, CultureInfo.InvariantCulture) == 0) throw new ArgumentException("Enter a length, decimal or fraction, for example 1/8 or 2 1/2.");
        return (fraction.Groups[1].Success ? double.Parse(fraction.Groups[1].Value, CultureInfo.InvariantCulture) : 0) + double.Parse(fraction.Groups[2].Value, CultureInfo.InvariantCulture) / double.Parse(fraction.Groups[3].Value, CultureInfo.InvariantCulture);
    }
    public static string FormatMm(double value, PrintUnit unit)
    {
        if (unit == PrintUnit.Millimeter) return value.ToString("0.###", CultureInfo.InvariantCulture);
        if (unit == PrintUnit.Inch) return (value / 25.4).ToString("0.####", CultureInfo.InvariantCulture);
        var total = value / 25.4; var feet = (int)(total / 12); return $"{feet}' {(total - feet * 12).ToString("0.####", CultureInfo.InvariantCulture)}\"";
    }
    public static double Dip(double mm) => mm * 96 / 25.4;
}
