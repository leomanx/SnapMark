namespace SnapMark.Core;
public sealed record FlattenedImage(byte[] Png, int Width, int Height);
// v1 deliberately gives exporters only the flattened image, never originals or history.
public interface ISnapMarkExporter
{
    string Name { get; }
    string Extension { get; }
    void Export(FlattenedImage flattenedImage, string path);
}
