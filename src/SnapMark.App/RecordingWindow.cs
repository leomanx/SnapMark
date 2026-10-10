using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SnapMark.Recording;

namespace SnapMark.App;
public sealed class RecordingWindow : Window
{
    private readonly RecordingService service;
    private readonly TextBlock status;
    private readonly Button stop;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Stopwatch elapsed = new();
    private bool stopping, completed, started;
    private readonly CancellationTokenSource countdown = new();
    public event Action<string>? Saved;
    public event Action? BeforeCapture;
    public bool HideBeforeRecording { get; set; }
    public RecordingWindow(RecordingService service)
    {
        Style = (Style)FindResource(typeof(Window));
        this.service = service; Title = "SnapMark · Recording"; Width = 340; Height = 175; Topmost = true; ResizeMode = ResizeMode.NoResize;
        var panel = new StackPanel { Margin = new Thickness(16) }; Content = panel;
        status = new TextBlock { Text = "Preparing recording…", Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(status);
        stop = new Button { Content = "Stop and save MP4", IsEnabled = false }; stop.Click += async (_, _) => await Stop(); panel.Children.Add(stop);
        panel.Children.Add(new TextBlock { Text = "MP4 · Saved locally · No upload", Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush"), Margin = new Thickness(4) });
        timer.Tick += async (_, _) => { status.Text = $"● Recording {elapsed.Elapsed:mm\\:ss}"; if (!service.IsRecording) await Stop(); };
        Closing += (_, e) => { if (completed) return; e.Cancel = true; Dispatcher.BeginInvoke(new Action(async () => { if (stop.IsEnabled) await Stop(); else { countdown.Cancel(); } })); };
    }
    public async Task Begin(RecordingRequest request)
    {
        try { for (var i = 3; i > 0; i--) { status.Text = $"Recording starts in {i}…"; await Task.Delay(1000, countdown.Token); } if (HideBeforeRecording) { Hide(); await Task.Delay(150, countdown.Token); } BeforeCapture?.Invoke(); await service.StartAsync(request); started = true; elapsed.Start(); timer.Start(); stop.IsEnabled = true; if (countdown.IsCancellationRequested) await Stop(); }
        catch (OperationCanceledException) { completed = true; Close(); }
        catch (Exception ex) { completed = true; MessageBox.Show(this, ex.Message, "Recording failed"); Close(); }
    }
    public Task StopRecording() => Stop();
    private async Task Stop()
    {
        if (!started) { countdown.Cancel(); return; }
        if (stopping) return; stopping = true; stop.IsEnabled = false; timer.Stop(); status.Text = "Finalizing MP4…";
        try { var path = await service.StopAsync(); Saved?.Invoke(path); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Recording failed"); }
        finally { completed = true; Close(); }
    }
}
