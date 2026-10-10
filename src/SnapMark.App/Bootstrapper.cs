using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using SnapMark.Capture;
using SnapMark.Core;
using SnapMark.Editor;
using SnapMark.Export;
using SnapMark.Recording;
using Forms = System.Windows.Forms;

namespace SnapMark.App;

// Composition root: capture and editor never reference each other.
public sealed class Bootstrapper : IDisposable
{
    private readonly ICaptureService capture = new CaptureService();
    private readonly IClipboardService clipboard = new ClipboardService();
    private readonly IExportService export = new ExportService();
    private readonly IAnnotationRenderer renderer = new AnnotationRenderer();
    private readonly ISettingsService settingsStore;
    private HistoryService history;
    private readonly string dataFolder;
    private readonly HotkeyService hotkeys = new();
    private readonly Forms.NotifyIcon tray;
    private readonly MainWindow main;
    private readonly List<EditorWindow> editors = [];
    private bool capturing;
    private bool isolatedInstance;
    private EditorWindow? activeEditor;
    private readonly RecordingService recording = new();
    private RecordingWindow? recordingWindow;
    private ScrollingWindow? scrollingWindow;
    private readonly RecoveryStore recovery;
    private readonly PluginCatalog plugins = new();
    public void ShowPlugins()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "SnapMark exporter assembly|*.dll", Title = "Load trusted local exporter · restart to unload" };
        if (dialog.ShowDialog() != true) return;
        if (MessageBox.Show("Plugins run local .NET code with your permissions. Load only code you trust.\n\n" + dialog.FileName, "SnapMark · Load plugin", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try { plugins.Load(dialog.FileName); main.SetStatus("Loaded exporters: " + string.Join(", ", plugins.Exporters.Select(p => p.Name))); }
        catch (Exception ex) { Report(ex, "Could not load plugin"); }
    }
    public AppSettings Settings { get; private set; }
    public Bootstrapper(string folder)
    {
        dataFolder = folder; settingsStore = new SettingsService(folder); Settings = settingsStore.Load();
        RecordingService.EncoderPath = Settings.FfmpegPath;
        ThemeService.Apply(Settings.Theme);
        history = new HistoryService(StoragePaths.History(Settings, folder), export);
        recovery = new RecoveryStore(StoragePaths.Recovery(Settings, folder));
        main = new MainWindow(this);
        using var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/SnapMark;component/Assets/SnapMark.ico")).Stream;
        using var sourceIcon = new System.Drawing.Icon(iconStream, 32, 32);
        tray = new Forms.NotifyIcon { Icon = (System.Drawing.Icon)sourceIcon.Clone(), Text = "SnapMark · Capture & markup", Visible = true };
        var menu = new Forms.ContextMenuStrip();
        void Item(string title, Action action) => menu.Items.Add(title, null, (_, _) => System.Windows.Application.Current.Dispatcher.Invoke(action));
        Item("Capture region", () => _ = Capture(CaptureMode.Region));
        Item("Quick copy region", () => _ = Capture(CaptureMode.Region, true));
        Item("Capture current monitor", () => _ = Capture(CaptureMode.Monitor));
        Item("Capture all screens", () => _ = Capture(CaptureMode.AllScreens));
        Item("Repeat last region", () => _ = Capture(CaptureMode.RepeatRegion));
        Item("Record region video", () => _ = RecordVideo());
        Item("Stop video", () => { if (recordingWindow is not null) _ = recordingWindow.StopRecording(); });
        menu.Items.Add(new Forms.ToolStripSeparator());
        Item("Open SnapMark", ShowMain); Item("History", ShowHistory); Item("Recover editable work", ShowRecovery); Item("Settings", ShowSettings);
        Item("Exit", ExitSafely);
        tray.ContextMenuStrip = menu; tray.DoubleClick += (_, _) => ShowMain();
    }
    public void Start(bool trayOnly, bool isolated = false)
    {
        isolatedInstance = isolated;
        if (!isolated) RegisterHotkeys(); else main.SetStatus("Isolated test instance · global hotkeys disabled");
        if (!trayOnly && !Settings.StartInTray) ShowMain();
        if (recovery.Entries().Count > 0) { main.SetStatus($"{recovery.Entries().Count} editable recovery copies available. Use Recover work."); Notify("Editable work is available in local recovery."); }
    }
    private void RegisterHotkeys()
    {
        if (isolatedInstance) return;
        hotkeys.Clear();
        var failures = new List<string>();
        void Register(int id, string name, Action action)
        {
            var label = Settings.Hotkeys.GetValueOrDefault(name, new AppSettings().Hotkeys[name]); if (string.IsNullOrWhiteSpace(label)) return;
            try { var gesture = HotkeyGesture.Parse(label); if (!hotkeys.Register(id, gesture.Modifiers, gesture.Key, action)) failures.Add(name + ": " + label); } catch { failures.Add(name + ": invalid shortcut"); }
        }
        Register(1, "Region", () => _ = Capture(CaptureMode.Region)); Register(2, "Quick", () => _ = Capture(CaptureMode.Region, true));
        Register(3, "Window", () => _ = Capture(CaptureMode.ActiveWindow)); Register(4, "Monitor", () => _ = Capture(CaptureMode.Monitor));
        Register(5, "Repeat", () => _ = Capture(CaptureMode.RepeatRegion)); Register(6, "History", ShowHistory);
        Register(7, "Fallback", () => _ = Capture(CaptureMode.Region)); Register(8, "StopVideo", () => { if (recordingWindow is not null) _ = recordingWindow.StopRecording(); });
        main.UpdateShortcuts(Settings);
        if (failures.Count > 0) { main.SetStatus("Hotkeys unavailable: " + string.Join(", ", failures) + ". Use Capture buttons or Ctrl+Shift+S."); Notify("Some hotkeys are already in use. Open SnapMark for details."); }
    }
    public void ShowMain() { main.Show(); main.WindowState = WindowState.Normal; main.Activate(); }
    public void ShowVideoTools() => new VideoToolsWindow(() => Settings.VideoFolder) { Owner = main.IsVisible ? main : null }.Show();
    public void ShowPhotoTools() => new PhotoToolsWindow(() => Settings.ExportFolder) { Owner = main.IsVisible ? main : null }.Show();
    public void ShowTeamFolders() => new TeamFolderWindow(Settings, () => settingsStore.Save(Settings)) { Owner = main.IsVisible ? main : null }.ShowDialog();
    public void ShowDiagnostics() => new DiagnosticsWindow { Owner = main.IsVisible ? main : null }.Show();
    public void ShowInstaller() => new InstallWindow { Owner = main.IsVisible ? main : null }.Show();
    public void ShowProfiles()
    {
        try
        {
        var dialog = new ProfilesWindow(Settings, new PresetStore(Path.Combine(dataFolder, "presets.json")).Load().Select(p => p.Name).ToArray()) { Owner = main.IsVisible ? main : null };
        if (dialog.ShowDialog() != true) return;
        try { Settings.Profiles = dialog.Profiles; Settings.ActiveProfile = dialog.Active; settingsStore.Save(Settings); main.SetStatus("Project profiles saved. Active: " + (dialog.Active.Length == 0 ? "Default settings" : dialog.Active)); }
        catch (Exception ex) { Report(ex, "Could not save profiles"); }
        } catch (Exception ex) { Report(ex, "Could not open profiles"); }
    }
    private ProjectProfile? ProfileFor(string app) => Settings.Profiles.FirstOrDefault(p => p.Applications.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Any(a => a.Equals(app, StringComparison.OrdinalIgnoreCase)))
        ?? Settings.Profiles.FirstOrDefault(p => p.Name == Settings.ActiveProfile);
    private string NextName(string kind, string sourceApp, ProjectProfile? profile)
    {
        var counter = checked(Settings.OutputCounter + 1); var name = OutputNaming.Create(profile?.FilenamePattern ?? Settings.FileNamePattern, kind, DateTime.Now, sourceApp, counter);
        Settings.OutputCounter = counter; settingsStore.Save(Settings); return name;
    }
    private string ImageFolder(ProjectProfile? profile)
    {
        var folder = string.IsNullOrWhiteSpace(profile?.ExportFolder) ? Settings.ExportFolder : profile.ExportFolder;
        return Settings.DateSubfolders ? Path.Combine(folder, DateTime.Now.ToString("yyyy"), DateTime.Now.ToString("MM"), DateTime.Now.ToString("dd")) : folder;
    }
    private async void ExitSafely()
    {
        try { if (recordingWindow is not null) { await recordingWindow.StopRecording(); if (recordingWindow is not null) return; } var open = editors.ToArray(); foreach (var editor in open) editor.PrepareForExit(); foreach (var editor in open) editor.CommitExit(); System.Windows.Application.Current.Shutdown(); }
        catch (Exception ex) { Report(ex, "Recovery could not be saved. SnapMark remains open."); }
    }
    public async Task ScrollCapture()
    {
        if (scrollingWindow is not null) { scrollingWindow.Activate(); return; }
        if (capturing || recording.IsRecording) return;
        capturing = true; var visible = editors.Cast<Window>().Append(main).Where(w => w.IsVisible).ToArray();
        try { foreach (var window in visible) window.Hide(); await Task.Delay(350); var result = await capture.CaptureAsync(new(CaptureMode.Region, AllowFreeform: false)); if (result is null) { foreach (var window in visible) window.Show(); return; }
            scrollingWindow = new ScrollingWindow(result.Image, result.SourceBounds, OpenEditor);
            scrollingWindow.Closed += (_, _) => { scrollingWindow = null; foreach (var window in visible) window.Show(); }; scrollingWindow.Show();
        } catch (Exception ex) { foreach (var window in visible) window.Show(); Report(ex, "Scrolling capture failed"); }
        finally { capturing = false; }
    }
    public async Task RecordVideo()
    {
        if (recordingWindow is not null) { recordingWindow.Activate(); return; }
        if (capturing || scrollingWindow is not null) return;
        if (RecordingService.FindEncoder() is null) { MessageBox.Show("Video requires FFmpeg. Open Settings and Browse to ffmpeg.exe, or clear the preference to search beside SnapMark.exe, tools, and PATH.", "SnapMark · Recording"); return; }
        var setup = new Window { Style = (Style)System.Windows.Application.Current.FindResource(typeof(Window)), Title = "SnapMark · Video capture", Width = 560, Height = 670, Owner = main.IsVisible ? main : null, WindowStartupLocation = WindowStartupLocation.CenterScreen };
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) }; setup.Content = new System.Windows.Controls.ScrollViewer { Content = panel, VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto };
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Capture video → local MP4", FontSize = 20, Margin = new Thickness(4, 0, 0, 12) });
        var mode = new System.Windows.Controls.ComboBox { ItemsSource = new[] { "Region", "Current monitor", "All screens", "Pick visible window (fixed region)" }, SelectedIndex = 0 }; panel.Children.Add(mode);
        var fps = new System.Windows.Controls.ComboBox { ItemsSource = new[] { 10, 15, 24, 30 }, SelectedItem = 15 }; panel.Children.Add(fps);
        var cursor = new System.Windows.Controls.CheckBox { Content = "Include cursor", IsChecked = true, Margin = new Thickness(4, 8, 4, 8), Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextBrush") }; panel.Children.Add(cursor); var highlight = new System.Windows.Controls.CheckBox { Content = "Cursor highlight", Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(highlight); var ripple = new System.Windows.Controls.CheckBox { Content = "Click ripple", Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(ripple);
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "MP4 resolution · physical pixels", Margin = new Thickness(4) });
        var resolution = new System.Windows.Controls.ComboBox { ItemsSource = new[] { "Native · same pixels as selected area", "720p maximum", "1080p maximum", "1440p maximum", "2160p maximum" }, SelectedIndex = 0 }; panel.Children.Add(resolution);
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Keeps aspect ratio; smaller areas are never enlarged. Odd native dimensions receive 1 px padding for H.264.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) });
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Quality", Margin = new Thickness(4) });
        var quality = new System.Windows.Controls.ComboBox { ItemsSource = new[] { "High · CRF 18", "Balanced · CRF 23", "Smaller file · CRF 28" }, SelectedIndex = 1 }; panel.Children.Add(quality);
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "AUDIO / เสียง", Margin = new Thickness(4) });
        var audio = new System.Windows.Controls.ComboBox { ItemsSource = Enum.GetValues<RecordingAudio>(), SelectedItem = RecordingAudio.None }; panel.Children.Add(audio);
        var system = new System.Windows.Controls.ComboBox { ToolTip = "System output device (WASAPI loopback)", IsEnabled = false }; panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "System output" }); panel.Children.Add(system);
        var microphone = new System.Windows.Controls.ComboBox { ToolTip = "Microphone input", IsEnabled = false }; panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "Microphone" }); panel.Children.Add(microphone);
        var audioStatus = new System.Windows.Controls.TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4), Text = "Choose an audio mode, then Test selected audio before recording." };
        void RefreshAudio()
        {
            void Fill(System.Windows.Controls.ComboBox picker, bool mic)
            {
                var previous = (picker.SelectedItem as AudioEndpoint)?.Id;
                try { var devices = AudioSession.Devices(mic); picker.ItemsSource = devices; picker.SelectedValuePath = "Id"; var preferred = previous ?? AudioSession.DefaultDeviceId(mic); picker.SelectedItem = devices.FirstOrDefault(d => d.Id == preferred) ?? devices.FirstOrDefault(); }
                catch (Exception ex) { audioStatus.Text = $"{(mic ? "Microphone" : "System output")}: {ex.Message}"; }
            }
            Fill(system, false); Fill(microphone, true);
        }
        RefreshAudio();
        audio.SelectionChanged += (_, _) => { var selected = (RecordingAudio)audio.SelectedItem; system.IsEnabled = selected is RecordingAudio.System or RecordingAudio.Both; microphone.IsEnabled = selected is RecordingAudio.Microphone or RecordingAudio.Both; };
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = "System records the selected speaker/headphone output; Both mixes it with the selected microphone. No audio is the default. Recording starts after 3 seconds.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 8, 4, 8) });
        var begin = new System.Windows.Controls.Button { Content = "Choose and adjust capture area / เลือกและปรับกรอบ" }; begin.Click += (_, _) => setup.DialogResult = true; panel.Children.Add(begin);
        var audioActions = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal }; panel.Children.Insert(panel.Children.Count - 1, audioActions);
        var refreshAudio = new System.Windows.Controls.Button { Content = "Refresh audio devices" }; var testAudio = new System.Windows.Controls.Button { Content = "Test selected audio · 2 sec" }; audioActions.Children.Add(refreshAudio); audioActions.Children.Add(testAudio); panel.Children.Insert(panel.Children.Count - 1, audioStatus);
        refreshAudio.Click += (_, _) => { audioStatus.Text = "Audio devices refreshed."; RefreshAudio(); };
        testAudio.Click += async (_, _) =>
        {
            var selectedMode = (RecordingAudio)audio.SelectedItem;
            if (selectedMode == RecordingAudio.None) { audioStatus.Text = "Choose System, Microphone or Both to test."; return; }
            if ((selectedMode is RecordingAudio.System or RecordingAudio.Both) && system.SelectedItem is null || (selectedMode is RecordingAudio.Microphone or RecordingAudio.Both) && microphone.SelectedItem is null) { audioStatus.Text = "Select available devices or refresh the device list."; return; }
            var prefix = Path.Combine(Path.GetTempPath(), "SnapMark-audio-test-" + Guid.NewGuid().ToString("N"));
            using var worker = new AudioWorker(); testAudio.IsEnabled = refreshAudio.IsEnabled = begin.IsEnabled = false; audioStatus.Text = "Opening selected audio devices…";
            try { await worker.Start(new(selectedMode, prefix, (system.SelectedItem as AudioEndpoint)?.Id, (microphone.SelectedItem as AudioEndpoint)?.Id)); audioStatus.Text = "Capturing a 2 second local test…"; await Task.Delay(2000); await worker.Stop(); audioStatus.Text = "Devices opened and test finalized. For voice quality, record a short MP4 and listen to it."; }
            catch (Exception ex) { audioStatus.Text = ex.Message.Split('\n')[0]; }
            finally { testAudio.IsEnabled = refreshAudio.IsEnabled = begin.IsEnabled = true; foreach (var suffix in new[] { ".system.wav", ".microphone.wav" }) { try { File.Delete(prefix + suffix); } catch { } } }
        };
        if (setup.ShowDialog() != true) return;
        var audioMode = (RecordingAudio)audio.SelectedItem;
        if ((audioMode is RecordingAudio.System or RecordingAudio.Both) && system.SelectedItem is null || (audioMode is RecordingAudio.Microphone or RecordingAudio.Both) && microphone.SelectedItem is null) { MessageBox.Show("The selected audio mode needs an available output/input device.", "Video capture"); return; }
        string videoFolder; try { videoFolder = EnsureFolder(Settings.VideoFolder); } catch (Exception ex) { Report(ex, "Video folder unavailable"); return; }
        var file = new Microsoft.Win32.SaveFileDialog { Filter = "MP4 video|*.mp4", FileName = NextName("video", "capture", ProfileFor("")) + ".mp4", DefaultExt = ".mp4", InitialDirectory = videoFolder };
        capturing = true;
        using var hiddenWindows = new CaptureWindowVisibility(editors.Cast<Window>().Append(main));
        try
        {
            await Task.Delay(350);
            var result = await capture.CaptureAsync(new(mode.SelectedIndex == 0 ? CaptureMode.Region : mode.SelectedIndex == 1 ? CaptureMode.Monitor : mode.SelectedIndex == 3 ? CaptureMode.WindowPick : CaptureMode.AllScreens, AllowFreeform: false, ConfirmRegion: true));
            if (result is null) return;
            hiddenWindows.Restore();
            if (file.ShowDialog() != true) return;
            var request = new RecordingRequest(result.SourceBounds, file.FileName, fps.SelectedItem is int frameRate ? frameRate : 15, cursor.IsChecked == true,
                Audio: audioMode, SystemDeviceId: (system.SelectedItem as AudioEndpoint)?.Id, MicrophoneDeviceId: (microphone.SelectedItem as AudioEndpoint)?.Id,
                MaximumHeight: new[] { 0, 720, 1080, 1440, 2160 }[resolution.SelectedIndex], Quality: new[] { 18, 23, 28 }[quality.SelectedIndex]);
            var size = RecordingService.OutputSize(request);
            if (MessageBox.Show($"Capture: {result.SourceBounds.Width:0} × {result.SourceBounds.Height:0} px\nMP4: {size.Width} × {size.Height} px · {request.FramesPerSecond} fps\nAudio: {audioMode}\nStart recording?", "SnapMark · Recording size", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
            // Region selection is finished. Existing editors must remain usable during recording.
            // Restore once here; stopping must not reopen editors the user has since closed/hidden.
            hiddenWindows.Restore();
            var control = new RecordingWindow(recording); recordingWindow = control; CursorOverlayWindow? cursorOverlay = null; control.BeforeCapture += () => { if (highlight.IsChecked == true || ripple.IsChecked == true) { cursorOverlay = new CursorOverlayWindow(result.SourceBounds, highlight.IsChecked == true, ripple.IsChecked == true); cursorOverlay.Show(); } };
            var outside = NativeDesktop.Monitors.SelectMany(m => new[] { new PixelRect(m.X, m.Y, 400, 210), new(m.Right - 400, m.Bottom - 210, 400, 210) }).FirstOrDefault(b => b.Intersect(result.SourceBounds).Width * b.Intersect(result.SourceBounds).Height == 0);
            if (outside.Width > 0) control.SourceInitialized += (_, _) => NativeDesktop.Position(control, outside);
            control.Saved += path => { Notify("Video saved: " + path); main.SetStatus("Video saved: " + path); };
            var frame = new RecordingFrame(); control.BeforeCapture += () => frame.Show(result.SourceBounds);
            control.Closed += (_, _) => {
                frame.Dispose(); cursorOverlay?.Close(); recordingWindow = null;
                main.Dispatcher.BeginInvoke(new Action(() => CaptureWindowVisibility.ReturnToEditor(activeEditor is { } active && editors.Contains(active) ? active : editors.LastOrDefault(), main)));
            };
            control.HideBeforeRecording = outside.Width == 0;
            control.Show(); await control.Begin(request);
            if (recording.IsRecording && outside.Width == 0) { control.Hide(); Notify("Recording video. Stop with " + Settings.Hotkeys.GetValueOrDefault("StopVideo", "tray menu") + " or the tray menu."); }
        }
        catch (Exception ex) { hiddenWindows.Restore(); Report(ex, "Video capture failed"); }
        finally { capturing = false; }
    }
    public async Task Capture(CaptureMode mode, bool quick = false)
    {
        if (capturing || scrollingWindow is not null) return;
        capturing = true;
        var foreground = NativeDesktop.GetForegroundWindow();
        var metadata = NativeDesktop.WindowMetadata(foreground);
        var visible = editors.Cast<Window>().Append(main).Where(w => w.IsVisible).ToArray();
        try
        {
            foreach (var window in visible) window.Hide();
            await Task.Delay(350); // Allow desktop composition to remove our windows before freezing.
            if (Settings.CaptureDelaySeconds > 0) { Notify($"Capture in {Settings.CaptureDelaySeconds} seconds. Esc cancels the region overlay afterward."); await Task.Delay(TimeSpan.FromSeconds(Settings.CaptureDelaySeconds)); }
            var result = await capture.CaptureAsync(new(mode, foreground));
            if (result is null) { foreach (var w in visible) w.Show(); return; }
            if (Settings.SoundFeedback) System.Media.SystemSounds.Asterisk.Play();
            if (quick || Settings.AutoCopy)
            {
                try { await clipboard.CopyAsync(result.Image); }
                catch (Exception ex) { Report(ex, "Copy failed; the capture is still available in the editor."); quick = false; }
            }
            foreach (var w in visible) w.Show();
            try { if (Settings.KeepHistory) { history.Add(result.Image, Settings.HistoryLimit, metadata.App, metadata.Title, Settings.HistoryDays); foreach (var editor in editors) _ = editor.RefreshInlineHistory(); } }
            catch (Exception ex) { Report(ex, "Capture succeeded, but history could not be saved."); }
            if (quick) Notify($"Copied {result.Image.PixelWidth} × {result.Image.PixelHeight}");
            else OpenEditor(result.Image, null, sourceApp: metadata.App);
        }
        catch (Exception ex) { foreach (var w in visible) w.Show(); Report(ex, "Capture failed"); }
        finally { capturing = false; }
    }
    public void OpenEditor(BitmapSource image) => OpenEditor(image, null);
    private void OpenEditor(BitmapSource image, EditorDocument? document, Guid? recoveryId = null, string sourceApp = "capture")
    {
        if (recoveryId is { } id && editors.FirstOrDefault(w => w.RecoveryId == id) is { } existing) { existing.Show(); existing.Activate(); return; }
        if (!image.IsFrozen && image.CanFreeze) image.Freeze();
        var profile = ProfileFor(sourceApp);
        var window = new EditorWindow(image, profile?.Color ?? Settings.DefaultColor, clipboard, export, renderer, Settings.ExportFolder, PickHistory, document, Settings.Autosave ? recovery : null, recoveryId, kind => NextName(kind, sourceApp, ProfileFor(sourceApp)), new PresetStore(Path.Combine(dataFolder, "presets.json")), new SymbolLibrary(Path.Combine(dataFolder, "symbols")), kind => kind == "project" ? Settings.ProjectFolder : ImageFolder(ProfileFor(sourceApp)), plugins, () => Settings, () => settingsStore.Save(Settings), () => history.Entries(), async entry => { await history.RecycleAsync(entry); foreach (var editor in editors) _ = editor.RefreshInlineHistory(); }, (entry, name) => { history.Rename(entry, name); foreach (var editor in editors) _ = editor.RefreshInlineHistory(); }, EditorLayoutStore.Load(Path.Combine(dataFolder, "editor-layout.json")));
        if (!string.IsNullOrWhiteSpace(profile?.StylePresetName)) window.ApplyPersonalPreset(profile.StylePresetName); editors.Add(window); window.Activated += (_, _) => activeEditor = window;
        window.Closed += (_, _) => { editors.Remove(window); if (activeEditor == window) activeEditor = editors.LastOrDefault(); }; window.Show(); window.Activate();
    }
    public void OpenImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Images / PDF / SnapMark projects|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.pdf;*.snapmark" };
        if (dialog.ShowDialog() == true) OpenImagePath(dialog.FileName);
    }
    public void OpenImagePath(string path)
    {
        try { if (Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase)) new PdfImportWindow(path, OpenEditor) { Owner = main.IsVisible ? main : null }.Show(); else if (Path.GetExtension(path).Equals(".snapmark", StringComparison.OrdinalIgnoreCase)) { var loaded = ProjectStorage.Load(path); OpenEditor(loaded.Image, loaded.Document); } else OpenEditor(HistoryService.LoadImage(path)); }
        catch (Exception ex) { Report(ex, "Could not open image"); }
    }
    public void PasteImage()
    {
        try { var image = System.Windows.Clipboard.GetImage(); if (image is not null) OpenEditor(image); else main.SetStatus("The clipboard does not contain an image."); }
        catch (Exception ex) { Report(ex, "Could not read clipboard"); }
    }
    public void ShowHistory()
    {
        try
        {
            var target = activeEditor;
            var window = new HistoryPickerWindow(history.Entries(), clipboard, target is null ? null : image => { target.InsertImage(image); target.Activate(); }, OpenEditor, sourceFolder: history.FolderPath);
            window.Show();
        }
        catch (Exception ex) { Report(ex, "Could not load history"); }
    }
    private BitmapSource? PickHistory(Window owner)
    {
        try { var picker = new HistoryPickerWindow(history.Entries(), clipboard, picker: true, sourceFolder: history.FolderPath) { Owner = owner }; return picker.ShowDialog() == true ? picker.SelectedImage : null; }
        catch (Exception ex) { Report(ex, "Could not load history"); return null; }
    }
    public void ShowSettings()
    {
        var dialog = new SettingsWindow(Settings, dataFolder) { Owner = main.IsVisible ? main : null };
        if (dialog.ShowDialog() != true) return;
        try { var restartRecovery = StoragePaths.Recovery(Settings, dataFolder) != StoragePaths.Recovery(dialog.Value, dataFolder) || Settings.Theme != dialog.Value.Theme; if (Settings.RunAtStartup != dialog.Value.RunAtStartup) LocalIntegration.SetStartup(dialog.Value.RunAtStartup); settingsStore.Save(dialog.Value); Settings = dialog.Value; RecordingService.EncoderPath = Settings.FfmpegPath; history = new HistoryService(StoragePaths.History(Settings, dataFolder), export); main.SetStatus(restartRecovery ? "Settings saved. Restart SnapMark to apply the theme / recovery folder changes." : "Settings saved. Storage folders applied."); RegisterHotkeys(); }
        catch (Exception ex) { Report(ex, "Could not save settings"); }
    }
    public void ShowRecovery()
    {
        var window = new RecoveryWindow(recovery, entry => { var project = recovery.Load(entry.Id); OpenEditor(project.Image, project.Document, entry.Id); }, id => editors.Any(e => e.RecoveryId == id)) { Owner = main.IsVisible ? main : null };
        window.Show();
    }
    private static string EnsureFolder(string path) { Directory.CreateDirectory(path); return path; }
    private void Notify(string text) { tray.BalloonTipTitle = "SnapMark"; tray.BalloonTipText = text; tray.ShowBalloonTip(2000); }
    private void Report(Exception ex, string context)
    {
        main.SetStatus(context + ": " + ex.Message); Notify(context + ": " + ex.Message);
        try { Directory.CreateDirectory(dataFolder); File.AppendAllText(Path.Combine(dataFolder, "snapmark.log"), $"{DateTimeOffset.Now:O} {context}: {ex.GetType().Name}\n"); } catch { }
    }
    public void Dispose()
    {
        foreach (var editor in editors.ToArray()) editor.FlushRecoveryOnExit();
        if (recording.IsRecording) { try { recording.StopAsync().GetAwaiter().GetResult(); } catch (Exception ex) { Report(ex, "Could not finalize recording on exit"); } }
        hotkeys.Dispose(); tray.Visible = false; var icon = tray.Icon; tray.Dispose(); icon?.Dispose();
    }
}
