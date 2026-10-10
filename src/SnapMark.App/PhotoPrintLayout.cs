using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SnapMark.App;

public enum PhotoCaption { Hidden, Below, OverlayBottom, OverlayTop }
public sealed record PhotoPrintOptions(double Width = 793.7, double Height = 1122.5, int Columns = 2, int Rows = 2, double Margin = 28, bool Names = true, PhotoCaption Caption = PhotoCaption.Below, double NameInset = 8, double FontSize = 10);
public static class PhotoPrintLayout
{
    public static FixedDocument Create(IReadOnlyList<string> paths, PhotoOptions photo, PhotoPrintOptions layout)
        => CreateImages(paths.Select(path => (path, PhotoProcessing.Transform(PhotoProcessing.Load(path), photo))).ToArray(), layout);
    public static FixedDocument CreateImages(IReadOnlyList<(string Path, BitmapSource Image)> images, PhotoPrintOptions layout)
    {
        if (!double.IsFinite(layout.Width) || !double.IsFinite(layout.Height) || !double.IsFinite(layout.Margin) || !double.IsFinite(layout.NameInset) || !double.IsFinite(layout.FontSize) || layout.Width <= 0 || layout.Height <= 0 || layout.Columns is < 1 or > 6 || layout.Rows is < 1 or > 8 || layout.Margin < 0 || layout.NameInset < 0 || layout.FontSize is < 6 or > 72 || layout.Margin * 2 >= Math.Min(layout.Width, layout.Height)) throw new ArgumentException("Invalid print layout.");
        var document = new FixedDocument(); document.DocumentPaginator.PageSize = new Size(layout.Width, layout.Height);
        var perPage = layout.Columns * layout.Rows; const double gap = 12;
        var w = (layout.Width - 2 * layout.Margin - (layout.Columns - 1) * gap) / layout.Columns;
        var h = (layout.Height - 2 * layout.Margin - (layout.Rows - 1) * gap) / layout.Rows;
        if (w <= 0 || h <= 0) throw new ArgumentException("Margins and grid leave no space for images.");
        for (var offset = 0; offset < images.Count; offset += perPage)
        {
            var page = new FixedPage { Width = layout.Width, Height = layout.Height, Background = Brushes.White };
            for (var i = 0; i < perPage && offset + i < images.Count; i++)
            {
                var (path, image) = images[offset + i]; var panel = new Canvas { Width = w, Height = h, ClipToBounds = true };
                var caption = layout.Names ? layout.Caption : PhotoCaption.Hidden;
                var textHeight = layout.FontSize * 1.5; var imageHeight = h - (caption == PhotoCaption.Below ? textHeight + layout.NameInset : 0);
                if (imageHeight <= 0) throw new ArgumentException("Filename size and inset leave no space for images.");
                var scale = Math.Min(w / image.PixelWidth, imageHeight / image.PixelHeight); var iw = image.PixelWidth * scale; var ih = image.PixelHeight * scale;
                var x = (w - iw) / 2; var y = (imageHeight - ih) / 2;
                var picture = new Image { Source = image, Width = iw, Height = ih, Stretch = Stretch.Fill }; Canvas.SetLeft(picture, x); Canvas.SetTop(picture, y); panel.Children.Add(picture);
                if (caption != PhotoCaption.Hidden)
                {
                    var overlay = caption is PhotoCaption.OverlayBottom or PhotoCaption.OverlayTop;
                    var inset = Math.Min(layout.NameInset, Math.Max(0, Math.Min(iw / 3, (ih - textHeight) / 2)));
                    var name = new TextBlock { Text = Path.GetFileName(path), FontSize = layout.FontSize, Foreground = Brushes.Black, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Height = textHeight, Width = overlay ? Math.Max(1, iw - inset * 2) : w, Background = overlay ? new SolidColorBrush(Color.FromArgb(210,255,255,255)) : Brushes.Transparent };
                    Canvas.SetLeft(name, overlay ? x + inset : 0); Canvas.SetTop(name, caption == PhotoCaption.Below ? y + ih + layout.NameInset : caption == PhotoCaption.OverlayTop ? y + inset : y + ih - inset - textHeight); panel.Children.Add(name);
                }
                FixedPage.SetLeft(panel, layout.Margin + i % layout.Columns * (w + gap)); FixedPage.SetTop(panel, layout.Margin + i / layout.Columns * (h + gap)); page.Children.Add(panel);
            }
            var content = new PageContent(); ((System.Windows.Markup.IAddChild)content).AddChild(page); document.Pages.Add(content);
        }
        return document;
    }
}
