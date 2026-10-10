namespace SnapMark.Core;
public static class OutputNaming
{
    public static string Create(string pattern, string kind, DateTime time, string app = "capture", long counter = 1)
    {
        app = string.Concat(app.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim().TrimEnd('.');
        var name = pattern.Replace("{date}", time.ToString("yyyy-MM-dd")).Replace("{time}", time.ToString("HHmmss_fff")).Replace("{kind}", kind).Replace("{app}", app.Length == 0 ? "capture" : app).Replace("{counter}", counter.ToString("D4"));
        if (string.IsNullOrWhiteSpace(name) || name.Length > 160 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains('{') || name.Contains('}') || name.EndsWith('.') || name.EndsWith(' ')) throw new ArgumentException("Filename: use {date}, {time}, {kind}, {app}, {counter}; no folder separators or invalid filename characters.");
        var device = name.Split('.')[0].ToUpperInvariant();
        if (device is "CON" or "PRN" or "AUX" or "NUL" || device.Length == 4 && (device.StartsWith("COM") || device.StartsWith("LPT")) && device[3] is >= '1' and <= '9') throw new ArgumentException("Reserved Windows filename.");
        return name;
    }
}
