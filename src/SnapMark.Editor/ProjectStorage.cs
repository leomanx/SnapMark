using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;
using SnapMark.Core;

namespace SnapMark.Editor;

public sealed record LoadedProject(BitmapSource Image, EditorDocument Document);
public static class ProjectStorage
{
    private sealed record SavedAsset(Guid Id, int Width, int Height);
    private sealed record Manifest(int Version, int Width, int Height, PixelRect? Crop, double UnitsPerPixel, string Unit, List<Annotation> Annotations, List<SavedAsset> Assets);
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    public static void Save(string path, BitmapSource image, EditorDocument document)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var zip = ZipFile.Open(temporary, ZipArchiveMode.Create))
            {
                using (var stream = zip.CreateEntry("image.png").Open())
                {
                    // Do not read decoder-owned metadata when saving a frozen image on an autosave worker.
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image, null, null, null));
                    using var buffer = new MemoryStream(); encoder.Save(buffer); buffer.Position = 0; buffer.CopyTo(stream);
                }
                var used = document.Annotations.Where(a => a.AssetId is not null).Select(a => a.AssetId!.Value).Distinct().ToArray();
                var assets = used.Select(id => new SavedAsset(id, document.Assets[id].Width, document.Assets[id].Height)).ToList();
                foreach (var id in used) { using var stream = zip.CreateEntry($"assets/{id:N}.png").Open(); stream.Write(document.Assets[id].Png); }
                using var metadata = zip.CreateEntry("document.json").Open();
                JsonSerializer.Serialize(metadata, new Manifest(1, document.Width, document.Height, document.Crop, document.UnitsPerPixel, document.Unit, document.Annotations, assets), Json);
            }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static LoadedProject Load(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        if (zip.Entries.Sum(e => e.Length) > 256_000_000) throw new InvalidDataException("Project exceeds the 256 MB uncompressed limit.");
        using var metadata = (zip.GetEntry("document.json") ?? throw new InvalidDataException("Missing document metadata.")).Open();
        var manifest = JsonSerializer.Deserialize<Manifest>(metadata, Json) ?? throw new InvalidDataException("Invalid document metadata.");
        if (manifest.Version != 1) throw new InvalidDataException("Unsupported SnapMark project version.");
        if (manifest.Width < 1 || manifest.Height < 1 || manifest.Width > 16000 || manifest.Height > 16000 || (long)manifest.Width * manifest.Height > 64_000_000 || manifest.Annotations.Count > 10000 || manifest.UnitsPerPixel <= 0 || !double.IsFinite(manifest.UnitsPerPixel)) throw new InvalidDataException("Invalid canvas or calibration.");
        using var imageStream = (zip.GetEntry("image.png") ?? throw new InvalidDataException("Missing original image.")).Open();
        using var imageBuffer = new MemoryStream(); imageStream.CopyTo(imageBuffer); imageBuffer.Position = 0;
        var image = BitmapDecoder.Create(imageBuffer, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0]; image.Freeze();
        var document = new EditorDocument(manifest.Width, manifest.Height) { Crop = manifest.Crop, UnitsPerPixel = manifest.UnitsPerPixel, Unit = manifest.Unit };
        if (document.Width < image.PixelWidth || document.Height < image.PixelHeight) throw new InvalidDataException("Canvas does not fit the original image.");
        foreach (var asset in manifest.Assets)
        {
            using var stream = (zip.GetEntry($"assets/{asset.Id:N}.png") ?? throw new InvalidDataException("Missing image layer.")).Open();
            using var buffer = new MemoryStream(); stream.CopyTo(buffer); document.Assets[asset.Id] = new(buffer.ToArray(), asset.Width, asset.Height);
        }
        foreach (var a in manifest.Annotations)
        {
            if (!Enum.IsDefined(a.Kind) || !double.IsFinite(a.Start.X) || !double.IsFinite(a.Start.Y) || !double.IsFinite(a.End.X) || !double.IsFinite(a.End.Y) || !double.IsFinite(a.Thickness) || a.Thickness <= 0 || !double.IsFinite(a.FontSize) || a.FontSize <= 0 || !double.IsFinite(a.Rotation) || !double.IsFinite(a.Opacity) || a.Opacity < 0 || a.Opacity > 1 || !double.IsFinite(a.EffectSize) || a.Points?.Count > 100000 || a.Points?.Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y)) == true) throw new InvalidDataException("Invalid annotation geometry.");
            if (!double.IsFinite(a.TextPadding) || a.TextPadding < 0 || a.TextPadding > 64 || !double.IsFinite(a.TextBackgroundOpacity) || a.TextBackgroundOpacity < 0 || a.TextBackgroundOpacity > 1) throw new InvalidDataException("Invalid text background settings.");
            if (a.MeasureDecimals < 0 || a.MeasureDecimals > 6 || a.MeasurePrefix.Length > 160 || a.MeasureSuffix.Length > 160) throw new InvalidDataException("Invalid measurement label format.");
            if (!double.IsFinite(a.TextWidth) || a.TextWidth < 0 || a.TextWidth > 100000) throw new InvalidDataException("Invalid text box width.");
            if (!double.IsFinite(a.StrokeOpacity) || a.StrokeOpacity < 0 || a.StrokeOpacity > 1 || a.FillOpacity is { } fillOpacity && (!double.IsFinite(fillOpacity) || fillOpacity < 0 || fillOpacity > 1)) throw new InvalidDataException("Invalid stroke/fill opacity.");
            if (a.FillColor is not null && !ColorMath.TryHex(a.FillColor, out _)) throw new InvalidDataException("Invalid fill color.");
            if (a.TextColor is not null && !ColorMath.TryHex(a.TextColor, out _)) throw new InvalidDataException("Invalid label color.");
            try { System.Windows.Media.ColorConverter.ConvertFromString(a.Color); System.Windows.Media.ColorConverter.ConvertFromString(a.SecondColor); System.Windows.Media.ColorConverter.ConvertFromString(a.TextBackgroundColor); } catch { throw new InvalidDataException("Invalid annotation color."); }
            if (a.AssetId is { } id && !document.Assets.ContainsKey(id)) throw new InvalidDataException("Missing annotation asset.");
            document.Annotations.Add(a);
        }
        if (document.Crop is { } crop && (!double.IsFinite(crop.X) || !double.IsFinite(crop.Y) || !double.IsFinite(crop.Width) || !double.IsFinite(crop.Height) || crop.Width < 1 || crop.Height < 1 || crop != crop.Intersect(new(0, 0, document.Width, document.Height)))) throw new InvalidDataException("Invalid crop bounds.");
        return new(image, document);
    }
}
