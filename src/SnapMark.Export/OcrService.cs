using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Media.Imaging;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace SnapMark.Export;

public sealed record OcrLanguage(string Tag, string Name);
public static class LocalOcr
{
    public static IReadOnlyList<OcrLanguage> Languages() => OcrEngine.AvailableRecognizerLanguages.Select(l => new OcrLanguage(l.LanguageTag, l.DisplayName)).ToArray();
    public static async Task<string> ReadAsync(BitmapSource image, string language)
    {
        var engine = (language == "auto" ? OcrEngine.TryCreateFromUserProfileLanguages() : OcrEngine.TryCreateFromLanguage(new Language(language))) ?? throw new InvalidOperationException("This OCR language is not installed in Windows. Add its OCR language pack in Windows Settings.");
        var scale = Math.Min(1, OcrEngine.MaxImageDimension / (double)Math.Max(image.PixelWidth, image.PixelHeight));
        if (scale < 1) { image = new TransformedBitmap(image, new System.Windows.Media.ScaleTransform(scale, scale)); image.Freeze(); }
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
        using var bytes = new MemoryStream(); encoder.Save(bytes);
        using var stream = new InMemoryRandomAccessStream();
        bytes.Position = 0; var writer = stream.AsStreamForWrite(); await bytes.CopyToAsync(writer); await writer.FlushAsync(); stream.Seek(0);
        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        var result = await engine.RecognizeAsync(bitmap);
        return string.Join(Environment.NewLine, result.Lines.Select(l => l.Text));
    }
}
