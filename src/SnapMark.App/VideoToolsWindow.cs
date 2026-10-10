using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SnapMark.Recording;

namespace SnapMark.App;
public sealed class VideoToolsWindow : Window
{
    public VideoToolsWindow(Func<string>? outputFolder = null)
    {
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Trim / GIF"; Width = 600; Height = 580; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(20) }; Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = "VIDEO · Trim MP4 / Export GIF", FontSize = 22 });
        var source = new TextBox(); FieldHelp.Add(panel, source, "ไฟล์วิดีโอต้นฉบับ / Source video file", @"กด Choose video หรือใส่ Path เต็มของไฟล์ เช่น D:\Videos\meeting.mp4 · ไม่ใช่โฟลเดอร์ · รองรับ MP4/MOV/MKV/AVI/WebM");
        var open = new Button { Content = "Choose video…" }; panel.Children.Add(open);
        open.Click += (_, _) => { var file = new Microsoft.Win32.OpenFileDialog { Filter = "Video|*.mp4;*.mov;*.mkv;*.avi;*.webm", InitialDirectory = outputFolder?.Invoke() }; if (file.ShowDialog(this) == true) source.Text = file.FileName; };
        panel.Children.Add(new TextBlock { Text = "Start / End · seconds (example: 2.5 to 8)" }); var range = new StackPanel { Orientation = Orientation.Horizontal }; panel.Children.Add(range);
        var start = new TextBox { Text = "0", Width = 150 }; var end = new TextBox { Text = "5", Width = 150 }; range.Children.Add(start); range.Children.Add(end);
        FieldHelp.Hint(panel, start, "Start · seconds", "ช่องซ้าย = เวลาเริ่ม ช่องขวา = เวลาจบ หน่วยวินาที เช่น 2.5 ถึง 8 · เวลาจบต้องมากกว่าเวลาเริ่ม"); System.Windows.Automation.AutomationProperties.SetName(end, "End · seconds"); end.ToolTip = "เวลาจบในไฟล์ต้นฉบับ หน่วยวินาที เช่น 8";
        var format = new ComboBox { ItemsSource = new[] { "MP4 · keeps available audio", "GIF · configurable size / fps / loop, no audio" }, SelectedIndex = 0, Margin = new Thickness(4, 8, 4, 8) }; panel.Children.Add(format);
        var gifSettings = new StackPanel { Orientation = Orientation.Horizontal, IsEnabled = false }; panel.Children.Add(gifSettings);
        var gifFps = new ComboBox { ItemsSource = new[] { 6, 10, 12, 15, 24, 30 }, SelectedIndex = 2, Width = 85 }; var gifWidth = new ComboBox { ItemsSource = new[] { 320, 640, 960, 1280, 1920 }, SelectedIndex = 2, Width = 95 }; var gifLoop = new ComboBox { ItemsSource = new[] { "Forever", "Play once", "Repeat 3 times" }, SelectedIndex = 0, Width = 150 };
        gifSettings.Children.Add(new TextBlock { Text = "fps" }); gifSettings.Children.Add(gifFps); gifSettings.Children.Add(new TextBlock { Text = "max px" }); gifSettings.Children.Add(gifWidth); gifSettings.Children.Add(gifLoop);
        format.SelectionChanged += (_, _) => gifSettings.IsEnabled = format.SelectedIndex == 1;
        var save = new Button { Content = "Export selected range…" }; var cancel = new Button { Content = "Cancel export", IsEnabled = false }; panel.Children.Add(save); panel.Children.Add(cancel);
        var status = new TextBlock { Text = "Local FFmpeg · source video stays unchanged.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 12, 4, 4) }; panel.Children.Add(status);
        CancellationTokenSource? work = null; cancel.Click += (_, _) => work?.Cancel(); Closed += (_, _) => work?.Cancel();
        save.Click += async (_, _) => {
            if (!double.TryParse(start.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var from) || !double.TryParse(end.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var to)) { status.Text = "Enter start/end seconds."; return; }
            string? folder = null; try { folder = outputFolder?.Invoke(); if (folder is not null) System.IO.Directory.CreateDirectory(folder); } catch (Exception ex) { status.Text = ex.Message; return; }
            var gif = format.SelectedIndex == 1; var file = new Microsoft.Win32.SaveFileDialog { Filter = gif ? "GIF|*.gif" : "MP4|*.mp4", FileName = gif ? "SnapMark-clip.gif" : "SnapMark-trim.mp4", InitialDirectory = folder }; if (file.ShowDialog(this) != true) return;
            work = new(); save.IsEnabled = open.IsEnabled = false; cancel.IsEnabled = true; status.Text = "Exporting…";
            try { await VideoTools.Export(source.Text, file.FileName, from, to, gif, work.Token, new GifOptions((int)gifFps.SelectedItem, (int)gifWidth.SelectedItem, gifLoop.SelectedIndex == 1 ? -1 : gifLoop.SelectedIndex == 2 ? 3 : 0)); status.Text = "Saved: " + file.FileName; } catch (OperationCanceledException) { status.Text = "Export cancelled."; } catch (Exception ex) { status.Text = ex.Message; }
            finally { work.Dispose(); work = null; save.IsEnabled = open.IsEnabled = true; cancel.IsEnabled = false; }
        };
    }
}
