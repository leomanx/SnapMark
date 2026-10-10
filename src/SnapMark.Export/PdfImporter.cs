using System.IO;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SnapMark.Export;
public static class PdfImporter
{
    public static async Task<uint> PageCount(string path) => (await Open(path)).PageCount;
    private static async Task<PdfDocument> Open(string path)
    {
        if (new FileInfo(path).Length > 256_000_000) throw new InvalidDataException("PDF import limit: 256 MB.");
        return await PdfDocument.LoadFromFileAsync(await StorageFile.GetFileFromPathAsync(Path.GetFullPath(path)));
    }
    public static async Task<BitmapSource> RenderPage(string path, int pageNumber, int dpi = 150)
    {
        if (dpi < 72 || dpi > 300) throw new ArgumentException("PDF DPI must be 72–300.");
        var document = await Open(path); if (pageNumber < 1 || pageNumber > document.PageCount) throw new ArgumentException("Page number outside PDF.");
        using var page = document.GetPage((uint)(pageNumber - 1));
        var width = (uint)Math.Ceiling(page.Size.Width * dpi / 96d); var height = (uint)Math.Ceiling(page.Size.Height * dpi / 96d);
        if (width < 1 || height < 1 || width > 16000 || height > 16000 || (long)width * height > 64_000_000) throw new ArgumentException("Rendered PDF exceeds 64 megapixels / 16000px. Choose a lower DPI.");
        using var stream = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(stream, new PdfPageRenderOptions { DestinationWidth = width, DestinationHeight = height });
        stream.Seek(0); using var buffer = new MemoryStream(); await stream.AsStreamForRead().CopyToAsync(buffer); buffer.Position = 0;
        var bitmap = BitmapDecoder.Create(buffer, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0]; bitmap.Freeze(); return bitmap;
    }
}
