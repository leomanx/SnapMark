using System.IO;
using System.Threading;
using System.Windows;

namespace SnapMark.App;
public partial class App : System.Windows.Application
{
    private Mutex? instance;
    private Bootstrapper? bootstrapper;
    private readonly InstanceMessages messages = new();
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Length == 2 && e.Args[0] == "--audio-worker") { Shutdown(Task.Run(() => SnapMark.Recording.AudioWorker.Run(e.Args[1])).GetAwaiter().GetResult()); return; }
        base.OnStartup(e);
        var isolated = e.Args.Contains("--isolated");
        instance = new Mutex(true, isolated ? "Local\\SnapMark.Test." + Environment.ProcessId : "Local\\SnapMark.Desktop", out var first);
        if (!first) { var openIndex = Array.IndexOf(e.Args, "--open"); var request = openIndex >= 0 && openIndex + 1 < e.Args.Length ? Path.GetFullPath(e.Args[openIndex + 1]) : "SHOW_MAIN"; if (!InstanceMessages.Send(request)) MessageBox.Show("SnapMark is already running. Use its tray icon or capture hotkeys.", "SnapMark"); Shutdown(); return; }
        var portable = e.Args.Contains("--portable") || File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.flag"));
        var folder = portable ? Path.Combine(AppContext.BaseDirectory, "data") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SnapMark");
        try
        {
            bootstrapper = new Bootstrapper(folder); bootstrapper.Start(e.Args.Contains("--tray"), isolated);
            if (!isolated) messages.Start(request => Dispatcher.BeginInvoke(new Action(() => { if (request == "SHOW_MAIN") bootstrapper.ShowMain(); else if (File.Exists(request)) bootstrapper.OpenImagePath(request); })));
            var open = Array.IndexOf(e.Args, "--open");
            if (open >= 0 && open + 1 < e.Args.Length) bootstrapper.OpenImagePath(e.Args[open + 1]);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "SnapMark could not start"); Shutdown(1); }
    }
    protected override void OnExit(ExitEventArgs e) { messages.Dispose(); bootstrapper?.Dispose(); instance?.Dispose(); base.OnExit(e); }
}
