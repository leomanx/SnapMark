using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Recording;

namespace SnapMark.App;

public enum PhotoTone { Original, BlackWhite, Grey, Chrome, Sepia, Blueprint }
public sealed record PhotoOptions(int Width = 0, int Height = 0, bool KeepRatio = true, bool AllowUpscale = false, PhotoTone Tone = PhotoTone.Original, int JpegQuality = 90);

public static class PhotoProcessing
{
    public const string Filter = "Photos|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.gif";
    public static bool IsPhoto(string path) => new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".gif" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    public static IEnumerable<string> FolderPhotos(string folder) => Directory.EnumerateFiles(folder).Where(IsPhoto).OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
    public static BitmapSource Load(string path)
    {
        using var file = File.OpenRead(path);
        var decoder = BitmapDecoder.Create(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0]; frame.Freeze(); return frame;
    }
    public static Size OutputSize(int width, int height, PhotoOptions options)
    {
        if (options.Width < 0 || options.Height < 0 || options.Width > 16000 || options.Height > 16000 || options.JpegQuality is < 1 or > 100) throw new ArgumentException("Size must be 0–16000 px; JPEG quality must be 1–100.");
        double w = width, h = height;
        if (options.KeepRatio)
        {
            var scale = Math.Min(options.Width > 0 ? (double)options.Width / width : double.PositiveInfinity, options.Height > 0 ? (double)options.Height / height : double.PositiveInfinity);
            if (double.IsPositiveInfinity(scale)) scale = 1;
            if (!options.AllowUpscale) scale = Math.Min(1, scale);
            w *= scale; h *= scale;
        }
        else { w = options.Width > 0 ? options.Width : width; h = options.Height > 0 ? options.Height : height; if (!options.AllowUpscale) { w = Math.Min(width, w); h = Math.Min(height, h); } }
        w = Math.Max(1, Math.Round(w)); h = Math.Max(1, Math.Round(h));
        if (w * h > 64_000_000) throw new ArgumentException("Output is limited to 64 megapixels.");
        return new Size(w, h);
    }
    public static BitmapSource Transform(BitmapSource source, PhotoOptions options)
    {
        var size = OutputSize(source.PixelWidth, source.PixelHeight, options);
        var visual = new DrawingVisual(); RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        using (var dc = visual.RenderOpen()) dc.DrawImage(source, new Rect(0, 0, size.Width, size.Height));
        var resized = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32); resized.Render(visual);
        if (options.Tone == PhotoTone.Original) { resized.Freeze(); return resized; }
        var converted = new FormatConvertedBitmap(resized, PixelFormats.Bgra32, null, 0);
        var stride = converted.PixelWidth * 4; var pixels = new byte[stride * converted.PixelHeight]; converted.CopyPixels(pixels, stride, 0);
        for (var i = 0; i < pixels.Length; i += 4)
        {
            var y = .2126 * pixels[i + 2] + .7152 * pixels[i + 1] + .0722 * pixels[i];
            var v = (byte)Math.Clamp(Math.Round(y), 0, 255);
            switch (options.Tone)
            {
                case PhotoTone.BlackWhite: v = y >= 128 ? (byte)255 : (byte)0; pixels[i] = pixels[i + 1] = pixels[i + 2] = v; break;
                case PhotoTone.Grey: pixels[i] = pixels[i + 1] = pixels[i + 2] = v; break;
                case PhotoTone.Chrome: v = (byte)Math.Clamp(Math.Round((y - 128) * 1.45 + 128), 0, 255); pixels[i] = pixels[i + 1] = pixels[i + 2] = v; break;
                case PhotoTone.Sepia: pixels[i] = (byte)(v * .65); pixels[i + 1] = (byte)(v * .88); pixels[i + 2] = v; break;
                case PhotoTone.Blueprint: var ink = 1 - y / 255; pixels[i] = (byte)(110 + 145 * ink); pixels[i + 1] = (byte)(48 + 207 * ink); pixels[i + 2] = (byte)(12 + 243 * ink); break;
            }
        }
        var result = BitmapSource.Create(converted.PixelWidth, converted.PixelHeight, 96, 96, PixelFormats.Bgra32, null, pixels, stride); result.Freeze(); return result;
    }
    public static void Save(BitmapSource image, string path, int quality = 90)
    {
        BitmapEncoder encoder = Path.GetExtension(path).ToLowerInvariant() switch { ".png" => new PngBitmapEncoder(), ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = quality }, ".bmp" => new BmpBitmapEncoder(), ".tif" or ".tiff" => new TiffBitmapEncoder(), ".gif" => new GifBitmapEncoder(), _ => throw new ArgumentException("Choose PNG, JPEG, BMP, TIFF or GIF.") };
        if (encoder is JpegBitmapEncoder or BmpBitmapEncoder)
        {
            var visual = new DrawingVisual(); using (var dc = visual.RenderOpen()) { dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, image.PixelWidth, image.PixelHeight)); dc.DrawImage(image, new Rect(0, 0, image.PixelWidth, image.PixelHeight)); }
            var opaque = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Pbgra32); opaque.Render(visual); image = opaque;
        }
        encoder.Frames.Add(BitmapFrame.Create(image));
        var destination = Path.GetFullPath(path); var temporary = Path.Combine(Path.GetDirectoryName(destination)!, ".SnapMark-photo-" + Guid.NewGuid().ToString("N") + ".tmp");
        try { using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write)) encoder.Save(stream); File.Move(temporary, destination, false); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static string NewPath(string folder, string stem, string extension)
    {
        Directory.CreateDirectory(folder); var path = Path.Combine(folder, stem + extension); var n = 2;
        while (File.Exists(path)) path = Path.Combine(folder, stem + "_" + n++ + extension);
        return path;
    }
    public static async Task MakeGif(IReadOnlyList<string> paths, string output, PhotoOptions options, int delayMs, bool loop, CancellationToken token)
    {
        if (paths.Count is < 2 or > 500 || delayMs is < 20 or > 10000) throw new ArgumentException("GIF needs 2–500 images and 20–10000 ms per frame.");
        var encoder = RecordingService.FindEncoder() ?? throw new InvalidOperationException("GIF requires FFmpeg, as with video export.");
        if (paths.Any(p => string.Equals(Path.GetFullPath(p), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Choose a new output file.");
        var temp = Path.Combine(Path.GetTempPath(), "SnapMark-photos-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(temp);
        var staged = Path.Combine(temp, "result.gif");
        try
        {
            await Task.Run(() =>
            {
                Size? canvas = null;
                for (var i = 0; i < paths.Count; i++)
                {
                    token.ThrowIfCancellationRequested(); var image = Transform(Load(paths[i]), options); canvas ??= new Size(image.PixelWidth, image.PixelHeight);
                    var visual = new DrawingVisual(); using (var dc = visual.RenderOpen())
                    {
                        dc.DrawRectangle(Brushes.White, null, new Rect(new Point(), canvas.Value));
                        var scale = Math.Min(canvas.Value.Width / image.PixelWidth, canvas.Value.Height / image.PixelHeight);
                        var w = image.PixelWidth * scale; var h = image.PixelHeight * scale;
                        dc.DrawImage(image, new Rect((canvas.Value.Width - w) / 2, (canvas.Value.Height - h) / 2, w, h));
                    }
                    var frame = new RenderTargetBitmap((int)canvas.Value.Width, (int)canvas.Value.Height, 96, 96, PixelFormats.Pbgra32); frame.Render(visual); Save(frame, Path.Combine(temp, $"frame-{i:D5}.png"));
                }
            }, token);
            var info = new ProcessStartInfo(encoder) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
            foreach (var arg in new[] { "-hide_banner", "-loglevel", "error", "-nostdin", "-n", "-framerate", (1000d / delayMs).ToString(CultureInfo.InvariantCulture), "-i", Path.Combine(temp, "frame-%05d.png"), "-filter_complex", "[0:v]split[a][b];[a]palettegen[p];[b][p]paletteuse", "-loop", loop ? "0" : "-1", staged }) info.ArgumentList.Add(arg);
            using var process = Process.Start(info)!; var error = process.StandardError.ReadToEndAsync(); var stdout = process.StandardOutput.ReadToEndAsync();
            try { await process.WaitForExitAsync(token); } catch { if (!process.HasExited) process.Kill(true); await process.WaitForExitAsync(); throw; }
            await stdout; var detail = await error; if (process.ExitCode != 0) throw new IOException("GIF failed: " + detail);
            token.ThrowIfCancellationRequested(); File.Move(staged, output, false);
        }
        finally { Directory.Delete(temp, true); }
    }
}
