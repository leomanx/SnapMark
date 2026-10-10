using System.Globalization;

namespace SnapMark.Core;

public static class CalibrationInput
{
    public static bool TryRead(string value, string unit, double pixels, out double length)
    {
        length = 0;
        return double.IsFinite(pixels) && pixels >= 2 && unit.Trim().Length is > 0 and <= 16
            && !unit.Any(char.IsControl)
            && (double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out length)
                || double.TryParse(value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out length))
            && double.IsFinite(length) && length > 0 && double.IsFinite(length / pixels) && length / pixels > 0;
    }
}
