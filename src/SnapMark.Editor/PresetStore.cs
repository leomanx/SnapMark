using System.IO;
using System.Text.Json;
using SnapMark.Core;

namespace SnapMark.Editor;
public sealed record UserStylePreset(string Name, string Color, string SecondColor, double Thickness, double FontSize, double Opacity,
    LinePattern LinePattern, ShapeFill Fill, double Rotation, EndpointMarker StartMarker, EndpointMarker EndMarker, double EffectSize, string StampText, int PolygonSides, bool ClosedPath, string FontFamily = "Segoe UI", bool Bold = false, TextAlign TextAlign = TextAlign.Left, bool TextBackground = false, string TextBackgroundColor = "#FFFFFF", double TextBackgroundOpacity = 1, double TextPadding = 8, string? FillColor = null, double? FillOpacity = null, double StrokeOpacity = 1, string? TextColor = null);
public sealed class PresetStore(string path)
{
    public IReadOnlyList<UserStylePreset> Load()
    {
        if (!File.Exists(path)) return [];
        if (new FileInfo(path).Length > 1_000_000) throw new InvalidDataException("Preset file exceeds 1 MB.");
        var items = JsonSerializer.Deserialize<List<UserStylePreset>>(File.ReadAllText(path)) ?? throw new InvalidDataException("Invalid preset file.");
        if (items.Count > 128) throw new InvalidDataException("Maximum 128 custom presets.");
        foreach (var item in items) Validate(item); return items;
    }
    public void Save(UserStylePreset preset)
    {
        Validate(preset); var items = Load().Where(p => !p.Name.Equals(preset.Name, StringComparison.OrdinalIgnoreCase)).Append(preset).ToArray();
        if (items.Length > 128) throw new InvalidDataException("Maximum 128 custom presets.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!); var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temp, JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true })); File.Move(temp, path, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private static void Validate(UserStylePreset p)
    {
        if (p.FillColor is not null && !ColorMath.TryHex(p.FillColor, out _) || p.TextColor is not null && !ColorMath.TryHex(p.TextColor, out _) || !double.IsFinite(p.StrokeOpacity) || p.StrokeOpacity < 0 || p.StrokeOpacity > 1 || p.FillOpacity is { } alpha && (!double.IsFinite(alpha) || alpha < 0 || alpha > 1)) throw new InvalidDataException("Invalid preset fill/stroke properties.");
        if (string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 60 || !ColorMath.TryHex(p.Color, out _) || !ColorMath.TryHex(p.SecondColor, out _) || !ColorMath.TryHex(p.TextBackgroundColor, out _) || !double.IsFinite(p.TextPadding) || p.TextPadding < 0 || p.TextPadding > 64 || !double.IsFinite(p.TextBackgroundOpacity) || p.TextBackgroundOpacity < 0 || p.TextBackgroundOpacity > 1 ||
            !double.IsFinite(p.Thickness) || p.Thickness <= 0 || p.Thickness > 100 || !double.IsFinite(p.FontSize) || p.FontSize <= 0 || p.FontSize > 1000 ||
            !double.IsFinite(p.Opacity) || p.Opacity < 0 || p.Opacity > 1 || !double.IsFinite(p.Rotation) || !double.IsFinite(p.EffectSize) || p.EffectSize <= 0 || p.PolygonSides is < 3 or > 32 ||
            !Enum.IsDefined(p.LinePattern) || !Enum.IsDefined(p.Fill) || !Enum.IsDefined(p.StartMarker) || !Enum.IsDefined(p.EndMarker) || p.StampText is null || p.StampText.Length > 1000) throw new InvalidDataException("Invalid preset properties.");
    }
}
