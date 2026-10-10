using System.Windows;
using System.Windows.Controls;
using SnapMark.Core;

namespace SnapMark.App;
public sealed class SettingsWindow : Window
{
    public AppSettings Value { get; private set; }
    public SettingsWindow(AppSettings settings, string dataFolder)
    {
        Style = (Style)FindResource(typeof(Window));
        Value = settings; Title = "SnapMark · Settings"; Width = 620; Height = 740; MinHeight = 450; WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var panel = new StackPanel { Margin = new Thickness(24) }; Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var copy = new CheckBox { Content = "Copy captures automatically", IsChecked = settings.AutoCopy, Margin = new Thickness(4, 4, 4, 16), Foreground = (System.Windows.Media.Brush)FindResource("TextBrush") };
        var tray = new CheckBox { Content = "Start in tray next time", IsChecked = settings.StartInTray, Margin = new Thickness(4, 4, 4, 16), Foreground = (System.Windows.Media.Brush)FindResource("TextBrush") };
        panel.Children.Add(copy); panel.Children.Add(tray);
        panel.Children.Add(new TextBlock { Text = "Theme · applies after restarting SnapMark" });
        var theme = new ComboBox { ItemsSource = new[] { "Dark", "Light", "System" }, SelectedItem = settings.Theme }; panel.Children.Add(theme);
        var sound = new CheckBox { Content = "Capture sound", IsChecked = settings.SoundFeedback, Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(sound);
        var startup = new CheckBox { Content = "Run SnapMark at Windows sign-in (this executable path)", IsChecked = settings.RunAtStartup, Margin = new Thickness(4), Foreground = (System.Windows.Media.Brush)FindResource("TextBrush") }; panel.Children.Add(startup);
        var explorer = new Button { Content = "Register Explorer: Edit with SnapMark" }; panel.Children.Add(explorer); explorer.Click += (_, _) => { try { LocalIntegration.RegisterExplorer(); MessageBox.Show(this, "Registered for images in this Windows user account. Keep this executable at its current path.", "SnapMark"); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Explorer integration"); } };
        var keepHistory = new CheckBox { Content = "Keep original captures in local history", IsChecked = settings.KeepHistory, Margin = new Thickness(4, 4, 4, 12), Foreground = (System.Windows.Media.Brush)FindResource("TextBrush") };
        panel.Children.Add(keepHistory);
        var autosave = new CheckBox { Content = "Autosave editable recovery (new editors)", IsChecked = settings.Autosave, Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"), Margin = new Thickness(4, 4, 4, 12) }; panel.Children.Add(autosave);
        panel.Children.Add(new TextBlock { Text = "STORAGE / โฟลเดอร์เริ่มต้น", FontWeight = FontWeights.SemiBold, Foreground = System.Windows.Media.Brushes.DeepSkyBlue, Margin = new Thickness(4, 12, 4, 8) });
        var defaults = new AppSettings();
        TextBox FolderRow(string label, string value, string defaultValue)
        {
            panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(4, 8, 4, 0) });
            var row = new DockPanel(); panel.Children.Add(row);
            var browse = new Button { Content = "Browse…", ToolTip = "Choose " + label }; DockPanel.SetDock(browse, Dock.Right); row.Children.Add(browse);
            var reset = new Button { Content = "Default", ToolTip = defaultValue }; DockPanel.SetDock(reset, Dock.Right); row.Children.Add(reset);
            var input = new TextBox { Text = value, ToolTip = label, MinWidth = 120 }; row.Children.Add(input);
            FieldHelp.Hint(panel, input, label + " · Folder path", @"เลือก Browse หรือกรอกโฟลเดอร์ในเครื่อง เช่น D:\SnapMark\Images · ไม่ใช่ชื่อไฟล์หรือ URL");
            reset.Click += (_, _) => input.Text = defaultValue;
            browse.Click += (_, _) => { try { var picker = new Microsoft.Win32.OpenFolderDialog { Title = label, InitialDirectory = System.IO.Directory.Exists(input.Text) ? input.Text : System.IO.Directory.Exists(dataFolder) ? dataFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) }; if (picker.ShowDialog(this) == true) input.Text = picker.FolderName; } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Folder"); } };
            return input;
        }
        var folder = FolderRow("Image / PDF export", settings.ExportFolder, defaults.ExportFolder);
        var videoFolder = FolderRow("Video recording / Trim / GIF", settings.VideoFolder, defaults.VideoFolder);
        var projectFolder = FolderRow("Editable projects (.snapmark)", settings.ProjectFolder, defaults.ProjectFolder);
        var historyFolder = FolderRow("Capture history · applies immediately", StoragePaths.History(settings, dataFolder), StoragePaths.History(defaults, dataFolder));
        var recoveryFolder = FolderRow("Autosave recovery · applies after restarting SnapMark", StoragePaths.Recovery(settings, dataFolder), StoragePaths.Recovery(defaults, dataFolder));
        panel.Children.Add(new TextBlock { Text = "เปลี่ยน path เพื่อใช้โฟลเดอร์ใหม่ ไฟล์เดิมยังอยู่ที่เดิมและไม่ได้ย้ายอัตโนมัติ · Default ของ history/recovery อยู่ใน data ของโปรแกรม", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 8, 4, 8), Foreground = System.Windows.Media.Brushes.LightSlateGray });
        panel.Children.Add(new TextBlock { Text = "VIDEO ENCODER / ตัวเข้ารหัสวิดีโอ", FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 14, 4, 4) });
        var encoderRow = new DockPanel(); panel.Children.Add(encoderRow);
        var encoderBrowse = new Button { Content = "Browse…" }; DockPanel.SetDock(encoderBrowse, Dock.Right); encoderRow.Children.Add(encoderBrowse);
        var encoderAuto = new Button { Content = "Auto" }; DockPanel.SetDock(encoderAuto, Dock.Right); encoderRow.Children.Add(encoderAuto);
        var encoderPath = new TextBox { Text = settings.FfmpegPath }; encoderRow.Children.Add(encoderPath);
        FieldHelp.Hint(panel, encoderPath, "FFmpeg executable · File path", @"เลือกไฟล์ ffmpeg.exe เช่น D:\Tools\ffmpeg\bin\ffmpeg.exe · เว้นว่างเพื่อค้นหาอัตโนมัติ ไม่ใช่โฟลเดอร์หรือ URL");
        var encoderStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) }; panel.Children.Add(encoderStatus);
        void EncoderStatus()
        {
            var path = encoderPath.Text.Trim();
            var found = path.Length == 0 ? "Auto: searches beside SnapMark.exe, tools, then PATH" : System.IO.File.Exists(path) && System.IO.Path.IsPathFullyQualified(path) && System.IO.Path.GetFileName(path).Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase) ? "File found · recording will verify encoder capabilities" : "File not found or not an absolute ffmpeg.exe path";
            encoderStatus.Text = found;
        }
        encoderPath.TextChanged += (_, _) => EncoderStatus(); EncoderStatus();
        encoderAuto.Click += (_, _) => encoderPath.Text = "";
        encoderBrowse.Click += (_, _) => { var picker = new Microsoft.Win32.OpenFileDialog { Title = "Select trusted ffmpeg.exe", Filter = "FFmpeg executable|ffmpeg.exe", CheckFileExists = true }; if (picker.ShowDialog(this) == true) encoderPath.Text = picker.FileName; };
        panel.Children.Add(new TextBlock { Text = "History limit" });
        var limit = new ComboBox { ItemsSource = new[] { 10, 20, 50, 100 }, SelectedItem = settings.HistoryLimit }; if (limit.SelectedItem is null) limit.SelectedIndex = 1; panel.Children.Add(limit);
        panel.Children.Add(new TextBlock { Text = "History age limit · 0 = count limit only (removes expired captures on next capture)" });
        var days = new ComboBox { ItemsSource = new[] { 0, 7, 30 }, SelectedItem = settings.HistoryDays }; if (days.SelectedItem is null) days.SelectedIndex = 0; panel.Children.Add(days);
        var dateFolders = new CheckBox { Content = "Image exports in year / month / day folders", IsChecked = settings.DateSubfolders, Foreground = (System.Windows.Media.Brush)FindResource("TextBrush"), Margin = new Thickness(4) }; panel.Children.Add(dateFolders);
        panel.Children.Add(new TextBlock { Text = "CAPTURE / OUTPUT", FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 18, 4, 8), Foreground = System.Windows.Media.Brushes.DeepSkyBlue });
        panel.Children.Add(new TextBlock { Text = "Delay before freezing screen (seconds)" });
        var delay = new ComboBox { ItemsSource = Enumerable.Range(0, 11), SelectedItem = settings.CaptureDelaySeconds }; panel.Children.Add(delay);
        panel.Children.Add(new TextBlock { Text = "Filename pattern · {date}, {time}, {kind}, {app}, {counter}" });
        var naming = new TextBox { Text = settings.FileNamePattern }; panel.Children.Add(naming);
        FieldHelp.Hint(panel, naming, "รูปแบบชื่อไฟล์ / Filename pattern", "กรอกชื่อพร้อมตัวแปร เช่น SnapMark_{date}_{time} · ไม่ต้องใส่โฟลเดอร์หรือนามสกุลไฟล์");
        panel.Children.Add(new TextBlock { Text = "HOTKEYS · Leave blank to disable", FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 18, 4, 8), Foreground = System.Windows.Media.Brushes.DeepSkyBlue });
        var bindings = new Dictionary<string, TextBox>();
        foreach (var binding in new AppSettings().Hotkeys)
        {
            var row = new DockPanel(); var label = new TextBlock { Text = binding.Key, Width = 100, Margin = new Thickness(4, 12, 4, 4) }; DockPanel.SetDock(label, Dock.Left); row.Children.Add(label);
            var input = new TextBox { Text = settings.Hotkeys.GetValueOrDefault(binding.Key, binding.Value) }; row.Children.Add(input); bindings.Add(binding.Key, input); panel.Children.Add(row);
            System.Windows.Automation.AutomationProperties.SetName(input, binding.Key + " shortcut"); input.ToolTip = "ปุ่มลัด เช่น Ctrl+Shift+S หรือ PrintScreen · เว้นว่างเพื่อปิดคำสั่งนี้";
        }
        panel.Children.Add(new TextBlock { Text = "History contains originals, before blur/cover. Turn history off before a sensitive capture. Existing history is kept.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 8, 4, 8), Foreground = System.Windows.Media.Brushes.LightSlateGray });
        var save = new Button { Content = "Save settings", HorizontalAlignment = HorizontalAlignment.Right }; panel.Children.Add(save);
        save.Click += (_, _) =>
        {
            try
            {
                foreach (var input in new[] { folder, videoFolder, projectFolder, historyFolder, recoveryFolder }) input.Text = StoragePaths.Validate(input.Text);
                OutputNaming.Create(naming.Text, "image", DateTime.Now);
                var encoder = encoderPath.Text.Trim();
                if (encoder.Length > 0 && (!System.IO.Path.IsPathFullyQualified(encoder) || !System.IO.Path.GetFileName(encoder).Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(encoder))) throw new ArgumentException("Choose an existing absolute ffmpeg.exe file path, or use Auto.");
                var gestures = bindings.Values.Where(b => !string.IsNullOrWhiteSpace(b.Text)).Select(b => SnapMark.Capture.HotkeyGesture.Parse(b.Text)).ToArray();
                if (gestures.Select(g => (g.Modifiers, g.Key)).Distinct().Count() != gestures.Length) throw new ArgumentException("Two actions use the same shortcut.");
                foreach (var input in new[] { folder, videoFolder, projectFolder, historyFolder, recoveryFolder }) System.IO.Directory.CreateDirectory(input.Text);
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Settings"); return; }
            Value = new() { Hotkeys = bindings.ToDictionary(b => b.Key, b => b.Value.Text.Trim()), CaptureDelaySeconds = delay.SelectedItem is int seconds ? seconds : 0, FileNamePattern = naming.Text.Trim(), Autosave = autosave.IsChecked == true, AutoCopy = copy.IsChecked == true, KeepHistory = keepHistory.IsChecked == true, StartInTray = tray.IsChecked == true, ExportFolder = folder.Text, VideoFolder = videoFolder.Text, ProjectFolder = projectFolder.Text, HistoryFolder = StoragePaths.DataSetting(historyFolder.Text, StoragePaths.History(defaults, dataFolder)), RecoveryFolder = StoragePaths.DataSetting(recoveryFolder.Text, StoragePaths.Recovery(defaults, dataFolder)), HistoryLimit = limit.SelectedItem is int count ? count : 20, DefaultColor = settings.DefaultColor };
            Value.HistoryDays = days.SelectedItem is int age ? age : 0; Value.DateSubfolders = dateFolders.IsChecked == true;
            Value.Profiles = settings.Profiles; Value.ActiveProfile = settings.ActiveProfile; Value.OutputCounter = settings.OutputCounter;
            Value.RunAtStartup = startup.IsChecked == true;
            Value.FfmpegPath = encoderPath.Text.Trim();
            Value.Theme = theme.SelectedItem as string ?? "Dark"; Value.SoundFeedback = sound.IsChecked == true;
            Value.GoogleDriveFolder = settings.GoogleDriveFolder; Value.GoogleDriveShareUrl = settings.GoogleDriveShareUrl; Value.OneDriveFolder = settings.OneDriveFolder; Value.OneDriveShareUrl = settings.OneDriveShareUrl;
            DialogResult = true;
        };
    }
}
