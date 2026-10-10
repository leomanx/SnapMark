using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SnapMark.App;

public sealed class PhotoPreviewPane : DockPanel
{
    private readonly Image image = new() { Stretch = Stretch.Fill };
    private readonly ScrollViewer imageScroll;
    private readonly DocumentViewer viewer = new();
    private readonly TextBlock caption = new() { Text = "Select photos to preview", Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock zoomLabel = new() { Margin = new Thickness(8), VerticalAlignment = VerticalAlignment.Center };
    private bool fit = true, layout;
    private double zoom = 1;
    public PhotoPreviewPane()
    {
        var bar = new WrapPanel(); SetDock(bar, Dock.Top); Children.Add(bar);
        void Button(string icon, string? label, string hint, Action click) { var b = new Button { Padding = new Thickness(7) }; ToolIcon.SetButton(b, icon, label, hint); b.Click += (_, _) => click(); bar.Children.Add(b); }
        Button("fit", "Fit page", "Fit whole photo / page", Fit);
        Button("actual", "100%", "Photo: actual image pixels · page: 100% document scale", () => SetZoom(1));
        Button("minus", null, "Zoom out", () => SetZoom(CurrentZoom() / 1.25)); Button("plus", null, "Zoom in", () => SetZoom(CurrentZoom() * 1.25)); bar.Children.Add(zoomLabel);
        SetDock(caption, Dock.Bottom); Children.Add(caption);
        imageScroll = new ScrollViewer { Content = image, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Background = Brushes.SlateGray };
        image.HorizontalAlignment = HorizontalAlignment.Center; image.VerticalAlignment = VerticalAlignment.Center;
        var grid = new Grid(); grid.Children.Add(imageScroll); grid.Children.Add(viewer); Children.Add(grid); viewer.Visibility = Visibility.Collapsed;
        imageScroll.SizeChanged += (_, _) => { if (fit && !layout) ResizeImage(); };
        viewer.SizeChanged += (_, _) => { if (fit && layout) FitDocument(); };
        var zoomProperty = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(DocumentViewer.ZoomProperty, typeof(DocumentViewer));
        EventHandler zoomChanged = (_, _) => UpdateZoom(); var listening = false;
        Loaded += (_, _) => { if (!listening) { zoomProperty.AddValueChanged(viewer, zoomChanged); listening = true; } };
        Unloaded += (_, _) => { if (listening) { zoomProperty.RemoveValueChanged(viewer, zoomChanged); listening = false; } };
        PreviewMouseWheel += (_, e) => { if (System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control)) { SetZoom(CurrentZoom() * (e.Delta > 0 ? 1.2 : 1 / 1.2)); e.Handled = true; } };
    }
    public void ShowImage(BitmapSource source, string text)
    {
        if (layout) fit = true;
        layout = false; image.Source = source; viewer.Document = null; viewer.Visibility = Visibility.Collapsed; imageScroll.Visibility = Visibility.Visible; caption.Text = text; ResizeImage();
    }
    public void ShowDocument(FixedDocument document, string text)
    {
        if (!layout) fit = true;
        layout = true; viewer.Document = document; image.Source = null; viewer.Visibility = Visibility.Visible; imageScroll.Visibility = Visibility.Collapsed; caption.Text = text;
        if (fit) FitDocument(); else viewer.Zoom = zoom * 100; UpdateZoom();
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() => { if (fit && layout) FitDocument(); }));
    }
    public void Clear() { image.Source = null; viewer.Document = null; caption.Text = "Select photos to preview"; }
    public void Print() { if (viewer.Document is not null) viewer.Print(); }
    public void Fit() { fit = true; if (layout) FitDocument(); else ResizeImage(); UpdateZoom(); }
    private void FitDocument()
    {
        if (viewer.Document is not FixedDocument document || viewer.ActualWidth < 60 || viewer.ActualHeight < 100) return;
        var page = document.DocumentPaginator.PageSize;
        viewer.FitToHeight(); var widthZoom = Math.Clamp((viewer.ActualWidth - 40) / page.Width * 100, 5, 800); if (viewer.Zoom > widthZoom) viewer.Zoom = widthZoom; UpdateZoom();
    }
    private double CurrentZoom() => layout ? viewer.Zoom / 100 : zoom;
    private void SetZoom(double value) { fit = false; zoom = Math.Clamp(value, .02, 8); if (layout) viewer.Zoom = zoom * 100; else ResizeImage(); UpdateZoom(); }
    private void ResizeImage()
    {
        if (image.Source is not BitmapSource source) return;
        if (fit) zoom = Math.Min(Math.Max(1, imageScroll.ActualWidth - 24) / source.PixelWidth, Math.Max(1, imageScroll.ActualHeight - 24) / source.PixelHeight);
        image.Width = source.PixelWidth * zoom; image.Height = source.PixelHeight * zoom; UpdateZoom();
    }
    private void UpdateZoom() => zoomLabel.Text = $"{CurrentZoom() * 100:0}%";
}
