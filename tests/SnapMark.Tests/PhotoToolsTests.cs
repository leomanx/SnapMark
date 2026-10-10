using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.App;
using SnapMark.Core;
using SnapMark.Editor;

internal static class PhotoToolsTests
{
    public static void Run(Action<bool, string> check, string folder, bool media)
    {
        var points = new PixelPoint[] { new(0, 0), new(40, 0), new(40, 30), new(0, 30) };
        check(GeometryMath.PathLength(points, false) == 110 && GeometryMath.PathLength(points, true) == 140, "closed route includes return segment exactly once");
        check(GeometryMath.PathLength(points.Append(points[0]).ToArray(), true) == 140, "explicitly repeated start does not double-count closing segment");
        var pixels = new byte[80 * 60 * 4]; for (var i = 0; i < pixels.Length; i += 4) { pixels[i] = 30; pixels[i + 1] = 100; pixels[i + 2] = 210; pixels[i + 3] = 255; }
        var image = BitmapSource.Create(80, 60, 144, 144, PixelFormats.Bgra32, null, pixels, 320); image.Freeze();
        var surface = new EditorSurface(image); var path = new Annotation(Guid.NewGuid(), AnnotationKind.Polygon, points[0], points[^1], Points: points);
        surface.History.Execute(new AddAnnotationCommand(surface.Document, path)); surface.SelectObject(path.Id); surface.MeasureSelectedPath(false);
        check(surface.Document.Annotations.Single().Kind == AnnotationKind.Length && surface.Document.Annotations.Single().Closed, "existing polygon converts to closed perimeter measurement");
        surface.History.Execute(new CalibrationCommand(surface.Document, .5, "mm"));
        check(GeometryMath.PathLength(points, true) * surface.Document.UnitsPerPixel == 70 && GeometryMath.Area(points) * Math.Pow(surface.Document.UnitsPerPixel, 2) == 300, "calibration scales perimeter linearly and area quadratically");
        surface.History.Undo(); surface.History.Undo(); check(surface.Document.Unit == "px" && surface.Document.Annotations.Single() == path, "calibration and measurement conversion undo preserve original geometry");
        check(PhotoProcessing.OutputSize(80, 60, new(40, 40)) == new Size(40, 30), "resize fits bounding box with aspect ratio");
        check(PhotoProcessing.OutputSize(80, 60, new(0, 30)) == new Size(40, 30), "single resize dimension calculates the other");
        check(PhotoProcessing.OutputSize(80, 60, new(160, 120)) == new Size(80, 60), "resize prevents enlargement unless enabled");
        var resized = PhotoProcessing.Transform(image, new(40, 20, false)); check(resized.PixelWidth == 40 && resized.PixelHeight == 20, "exact resize outputs requested pixels regardless of source DPI");
        foreach (var tone in Enum.GetValues<PhotoTone>())
        {
            var result = PhotoProcessing.Transform(image, new(Tone: tone)); var bytes = new byte[80 * 60 * 4]; result.CopyPixels(bytes, 320, 0);
            check(result.IsFrozen && result.PixelWidth == 80 && bytes[3] == 255, "photo tone " + tone + " produces frozen valid image");
            if (tone is PhotoTone.Grey or PhotoTone.Chrome or PhotoTone.BlackWhite) check(bytes[0] == bytes[1] && bytes[1] == bytes[2], tone + " has equal RGB channels");
            if (tone == PhotoTone.BlackWhite) check(bytes[0] is 0 or 255, "black and white is binary threshold");
            if (tone == PhotoTone.Blueprint) check(bytes[0] > bytes[1] && bytes[1] > bytes[2], "blueprint maps image to blue tones");
        }
        var files = new List<string>();
        check(PhotoPaper.Sizes.Single(p => p.Name == "A0").WidthMm == 841 && PhotoPaper.Sizes.Single(p => p.Name == "A4").HeightMm == 297 && PhotoPaper.Sizes.Single(p => p.Name == "ISO B3").WidthMm == 353, "paper presets distinguish ISO A and ISO B dimensions");
        check(Math.Abs(PrintUnits.ParseMm("1' 2.5\"", PrintUnit.FootInch) - 368.3) < .001 && PrintUnits.ParseMm("1", PrintUnit.Inch) == 25.4, "foot-inch and inch inputs convert to physical millimeters");
        check(Math.Abs(PrintUnits.ParseMm("1'-2 1/2\"", PrintUnit.FootInch) - 368.3) < .001 && PrintUnits.ParseMm("1/8\"", PrintUnit.Inch) == 3.175, "architectural feet-inch notation and fractional inches are accepted");
        check(Math.Abs(PrintUnits.ParseMm(PrintUnits.FormatMm(7, PrintUnit.FootInch), PrintUnit.FootInch) - 7) < .002, "unit switching preserves physical margin");
        foreach (var extension in new[] { ".png", ".jpg", ".bmp", ".tiff", ".gif" })
        {
            var file = PhotoProcessing.NewPath(folder, "photo", extension); PhotoProcessing.Save(resized, file); files.Add(file);
            var loaded = PhotoProcessing.Load(file); check(loaded.PixelWidth == 40 && loaded.PixelHeight == 20, "photo converts and decodes " + extension);
        }
        var original = File.ReadAllBytes(files[0]);
        File.Copy(files[0], Path.Combine(folder, "UPPER.PNG")); File.WriteAllText(Path.Combine(folder, "ignore.txt"), "not an image");
        check(PhotoProcessing.FolderPhotos(folder).Contains(Path.Combine(folder, "UPPER.PNG")) && !PhotoProcessing.FolderPhotos(folder).Any(p => p.EndsWith("ignore.txt")), "Add folder accepts uppercase photo extensions and filters non-images");
        check(PhotoProcessing.NewPath(folder, "photo", ".png") != files[0], "batch output avoids existing filenames");
        try { PhotoProcessing.Save(image, files[0]); check(false, "photo source overwrite rejected"); } catch (IOException) { check(File.ReadAllBytes(files[0]).SequenceEqual(original), "photo export cannot overwrite existing source"); }
        var print = PhotoPrintLayout.Create(files, new(), new(Columns: 2, Rows: 2)); check(print.Pages.Count == 2 && print.DocumentPaginator.PageSize.Width > 790, "batch print layout paginates selected photos on A4");
        var page = print.DocumentPaginator.GetPage(0); var rendered = new RenderTargetBitmap(794, 1123, 96, 96, PixelFormats.Pbgra32); rendered.Render(page.Visual); var ink = new byte[794 * 1123 * 4]; rendered.CopyPixels(ink, 794 * 4, 0); var sample = (290 * 794 + 210) * 4; check(ink[sample + 2] > 190 && ink[sample + 1] is > 80 and < 120 && ink[sample] < 50, "print layout renders source photo pixels inside its first cell");
        foreach (var mode in Enum.GetValues<PhotoCaption>())
        {
            var layout = PhotoPrintLayout.Create(files.Take(1).ToArray(), new(), new(Caption: mode, NameInset: 12));
            var cell = (System.Windows.Controls.Canvas)layout.Pages[0].Child.Children[0]; var photoImage = (System.Windows.Controls.Image)cell.Children[0];
            var text = cell.Children.OfType<System.Windows.Controls.TextBlock>().SingleOrDefault();
            check((text is null) == (mode == PhotoCaption.Hidden), "caption mode " + mode + " controls filename visibility");
            if (text is not null && mode != PhotoCaption.Below) check(System.Windows.Controls.Canvas.GetLeft(text) >= System.Windows.Controls.Canvas.GetLeft(photoImage) + 11 && System.Windows.Controls.Canvas.GetTop(text) >= System.Windows.Controls.Canvas.GetTop(photoImage) && System.Windows.Controls.Canvas.GetTop(text) + text.Height <= System.Windows.Controls.Canvas.GetTop(photoImage) + photoImage.Height, "overlay filename stays inset inside actual photo bounds");
            if (text is not null && mode == PhotoCaption.Below) check(System.Windows.Controls.Canvas.GetTop(text) >= System.Windows.Controls.Canvas.GetTop(photoImage) + photoImage.Height + 11, "below filename respects its gap from actual image");
        }
        var pane = new PhotoPreviewPane(); pane.ShowImage(image, "test");
        typeof(PhotoPreviewPane).GetMethod("SetZoom", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(pane, [1d]);
        var previewImage = (System.Windows.Controls.Image)typeof(PhotoPreviewPane).GetField("image", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(pane)!;
        check(previewImage.Width == 80 && previewImage.Height == 60, "100% photo preview uses image pixel dimensions independent of source DPI");
        if (media)
        {
            var gif = Path.Combine(folder, "photos-animation.gif"); PhotoProcessing.MakeGif(files.Take(3).ToArray(), gif, new(), 500, true, CancellationToken.None).GetAwaiter().GetResult();
            using var stream = File.OpenRead(gif); var decoder = new GifBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            check(decoder.Frames.Count == 3 && decoder.Frames.All(f => f.PixelWidth == 40 && f.PixelHeight == 20), "photo set produces three animated GIF frames on a common canvas");
            var metadata = (BitmapMetadata)decoder.Frames[0].Metadata; check(Convert.ToInt32(metadata.GetQuery("/grctlext/Delay")) == 50, "animated photo GIF preserves 500 ms frame delay");
            check(File.ReadAllBytes(files[0]).SequenceEqual(original), "animated GIF preserves source image bytes");
        }
    }
}
