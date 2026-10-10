using System.IO;
using System.Windows;
using System.Windows.Controls;
using SnapMark.Export;
namespace SnapMark.App;
public sealed class InstallWindow : Window
{
    public InstallWindow()
    {
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Local install / update"; Width = 610; Height = 460; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(20) }; Content = panel;
        panel.Children.Add(new TextBlock { Text = "Install / update from a local package", FontSize = 20 });
        panel.Children.Add(new TextBlock { Text = "Checks SHA-256, installs a new version folder and preserves previous versions. Installed-mode data lives in AppData; portable history is not copied. Close the old app before opening the installed version.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 8, 4, 8) });
        var source = new TextBox { Text = AppContext.BaseDirectory }; FieldHelp.Add(panel, source, "โฟลเดอร์แพ็กเกจ SnapMark ที่แตก ZIP แล้ว", @"เลือกโฟลเดอร์ที่มี SnapMark.exe และ release-manifest.json เช่น D:\Downloads\SnapMark-0.6.12-portable · ไม่ใช่ไฟล์ ZIP หรือโฟลเดอร์ปลายทางติดตั้ง");
        var browse = new Button { Content = "Choose package folder…" }; panel.Children.Add(browse); browse.Click += (_, _) => { var dialog = new Microsoft.Win32.OpenFolderDialog(); if (dialog.ShowDialog(this) == true) source.Text = dialog.FolderName; };
        var install = new Button { Content = "Verify and install for this Windows user" }; panel.Children.Add(install);
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) }; panel.Children.Add(status);
        var busy = false; Closing += (_, e) => { if (busy) e.Cancel = true; };
        install.Click += async (_, _) => { busy = true; install.IsEnabled = browse.IsEnabled = source.IsEnabled = false; status.Text = "Verifying and installing…";
            try { var path = source.Text; var installed = await Task.Run(() => PackageInstaller.Install(path, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SnapMark"))); LocalIntegration.CreateShortcut(installed); status.Text = "Installed: " + installed + "\nStart Menu shortcut updated."; }
            catch (Exception ex) { status.Text = ex.Message; } finally { busy = false; install.IsEnabled = browse.IsEnabled = source.IsEnabled = true; }
        };
    }
}
