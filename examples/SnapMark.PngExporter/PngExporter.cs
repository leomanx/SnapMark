using SnapMark.Core;
namespace SnapMark.Examples;
public sealed class PngExporter : ISnapMarkExporter
{
    public string Name => "Example · Flattened PNG";
    public string Extension => ".png";
    public void Export(FlattenedImage image, string path) => File.WriteAllBytes(path, image.Png);
}
