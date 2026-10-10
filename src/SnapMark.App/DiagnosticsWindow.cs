using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Capture;
using SnapMark.Core;
using SnapMark.Editor;
using SnapMark.Export;
using SnapMark.Recording;

namespace SnapMark.App;
public sealed class DiagnosticsWindow : Window
{
    public DiagnosticsWindow()
    {
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Diagnostics"; Width = 740; Height = 560; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new DockPanel { Margin = new Thickness(18) }; Content = root;
        var actions = new StackPanel { Orientation = Orientation.Horizontal }; DockPanel.SetDock(actions, Dock.Top); root.Children.Add(actions);
        var test = new Button { Content = "Run local checks" }; actions.Children.Add(test);
        var copy = new Button { Content = "Copy report" }; actions.Children.Add(copy);
        var report = new TextBox { IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; root.Children.Add(report);
        string Run()
        {
            var text = new StringBuilder(); text.AppendLine("SnapMark " + typeof(DiagnosticsWindow).Assembly.GetName().Version);
            text.AppendLine(RuntimeInformation.OSDescription + " · " + RuntimeInformation.ProcessArchitecture);
            text.AppendLine("Current window DPI: " + VisualTreeHelper.GetDpi(this).PixelsPerInchX);
            text.AppendLine("Virtual desktop: " + NativeDesktop.VirtualBounds);
            foreach (var monitor in NativeDesktop.Monitors) text.AppendLine("Monitor physical pixels: " + monitor);
            text.AppendLine("Local OCR languages: " + string.Join(", ", LocalOcr.Languages().Select(l => l.Tag)));
            text.AppendLine("FFmpeg: " + (RecordingService.FindEncoder() ?? "Not installed"));
            var image = BitmapSource.Create(900, 520, 96, 96, PixelFormats.Bgra32, null, new byte[900 * 520 * 4], 3600); image.Freeze();
            var document = new EditorDocument(900, 520);
            for (int i = 0; i < 300; i++) document.Annotations.Add(new(Guid.NewGuid(), AnnotationKind.Rectangle, new((i * 37) % 850, (i * 19) % 460), new((i * 37) % 850 + 35, (i * 19) % 460 + 35), Thickness: 2));
            var timer = Stopwatch.StartNew(); var output = new AnnotationRenderer().Render(image, document); timer.Stop();
            text.AppendLine($"Render 900×520 with 300 objects: {timer.Elapsed.TotalMilliseconds:F1} ms · {output.PixelWidth}×{output.PixelHeight}");
            text.AppendLine($"Managed memory: {GC.GetTotalMemory(false) / 1048576d:F1} MB");
            text.AppendLine("Checks describe connected hardware only. Mixed-DPI monitor switching, disconnects, HDR/protected windows and microphone synchronization need checks on their target hardware.");
            return text.ToString();
        }
        test.Click += (_, _) => { try { report.Text = Run(); } catch (Exception ex) { report.Text = ex.Message; } };
        copy.Click += (_, _) => { try { Clipboard.SetText(report.Text); } catch (Exception ex) { MessageBox.Show(this, ex.Message); } };
        Loaded += (_, _) => test.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
}
