using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Editor;
using SnapMark.Export;

namespace SnapMark.App;
public sealed class CompareWindow : Window
{
    public CompareWindow(BitmapSource before, BitmapSource after)
    {
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Compare images"; Width = 1100; Height = 760; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        if (before.PixelWidth != after.PixelWidth || before.PixelHeight != after.PixelHeight) throw new ArgumentException("Compare images must have equal pixel dimensions.");
        var root = new DockPanel(); Content = root; var bar = new WrapPanel(); DockPanel.SetDock(bar, Dock.Top); root.Children.Add(bar);
        var mode = new ComboBox { ItemsSource = new[] { "Overlay", "Side by side", "Difference" }, SelectedIndex = 0 }; bar.Children.Add(mode);
        var opacity = new Slider { Minimum = 0, Maximum = 1, Value = .5, Width = 150, Margin = new Thickness(8), ToolTip = "After image opacity" }; bar.Children.Add(opacity);
        var threshold = new TextBox { Text = "16", Width = 70, ToolTip = "Difference threshold 0–255" }; bar.Children.Add(new TextBlock { Text = "Threshold", Margin = new Thickness(8, 12, 0, 0) }); bar.Children.Add(threshold);
        var calculate = new Button { Content = "Calculate difference" }; bar.Children.Add(calculate);
        var save = new Button { Content = "Save difference PNG", IsEnabled = false }; bar.Children.Add(save);
        var status = new TextBlock { Text = "Reference (before) and current editor (after) · equal pixel dimensions; no automatic registration.", Margin = new Thickness(14), Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush"), TextWrapping = TextWrapping.Wrap }; DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
        var grid = new Grid { Width = before.PixelWidth, Height = before.PixelHeight }; grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(0) });
        var first = new Image { Source = before, Stretch = Stretch.Fill }; var second = new Image { Source = after, Stretch = Stretch.Fill, Opacity = .5 }; grid.Children.Add(first); grid.Children.Add(second);
        root.Children.Add(new Viewbox { Child = grid, Stretch = Stretch.Uniform, Margin = new Thickness(16) }); BitmapSource? difference = null;
        void Update()
        {
            if (mode.SelectedIndex == 2 && difference is null) { status.Text = "Calculate difference first; current preview shows the after image."; }
            var side = mode.SelectedIndex == 1; grid.Width = before.PixelWidth * (side ? 2 : 1); grid.ColumnDefinitions[1].Width = side ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            Grid.SetColumn(second, side ? 1 : 0); second.Source = mode.SelectedIndex == 2 && difference is not null ? difference : after; second.Opacity = mode.SelectedIndex == 0 ? opacity.Value : 1;
            opacity.IsEnabled = mode.SelectedIndex == 0;
        }
        mode.SelectionChanged += (_, _) => Update(); opacity.ValueChanged += (_, _) => Update();
        calculate.Click += async (_, _) =>
        {
            if (!int.TryParse(threshold.Text, out var value) || value < 0 || value > 255) { status.Text = "Enter threshold 0–255."; return; }
            calculate.IsEnabled = false; status.Text = "Comparing pixels…";
            try { var result = await Task.Run(() => ImageComparison.Compare(before, after, value)); difference = result.Difference; save.IsEnabled = true; mode.SelectedIndex = 2; Update(); status.Text = $"Changed {result.ChangedPixels:N0} / {result.TotalPixels:N0} pixels · {result.ChangedPercent:0.##}% · threshold {value}"; }
            catch (Exception ex) { status.Text = ex.Message; } finally { calculate.IsEnabled = true; }
        };
        save.Click += (_, _) => { var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Difference PNG|*.png", FileName = "Comparison.png" }; if (difference is not null && dialog.ShowDialog(this) == true) { try { new ExportService().SavePng(difference, dialog.FileName); } catch (Exception ex) { status.Text = ex.Message; } } };
    }
}
