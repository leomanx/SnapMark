using System.IO;
using System.Windows.Media.Imaging;
using SnapMark.Core;

namespace SnapMark.Editor;
public sealed record RecoveryEntry(Guid Id, string Path, DateTime UpdatedUtc, long Bytes);
public sealed class RecoveryStore(string folder)
{
    public string PathFor(Guid id) => Path.Combine(folder, $"{id:N}.snapmark");
    public IReadOnlyList<RecoveryEntry> Entries() => !Directory.Exists(folder) ? [] : Directory.EnumerateFiles(folder, "*.snapmark")
        .Select(path => new FileInfo(path)).Where(f => Guid.TryParseExact(Path.GetFileNameWithoutExtension(f.Name), "N", out _))
        .OrderByDescending(f => f.LastWriteTimeUtc).Select(f => new RecoveryEntry(Guid.ParseExact(Path.GetFileNameWithoutExtension(f.Name), "N"), f.FullName, f.LastWriteTimeUtc, f.Length)).ToArray();
    public static EditorDocument Snapshot(EditorDocument source)
    {
        var copy = new EditorDocument(source.Width, source.Height) { Crop = source.Crop, Unit = source.Unit, UnitsPerPixel = source.UnitsPerPixel };
        copy.Annotations.AddRange(source.Annotations.Select(a => a with { Points = a.Points?.ToArray() }));
        foreach (var asset in source.Assets) copy.Assets.Add(asset.Key, asset.Value);
        return copy;
    }
    public void Save(Guid id, BitmapSource image, EditorDocument snapshot) { Directory.CreateDirectory(folder); ProjectStorage.Save(PathFor(id), image, snapshot); }
    public LoadedProject Load(Guid id) => ProjectStorage.Load(PathFor(id));
    public void Remove(Guid id) { var path = PathFor(id); if (File.Exists(path)) File.Delete(path); }
}
