using System.IO;
using System.Windows.Media.Imaging;

namespace SnapMark.Editor;
public sealed record SavedSymbol(string Name, string Path);
public sealed class SymbolLibrary(string folder)
{
    public IReadOnlyList<SavedSymbol> Entries() => !Directory.Exists(folder) ? [] : Directory.EnumerateFiles(folder, "*.png").Select(p => new SavedSymbol(Path.GetFileNameWithoutExtension(p).Split("__")[0], p)).OrderBy(p => p.Name).ToArray();
    public SavedSymbol ImportSvg(string path, string color)
    {
        var image = SvgSymbolImporter.Load(path, color); Directory.CreateDirectory(folder);
        var name = Path.GetFileNameWithoutExtension(path); var destination = Path.Combine(folder, name + "__" + Guid.NewGuid().ToString("N") + ".png");
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image)); using (var stream = File.Create(destination)) encoder.Save(stream);
        return new(name, destination);
    }
    public static BitmapSource Load(SavedSymbol symbol)
    {
        using var stream = File.OpenRead(symbol.Path); var image = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0]; image.Freeze(); return image;
    }
}
