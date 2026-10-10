using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SnapMark.Core;

namespace SnapMark.App;
public sealed class ProfilesWindow : Window
{
    public List<ProjectProfile> Profiles { get; }
    public string Active { get; private set; }
    public ProfilesWindow(AppSettings settings, IReadOnlyList<string>? styles = null)
    {
        Profiles = settings.Profiles.ToList(); Active = settings.ActiveProfile;
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Project profiles"; Width = 620; Height = 700; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(20) }; Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        panel.Children.Add(new TextBlock { Text = "PROJECT / APP PROFILES", FontSize = 20 });
        panel.Children.Add(new TextBlock { Text = "An app match takes priority over the active profile. Blank app list = manual project. Image exports use the profile folder; video/projects retain their own Settings folders.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 8, 4, 8) });
        var pick = new ComboBox { ItemsSource = Profiles.Select(p => p.Name).Prepend("Default settings").ToArray(), SelectedItem = Active.Length == 0 ? "Default settings" : Active }; panel.Children.Add(pick);
        TextBox Field(string label) { panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(4, 8, 4, 0) }); var box = new TextBox(); panel.Children.Add(box); return box; }
        var name = Field("Profile name"); var apps = Field("App process names · comma separated, e.g. Revit, acad"); var color = Field("Markup HEX color"); color.Text = "#EF4444";
        var pattern = Field("Filename · {date}, {time}, {kind}, {app}, {counter}"); pattern.Text = "{app}_{date}_{counter}";
        var folder = Field("Image export folder · blank uses Settings");
        FieldHelp.Hint(panel, folder, "Profile image export folder", @"โฟลเดอร์สำหรับบันทึกภาพของโปรเจกต์ เช่น D:\Projects\Project-A\Markup · ไม่ใช่ชื่อไฟล์ · เว้นว่างใช้โฟลเดอร์จาก Settings");
        panel.Children.Add(new TextBlock { Text = "Personal style / stamp preset · overrides markup color", Margin = new Thickness(4) });
        var style = new ComboBox { ItemsSource = (styles ?? []).Prepend("Default color only").ToArray(), SelectedIndex = 0 }; panel.Children.Add(style);
        var browse = new Button { Content = "Browse folder…" }; panel.Children.Add(browse); browse.Click += (_, _) => { var dialog = new Microsoft.Win32.OpenFolderDialog(); if (dialog.ShowDialog(this) == true) folder.Text = dialog.FolderName; };
        pick.SelectionChanged += (_, _) => { var profile = Profiles.FirstOrDefault(p => p.Name == pick.SelectedItem as string); if (profile is null) { name.Text = apps.Text = folder.Text = ""; style.SelectedIndex = 0; return; } name.Text = profile.Name; apps.Text = profile.Applications; color.Text = profile.Color; pattern.Text = profile.FilenamePattern; folder.Text = profile.ExportFolder; style.SelectedItem = profile.StylePresetName.Length > 0 ? profile.StylePresetName : "Default color only"; };
        var add = new Button { Content = "Add / update profile" }; panel.Children.Add(add);
        add.Click += (_, _) => { try { if (string.IsNullOrWhiteSpace(name.Text) || name.Text.Length > 60 || name.Text == "Default settings") throw new ArgumentException("Enter a profile name (up to 60 characters)."); if (Profiles.Count >= 50 && Profiles.All(p => p.Name != name.Text)) throw new ArgumentException("Maximum 50 profiles.");
            if (!SnapMark.Editor.ColorMath.TryHex(color.Text, out var hex)) throw new ArgumentException("Enter a HEX color."); OutputNaming.Create(pattern.Text, "image", DateTime.Now); if (!string.IsNullOrWhiteSpace(folder.Text)) folder.Text = StoragePaths.Validate(folder.Text);
            var savedName = name.Text.Trim(); var styleName = style.SelectedItem as string ?? ""; Profiles.RemoveAll(p => p.Name.Equals(savedName, StringComparison.OrdinalIgnoreCase)); Profiles.Add(new(savedName, apps.Text.Trim(), SnapMark.Editor.ColorMath.Hex(hex), pattern.Text.Trim(), folder.Text.Trim(), styleName == "Default color only" ? "" : styleName)); pick.ItemsSource = Profiles.Select(p => p.Name).Prepend("Default settings").ToArray(); pick.SelectedItem = savedName;
        } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Profile"); } };
        var save = new Button { Content = "Save profiles and activate selected" }; panel.Children.Add(save); save.Click += (_, _) => { Active = pick.SelectedItem as string is { } selected && selected != "Default settings" ? selected : ""; DialogResult = true; };
        pick.SelectedIndex = -1; pick.SelectedItem = Active.Length == 0 ? "Default settings" : Active;
    }
}
