using System.ComponentModel;
using System.Windows;
using SnapMark.Core;

namespace SnapMark.App;
public partial class MainWindow : Window
{
    private readonly Bootstrapper app;
    private readonly ViewModels.MainWindowViewModel viewModel = new();
    public MainWindow(Bootstrapper app)
    {
        this.app = app; InitializeComponent(); DataContext = viewModel; AllowDrop = true;
        VersionLabel.Text = "v" + typeof(MainWindow).Assembly.GetName().Version?.ToString(3);
        ConfigureIcons();
        DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        Drop += (_, e) => { if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) foreach (var path in paths.Take(8)) app.OpenImagePath(path); };
    }
    public void UpdateShortcuts(AppSettings settings)
    {
        string Label(string name) => settings.Hotkeys.GetValueOrDefault(name, "");
        void Shortcut(System.Windows.Controls.Button button, string hint, string key)
        {
            var text = hint + "\n" + Label(key); button.ToolTip = text;
            System.Windows.Automation.AutomationProperties.SetName(button, text);
        }
        Shortcut(RegionButton, "ลากกรอบเลือกพื้นที่ · W เลือกหน้าต่าง", "Region");
        Shortcut(QuickButton, "จับพื้นที่แล้ว copy ทันที", "Quick");
        Shortcut(MonitorButton, "จับเฉพาะจอที่เมาส์อยู่", "Monitor");
        Shortcut(RepeatButton, "จับกรอบพื้นที่เดิมซ้ำ", "Repeat");
        Shortcut(HistoryButton, "เปิดภาพ capture ย้อนหลัง", "History");
        ShortcutHint.Text = $"{Label("Region")}  จับภาพ  ·  W  เลือกหน้าต่าง  ·  {Label("Window")}  หน้าต่างปัจจุบัน";
    }
    private void ConfigureIcons()
    {
        foreach (var item in new (System.Windows.Controls.Button Button, string Icon, string Label, string Hint)[]
        {
            (RegionButton, "capture", "ลากกรอบ", "ลากกรอบจับภาพ · PrintScreen · W เลือกหน้าต่าง"),
            (QuickButton, "quick", "Copy ทันที", "เลือกพื้นที่แล้ว copy · Shift+PrintScreen"),
            (WindowButton, "window", "หน้าต่าง", "ชี้หน้าต่างโปรแกรมแล้วคลิกจับภาพ"),
            (MonitorButton, "monitor", "จอเดียว", "จับจอที่เมาส์อยู่ · Ctrl+PrintScreen"),
            (AllButton, "screens", "ทุกจอ", "จับภาพรวมทุกจอ"),
            (RepeatButton, "repeat", "กรอบเดิม", "จับพื้นที่เดิมซ้ำ · Ctrl+Shift+R"),
            (ScrollButton, "scroll", "เลื่อนหน้า", "Scrolling capture / จับภาพหน้าเลื่อนยาว"),
            (VideoButton, "video", "อัดวิดีโอ", "เลือกและปรับกรอบพื้นที่อัด MP4"),
            (TrimButton, "trim", "Trim / GIF", "ตัดช่วงวิดีโอหรือส่งออก GIF"),
            (OpenButton, "open", "เปิดไฟล์", "เปิดภาพ, PDF หรือ editable project"),
            (PhotoButton, "image", "Photo Tools", "Resize, convert, monochrome, animated GIF and print layouts · single / batch"),
            (PasteButton, "paste", "Paste", "เปิดภาพจาก clipboard"),
            (HistoryButton, "history", "History", "ภาพ capture ย้อนหลัง · Ctrl+Shift+V"),
            (RecoveryButton, "recover", "กู้คืน", "เปิดงานแก้ไขที่เก็บใน recovery"),
            (TeamButton, "team", "ทีม", "โฟลเดอร์ทีม Google Drive / OneDrive"),
            (SettingsButton, "settings", "ตั้งค่า", "Settings / ปุ่มลัด, ธีม และโฟลเดอร์"),
            (ProfilesButton, "profiles", "Profiles", "ตั้งค่าเฉพาะโปรเจกต์และโปรแกรม"),
            (DiagnosticsButton, "diagnostics", "ตรวจระบบ", "Diagnostics / ตรวจสถานะการทำงาน"),
            (PluginsButton, "plugin", "Plugins", "โหลด exporter plugin"),
            (InstallButton, "update", "อัปเดต", "ติดตั้งหรืออัปเดตจาก local package")
        }) ToolIcon.SetButton(item.Button, item.Icon, item.Label, item.Hint);
    }
    public void SetStatus(string message) => viewModel.Status = message;
    protected override void OnClosing(CancelEventArgs e) { e.Cancel = true; Hide(); }
    private void Region_Click(object sender, RoutedEventArgs e) => _ = app.Capture(CaptureMode.Region);
    private void Quick_Click(object sender, RoutedEventArgs e) => _ = app.Capture(CaptureMode.Region, true);
    private void Monitor_Click(object sender, RoutedEventArgs e) => _ = app.Capture(CaptureMode.Monitor);
    private void All_Click(object sender, RoutedEventArgs e) => _ = app.Capture(CaptureMode.AllScreens);
    private void Video_Click(object sender, RoutedEventArgs e) => _ = app.RecordVideo();
    private void Scrolling_Click(object sender, RoutedEventArgs e) => _ = app.ScrollCapture();
    private void VideoTools_Click(object sender, RoutedEventArgs e) => app.ShowVideoTools();
    private void PhotoTools_Click(object sender, RoutedEventArgs e) => app.ShowPhotoTools();
    private void Diagnostics_Click(object sender, RoutedEventArgs e) => app.ShowDiagnostics();
    private void Repeat_Click(object sender, RoutedEventArgs e) => _ = app.Capture(CaptureMode.RepeatRegion);
    private void History_Click(object sender, RoutedEventArgs e) => app.ShowHistory();
    private void Recovery_Click(object sender, RoutedEventArgs e) => app.ShowRecovery();
    private void Profiles_Click(object sender, RoutedEventArgs e) => app.ShowProfiles();
    private void TeamFolders_Click(object sender, RoutedEventArgs e) => app.ShowTeamFolders();
    private void Plugins_Click(object sender, RoutedEventArgs e) => app.ShowPlugins();
    private void Install_Click(object sender, RoutedEventArgs e) => app.ShowInstaller();
    private void PickWindow_Click(object sender, RoutedEventArgs e) => _ = app.Capture(CaptureMode.WindowPick);
    private void Open_Click(object sender, RoutedEventArgs e) => app.OpenImage();
    private void Paste_Click(object sender, RoutedEventArgs e) => app.PasteImage();
    private void Settings_Click(object sender, RoutedEventArgs e) => app.ShowSettings();
}
