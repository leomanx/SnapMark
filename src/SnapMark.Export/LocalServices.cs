using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using SnapMark.Core;

namespace SnapMark.Export;

public interface IClipboardService { Task CopyAsync(BitmapSource image); }
public sealed class ClipboardService : IClipboardService
{
    public async Task CopyAsync(BitmapSource image)
    {
        for (var attempt = 0; ; attempt++)
        {
            try { Clipboard.SetImage(image); return; }
            catch (System.Runtime.InteropServices.COMException) when (attempt < 4) { await Task.Delay(75 * (attempt + 1)); }
        }
    }
}
public interface IExportService { void SavePng(BitmapSource image, string path); }
public sealed class ExportService : IExportService
{
    public void SavePng(BitmapSource image, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
            using (var stream = File.Create(temporary)) encoder.Save(stream);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
public sealed class SettingsService(string folder) : ISettingsService
{
    public AppSettings Load()
    {
        var path = Path.Combine(folder, "settings.json");
        if (!File.Exists(path)) return new();
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new();
            settings.HistoryLimit = Math.Clamp(settings.HistoryLimit, 1, 100);
            settings.CaptureDelaySeconds = Math.Clamp(settings.CaptureDelaySeconds, 0, 10);
            settings.HistoryDays = settings.HistoryDays is 7 or 30 ? settings.HistoryDays : 0;
            settings.OutputCounter = Math.Clamp(settings.OutputCounter, 0, long.MaxValue - 1);
            settings.Profiles ??= [];
            settings.Profiles = settings.Profiles.Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Name) && p.Name.Length <= 60 && p.Applications is not null && p.FilenamePattern is not null && p.ExportFolder is not null && p.Color is not null && p.StylePresetName is not null).Take(50).ToList();
            settings.Theme = settings.Theme is "Dark" or "Light" or "System" ? settings.Theme : "Dark";
            settings.Hotkeys ??= new AppSettings().Hotkeys;
            foreach (var binding in new AppSettings().Hotkeys) settings.Hotkeys.TryAdd(binding.Key, binding.Value);
            try { OutputNaming.Create(settings.FileNamePattern, "image", DateTime.Now); } catch { settings.FileNamePattern = new AppSettings().FileNamePattern; }
            if (string.IsNullOrWhiteSpace(settings.ExportFolder)) settings.ExportFolder = new AppSettings().ExportFolder;
            try { settings.ExportFolder = StoragePaths.Validate(settings.ExportFolder); } catch { settings.ExportFolder = new AppSettings().ExportFolder; }
            try { settings.VideoFolder = StoragePaths.Validate(settings.VideoFolder); } catch { settings.VideoFolder = new AppSettings().VideoFolder; }
            try { settings.ProjectFolder = StoragePaths.Validate(settings.ProjectFolder); } catch { settings.ProjectFolder = new AppSettings().ProjectFolder; }
            try { if (!string.IsNullOrWhiteSpace(settings.HistoryFolder)) settings.HistoryFolder = StoragePaths.Validate(settings.HistoryFolder); } catch { settings.HistoryFolder = ""; }
            try { if (!string.IsNullOrWhiteSpace(settings.RecoveryFolder)) settings.RecoveryFolder = StoragePaths.Validate(settings.RecoveryFolder); } catch { settings.RecoveryFolder = ""; }
            try { System.Windows.Media.ColorConverter.ConvertFromString(settings.DefaultColor); }
            catch { settings.DefaultColor = "#EF4444"; }
            return settings;
        }
        catch (JsonException) { return new(); }
    }
    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "settings.json");
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, path, true);
    }
}
public sealed record HistoryEntry(string Path, DateTimeOffset Timestamp, int Width, int Height, string SourceApp = "", string WindowTitle = "", string Name = "")
{
    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Timestamp.ToLocalTime().ToString("dd MMM HH:mm:ss") : Name;
}
public sealed class HistoryService(string folder, IExportService export)
{
    public string FolderPath => Path.GetFullPath(folder);
    public HistoryEntry Rename(HistoryEntry entry, string name)
    {
        var current = Entries().FirstOrDefault(e => string.Equals(e.Path, entry.Path, StringComparison.OrdinalIgnoreCase)) ?? throw new ArgumentException("Capture is no longer in the configured history folder.");
        if (new[] { current.Path, current.Path + ".json" }.Any(p => File.Exists(p) && File.GetAttributes(p).HasFlag(FileAttributes.ReparsePoint))) throw new IOException("Linked history files cannot be renamed.");
        name = name.Trim(); if (name.Length > 160 || name.Any(char.IsControl)) throw new ArgumentException("Use a name up to 160 characters without line breaks.");
        var updated = current with { Name = name }; var metadata = current.Path + ".json"; var temp = metadata + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(updated)); File.Move(temp, metadata, true); return updated;
    }
    public Task RecycleAsync(HistoryEntry entry, Func<IReadOnlyList<string>, Task>? send = null)
    {
        var path = Path.GetFullPath(entry.Path);
        if (!string.Equals(Path.GetDirectoryName(path), Path.TrimEndingDirectorySeparator(FolderPath), StringComparison.OrdinalIgnoreCase) || !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(path), @"^Capture_\d{8}_\d{6}_\d{3}_[0-9a-fA-F]{32}\.png$"))
            throw new ArgumentException("Only captures in the configured history folder can be recycled.");
        var paths = new[] { path, path + ".json", path + ".ocr.json" }.Where(File.Exists).ToArray();
        if (paths.Any(p => File.GetAttributes(p).HasFlag(FileAttributes.ReparsePoint))) throw new IOException("Linked history files cannot be recycled from this panel.");
        return paths.Length == 0 ? Task.CompletedTask : (send ?? RecycleBin.SendAsync)(paths);
    }
    private IEnumerable<string> CaptureFiles() => Directory.EnumerateFiles(folder, "Capture_*.png").Where(path => System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(path), @"^Capture_\d{8}_\d{6}_\d{3}_[0-9a-fA-F]{32}\.png$"));
    public IReadOnlyList<HistoryEntry> Entries()
    {
        if (!Directory.Exists(folder)) return [];
        return CaptureFiles().OrderByDescending(File.GetLastWriteTimeUtc).Select(path =>
        {
            try { var image = LoadImage(path); HistoryEntry? metadata = null;
                try { if (File.Exists(path + ".json")) metadata = JsonSerializer.Deserialize<HistoryEntry>(File.ReadAllText(path + ".json")); } catch { }
                return new HistoryEntry(path, File.GetLastWriteTimeUtc(path), image.PixelWidth, image.PixelHeight, metadata?.SourceApp ?? "", metadata?.WindowTitle ?? "", metadata?.Name ?? "");
            } catch { return null; }
        }).Where(e => e is not null).Cast<HistoryEntry>().ToArray();
    }
    public void Add(BitmapSource image, int limit, string sourceApp = "", string windowTitle = "", int days = 0)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"Capture_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}.png"); export.SavePng(image, path);
        File.WriteAllText(path + ".json", JsonSerializer.Serialize(new HistoryEntry(path, DateTimeOffset.Now, image.PixelWidth, image.PixelHeight, sourceApp, windowTitle)));
        var files = CaptureFiles().OrderByDescending(File.GetLastWriteTimeUtc).ToArray();
        foreach (var expired in files.Skip(Math.Clamp(limit, 1, 100)).Concat(days > 0 ? files.Where(f => File.GetLastWriteTimeUtc(f) < DateTime.UtcNow.AddDays(-days)) : []).Distinct())
        { File.Delete(expired); if (File.Exists(expired + ".json")) File.Delete(expired + ".json"); if (File.Exists(expired + ".ocr.json")) File.Delete(expired + ".ocr.json"); }
    }
    public static BitmapSource LoadImage(string path)
    {
        using var stream = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var image = decoder.Frames[0]; image.Freeze(); return image;
    }
}
