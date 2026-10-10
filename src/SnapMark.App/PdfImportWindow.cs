using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SnapMark.Export;
using System.Windows.Media.Imaging;

namespace SnapMark.App;
public sealed class PdfImportWindow : Window
{
    public PdfImportWindow(string path, Action<BitmapSource> imported)
    {
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · PDF page import"; Width = 500; Height = 350; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new StackPanel { Margin = new Thickness(20) }; Content = root;
        var status = new TextBlock { Text = "Reading PDF…", TextWrapping = TextWrapping.Wrap, Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush"), Margin = new Thickness(4) }; root.Children.Add(status);
        root.Children.Add(new TextBlock { Text = "Page number (1-based)" }); var page = new TextBox { Text = "1" }; root.Children.Add(page);
        root.Children.Add(new TextBlock { Text = "Resolution (DPI)" }); var dpi = new ComboBox { ItemsSource = new[] { 72, 96, 150, 200, 300 }, SelectedItem = 150 }; root.Children.Add(dpi);
        var open = new Button { Content = "Import page as image", IsEnabled = false }; root.Children.Add(open); uint count = 0;
        Loaded += async (_, _) => { try { count = await PdfImporter.PageCount(path); status.Text = $"{count} pages · imported as an image for markup; original PDF stays unchanged."; open.IsEnabled = true; } catch (Exception ex) { status.Text = ex.Message; } };
        open.Click += async (_, _) =>
        {
            if (!int.TryParse(page.Text, out var number) || number < 1 || number > count) { status.Text = "Enter a valid page number."; return; }
            open.IsEnabled = false; status.Text = "Rendering page…";
            try { var image = await PdfImporter.RenderPage(path, number, dpi.SelectedItem is int value ? value : 150); imported(image); Close(); } catch (Exception ex) { status.Text = ex.Message; open.IsEnabled = true; }
        };
    }
}
