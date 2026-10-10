using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
namespace SnapMark.App;
public sealed record PaletteCommand(string Name, Action Execute);
public sealed class CommandPalette : Window
{
    public CommandPalette(Window owner, IEnumerable<PaletteCommand> commands)
    {
        Owner = owner; Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Find tool / command"; Width = 560; Height = 480; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var items = commands.ToArray(); var panel = new DockPanel { Margin = new Thickness(16) }; Content = panel;
        var search = new TextBox(); var searchGroup = new StackPanel(); DockPanel.SetDock(searchGroup, Dock.Top); panel.Children.Add(searchGroup); FieldHelp.Add(searchGroup, search, "ค้นหาเครื่องมือ / Tool or command", "พิมพ์ชื่อ เช่น Blur, Select, Copy · Enter เรียกคำสั่ง · Esc ปิด · ไม่ใช่ช่อง Path");
        var list = new ListBox { DisplayMemberPath = "Name", ItemsSource = items, SelectedIndex = 0 }; panel.Children.Add(list);
        search.TextChanged += (_, _) => { list.ItemsSource = items.Where(c => c.Name.Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray(); list.SelectedIndex = 0; };
        void Execute() { if (list.SelectedItem is not PaletteCommand command) return; Close(); command.Execute(); }
        list.MouseDoubleClick += (_, _) => Execute();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { Close(); e.Handled = true; } else if (e.Key == Key.Enter) { Execute(); e.Handled = true; } else if (e.Key is Key.Down or Key.Up) { list.SelectedIndex = Math.Clamp(list.SelectedIndex + (e.Key == Key.Down ? 1 : -1), 0, Math.Max(0, list.Items.Count - 1)); if (list.SelectedItem is not null) list.ScrollIntoView(list.SelectedItem); e.Handled = true; } };
        Loaded += (_, _) => search.Focus();
    }
}
