using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SnapMark.Core;
using SnapMark.Capture;

namespace SnapMark.App;
public sealed class ScrollingWindow : Window
{
    public ScrollingWindow(BitmapSource initial, PixelRect bounds, Action<BitmapSource> finished)
    {
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Scrolling capture"; Width = 470; Height = 460; Topmost = true; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var stitcher = new ScrollStitcher(); stitcher.Add(initial);
        var panel = new StackPanel { Margin = new Thickness(18) }; Content = panel;
        panel.Children.Add(new TextBlock { Text = "Scrolling capture · guided", FontSize = 22 });
        panel.Children.Add(new TextBlock { Text = "Scroll the target downward manually, leaving at least 20% overlap. Then click Next frame. Select content that excludes fixed headers, footers and animations. This panel hides during each capture.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 12, 4, 12) });
        var status = new TextBlock { Text = $"1 frame · {stitcher.Height}px tall", TextWrapping = TextWrapping.Wrap }; panel.Children.Add(status);
        var next = new Button { Content = "Next frame" }; panel.Children.Add(next);
        var undo = new Button { Content = "Undo last frame", IsEnabled = false }; panel.Children.Add(undo);
        var done = new Button { Content = "Finish → editor" }; panel.Children.Add(done);
        var auto = new Button { Content = "Auto scroll · up to 20 new frames" }; panel.Children.Add(auto);
        var cancel = new Button { Content = "Stop auto scrolling", IsEnabled = false }; panel.Children.Add(cancel);
        CancellationTokenSource? automation = null; cancel.Click += (_, _) => automation?.Cancel();
        Closing += (_, e) => { if (automation is not null) { e.Cancel = true; automation.Cancel(); } };
        auto.Click += async (_, _) =>
        {
            automation = new CancellationTokenSource(); var token = automation.Token; auto.IsEnabled = next.IsEnabled = done.IsEnabled = undo.IsEnabled = false; cancel.IsEnabled = true;
            try
            {
                for (var i = 0; i < 20; i++)
                {
                    token.ThrowIfCancellationRequested(); Hide(); await Task.Delay(180, token); NativeDesktop.ScrollRegion(bounds); await Task.Delay(600, token);
                    var frame = NativeDesktop.Snapshot(bounds); var before = stitcher.Count; await Task.Run(() => stitcher.Add(frame), token);
                    Show(); status.Text = $"{stitcher.Count} frames · {stitcher.Height}px tall";
                    if (stitcher.Count == before) { status.Text += " · stopped at unchanged frame"; break; }
                    await Task.Delay(700, token);
                }
            }
            catch (OperationCanceledException) { status.Text = "Auto scroll stopped. Captured frames remain available."; }
            catch (Exception ex) { status.Text = "Auto scroll stopped: " + ex.Message + " · use manual Next frame if overlap is insufficient."; }
            finally { automation.Dispose(); automation = null; Show(); Activate(); auto.IsEnabled = next.IsEnabled = done.IsEnabled = true; undo.IsEnabled = stitcher.Count > 1; cancel.IsEnabled = false; }
        };
        next.Click += async (_, _) => { next.IsEnabled = done.IsEnabled = undo.IsEnabled = false; Hide(); try { await Task.Delay(180); var frame = NativeDesktop.Snapshot(bounds); await Task.Run(() => stitcher.Add(frame)); status.Text = $"{stitcher.Count} frames · {stitcher.Height}px tall"; } catch (Exception ex) { status.Text = ex.Message; } finally { Show(); Activate(); next.IsEnabled = done.IsEnabled = true; undo.IsEnabled = stitcher.Count > 1; } };
        undo.Click += (_, _) => { stitcher.Undo(); status.Text = $"{stitcher.Count} frames · {stitcher.Height}px tall"; undo.IsEnabled = stitcher.Count > 1; };
        done.Click += (_, _) => { try { var image = stitcher.Render(); Close(); finished(image); } catch (Exception ex) { status.Text = ex.Message; } };
    }
}
