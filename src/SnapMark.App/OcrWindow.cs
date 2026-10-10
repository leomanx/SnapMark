using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SnapMark.Export;

namespace SnapMark.App;

public sealed class OcrWindow : Window
{
    public OcrWindow(BitmapSource image)
    {
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Local OCR"; Width = 660; Height = 520; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new DockPanel { Margin = new Thickness(16) }; Content = panel;
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top); panel.Children.Add(top);
        var languages = LocalOcr.Languages();
        top.Children.Add(new TextBlock { Text = "OCR · อ่านข้อความในภาพที่แสดง / crop ปัจจุบัน", FontSize = 18 });
        top.Children.Add(new TextBlock { Text = "Runs locally using installed Windows OCR languages. Thai requires the Thai OCR language pack.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) });
        var choice = new ComboBox { ItemsSource = languages.Prepend(new OcrLanguage("auto", "Auto · Windows user language preferences")).ToArray(), DisplayMemberPath = "Name", SelectedIndex = 0, Margin = new Thickness(4) }; top.Children.Add(choice);
        top.Children.Add(new TextBlock { Text = $"OCR region: x, y, width, height (pixels in displayed image, {image.PixelWidth} × {image.PixelHeight})", TextWrapping = TextWrapping.Wrap });
        var roi = new TextBox { Text = $"0, 0, {image.PixelWidth}, {image.PixelHeight}" }; top.Children.Add(roi);
        var actions = new StackPanel { Orientation = Orientation.Horizontal }; top.Children.Add(actions);
        var read = new Button { Content = "Read text", IsEnabled = languages.Count > 0 }; actions.Children.Add(read);
        var copy = new Button { Content = "Copy text", IsEnabled = false }; actions.Children.Add(copy);
        var status = new TextBlock { Text = languages.Count == 0 ? "No OCR language installed in Windows." : "Ready · image processing stays on this computer.", Margin = new Thickness(4) }; top.Children.Add(status);
        var result = new TextBox { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(4) }; panel.Children.Add(result);
        var search = new TextBox { Margin = new Thickness(4) }; FieldHelp.Add(top, search, "ค้นหาในข้อความที่อ่านได้ / Search OCR result", "พิมพ์คำที่ต้องการค้นหา เช่น RFI หรือชื่อห้อง · ไม่ใช่ Path · อ่านภาพก่อนแล้วจึงค้นหา");
        var find = new Button { Content = "Find next in OCR text" }; top.Children.Add(find); find.Click += (_, _) => { if (search.Text.Length == 0) return; var index = result.Text.IndexOf(search.Text, Math.Min(result.Text.Length, result.SelectionStart + result.SelectionLength), StringComparison.OrdinalIgnoreCase); if (index < 0) index = result.Text.IndexOf(search.Text, StringComparison.OrdinalIgnoreCase); if (index >= 0) { result.Focus(); result.Select(index, search.Text.Length); } else status.Text = "Text not found."; };
        read.Click += async (_, _) => { if (choice.SelectedItem is not OcrLanguage language) return; read.IsEnabled = false; status.Text = "Reading…"; try {
            var parts = roi.Text.Split(',', StringSplitOptions.TrimEntries).Select(int.Parse).ToArray(); if (parts.Length != 4 || parts[0] < 0 || parts[1] < 0 || parts[2] <= 0 || parts[3] <= 0 || (long)parts[0] + parts[2] > image.PixelWidth || (long)parts[1] + parts[3] > image.PixelHeight) throw new ArgumentException("Enter x, y, width, height inside the displayed image.");
            var region = new CroppedBitmap(image, new Int32Rect(parts[0], parts[1], parts[2], parts[3])); region.Freeze();
            result.Text = await LocalOcr.ReadAsync(region, language.Tag); copy.IsEnabled = result.Text.Length > 0; status.Text = result.Text.Length == 0 ? "No text found." : "Done · review recognition before using the text."; } catch (Exception ex) { status.Text = ex.Message; } finally { read.IsEnabled = true; } };
        copy.Click += (_, _) => { try { Clipboard.SetText(result.Text); status.Text = "Text copied."; } catch (Exception ex) { status.Text = ex.Message; } };
    }
}
