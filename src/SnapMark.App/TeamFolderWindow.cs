using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SnapMark.Core;

namespace SnapMark.App;

public sealed class TeamFolderWindow : Window
{
    public TeamFolderWindow(AppSettings settings, Action persist, Action<string, bool>? save = null)
    {
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Team folders"; Width = 620; Height = 680; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(20) }; Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var copying = 0; Closing += (_, e) => { if (copying > 0) { e.Cancel = true; Title = "SnapMark · Finishing team file copy…"; } };
        panel.Children.Add(new TextBlock { Text = "Google Drive / OneDrive · Team folders", FontSize = 20, Margin = new Thickness(4) });
        panel.Children.Add(new TextBlock { Text = "เลือกโฟลเดอร์ทีมที่ sync อยู่ในเครื่อง เซฟแล้ว Drive/OneDrive จะ sync ตามสถานะของแอปนั้น เปิดหน้าเว็บเพื่อจัดสิทธิ์แชร์ · SnapMark ยังไม่ตรวจยืนยันว่า upload เสร็จ", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 8, 4, 12) });
        foreach (var google in new[] { true, false })
        {
            var title = google ? "Google Drive" : "OneDrive";
            var group = new StackPanel { Margin = new Thickness(10) }; panel.Children.Add(new Border { Child = group, Background = (Brush)FindResource("PanelBrush"), Margin = new Thickness(0, 4, 0, 4) });
            group.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4) });
            FieldHelp.Label(group, "โฟลเดอร์ทีมที่ sync ในเครื่อง / Local folder path");
            var row = new DockPanel(); var browse = new Button { Content = "Browse…" }; DockPanel.SetDock(browse, Dock.Right); row.Children.Add(browse);
            var folder = new TextBox { Text = google ? settings.GoogleDriveFolder : settings.OneDriveFolder, ToolTip = "Existing synced team folder" }; row.Children.Add(folder); group.Children.Add(row);
            FieldHelp.Hint(group, folder, title + " synced folder", google ? @"กด Browse เลือกโฟลเดอร์ที่ Google Drive sync เช่น H:\Shared drives\Project · ไม่ใช่ลิงก์เว็บ" : @"กด Browse เลือกโฟลเดอร์ที่ OneDrive sync เช่น C:\Users\YourName\OneDrive\Project · ไม่ใช่ลิงก์เว็บ");
            browse.Click += (_, _) => { var picker = new Microsoft.Win32.OpenFolderDialog { Title = "Choose " + title + " synced team folder" }; if (picker.ShowDialog(this) == true) folder.Text = picker.FolderName; };
            group.Children.Add(new TextBlock { Text = "Shared folder web link (optional)", Margin = new Thickness(4) });
            var link = new TextBox { Text = google ? settings.GoogleDriveShareUrl : settings.OneDriveShareUrl, ToolTip = "Paste the provider's shared-folder URL" }; group.Children.Add(link);
            FieldHelp.Hint(group, link, title + " shared-folder URL (optional)", google ? "วางลิงก์แชร์โฟลเดอร์ เช่น https://drive.google.com/drive/folders/... · เว้นว่างได้ ไม่ใช่ Path ในเครื่อง" : "วางลิงก์แชร์จาก OneDrive เช่น https://1drv.ms/... · เว้นว่างได้ ไม่ใช่ Path ในเครื่อง");
            var buttons = new WrapPanel(); group.Children.Add(buttons);
            var connect = new Button { Content = "Save connection" }; var open = new Button { Content = "Open sharing page" }; var copy = new Button { Content = "Copy folder link" }; var png = new Button { Content = "Save PNG to team", IsEnabled = save is not null }; var project = new Button { Content = "Save editable copy", IsEnabled = save is not null, ToolTip = "Includes original image and editable objects" };
            foreach (var b in new[] { connect, open, copy, png, project }) buttons.Children.Add(b);
            var existing = new Button { Content = "Copy file / MP4 to team…" }; buttons.Children.Add(existing);
            var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) }; group.Children.Add(status);
            void Store()
            {
                var validLink = TeamFolder.ValidateLink(link.Text, google);
                var validFolder = string.IsNullOrWhiteSpace(folder.Text) ? "" : StoragePaths.Validate(folder.Text);
                if (validFolder.Length > 0 && !System.IO.Directory.Exists(validFolder)) throw new System.IO.DirectoryNotFoundException("The folder is unavailable. Check your sync client or Browse again.");
                var oldFolder = google ? settings.GoogleDriveFolder : settings.OneDriveFolder; var oldLink = google ? settings.GoogleDriveShareUrl : settings.OneDriveShareUrl;
                try { if (google) { settings.GoogleDriveFolder = validFolder; settings.GoogleDriveShareUrl = validLink; } else { settings.OneDriveFolder = validFolder; settings.OneDriveShareUrl = validLink; } persist(); }
                catch { if (google) { settings.GoogleDriveFolder = oldFolder; settings.GoogleDriveShareUrl = oldLink; } else { settings.OneDriveFolder = oldFolder; settings.OneDriveShareUrl = oldLink; } throw; }
            }
            void Run(Action action) { try { action(); } catch (Exception ex) { status.Text = ex.Message; } }
            connect.Click += (_, _) => Run(() => { Store(); status.Text = "Saved. Folder sync is managed by " + title + "."; });
            open.Click += (_, _) => Run(() => { var url = TeamFolder.ValidateLink(link.Text, google); Process.Start(new ProcessStartInfo(url.Length > 0 ? url : google ? "https://drive.google.com/drive/u/0/my-drive" : "https://onedrive.live.com/") { UseShellExecute = true }); status.Text = "Opened provider page. Manage access and copy file links there."; });
            copy.Click += (_, _) => Run(() => { var url = TeamFolder.ValidateLink(link.Text, google); if (url.Length == 0) throw new ArgumentException("Paste the shared-folder link first."); Clipboard.SetText(url); status.Text = "Copied shared-folder link."; });
            void Save(bool editable) => Run(() => { Store(); var path = TeamFolder.NewFile(folder.Text, editable); save!(path, editable); status.Text = "Saved local copy: " + System.IO.Path.GetFileName(path) + " · Check sync status in " + title + "."; var explorer = new ProcessStartInfo("explorer.exe"); explorer.ArgumentList.Add("/select,"); explorer.ArgumentList.Add(path); Process.Start(explorer); });
            png.Click += (_, _) => Save(false); project.Click += (_, _) => Save(true);
            existing.Click += async (_, _) =>
            {
                try
                {
                    var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Images, video and projects|*.png;*.jpg;*.jpeg;*.mp4;*.pdf;*.snapmark" }; if (dialog.ShowDialog(this) != true) return;
                    Store(); existing.IsEnabled = false; copying++; status.Text = "Copying file to team folder…";
                    var destination = await TeamFolder.CopyFileAsync(dialog.FileName, folder.Text);
                    status.Text = "Copied local file: " + System.IO.Path.GetFileName(destination) + " · Check sync status in " + title + ".";
                }
                catch (Exception ex) { status.Text = ex.Message; }
                finally { if (!existing.IsEnabled) copying--; existing.IsEnabled = true; Title = "SnapMark · Team folders"; }
            };
        }
    }
}
