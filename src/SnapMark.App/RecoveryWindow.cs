using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SnapMark.Editor;

namespace SnapMark.App;
public sealed class RecoveryWindow : Window
{
    public RecoveryWindow(RecoveryStore store, Action<RecoveryEntry> restore, Func<Guid, bool>? isOpen = null)
    {
        Style = (Style)FindResource(typeof(Window)); Title = "SnapMark · Recover editable work"; Width = 670; Height = 450; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new DockPanel { Margin = new Thickness(18) }; Content = root;
        var heading = new TextBlock { Text = "Local editable recovery copies", FontSize = 20, Margin = new Thickness(4, 0, 4, 12) }; DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        var status = new TextBlock { Text = "Restore opens editable work; its autosave updates this recovery. Includes original images and markup.", TextWrapping = TextWrapping.Wrap, Foreground = (System.Windows.Media.Brush)FindResource("SecondaryBrush"), Margin = new Thickness(4) }; DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
        var bar = new WrapPanel(); DockPanel.SetDock(bar, Dock.Bottom); root.Children.Add(bar);
        var list = new ListBox { Background = Brushes.Transparent, Foreground = (System.Windows.Media.Brush)FindResource("TextBrush") }; root.Children.Add(list);
        void Refresh() { list.Items.Clear(); foreach (var e in store.Entries()) list.Items.Add(new ListBoxItem { Content = $"{e.UpdatedUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss} · {e.Bytes / 1024d:0} KB · {e.Id.ToString("N")[..8]}", Tag = e }); }
        var open = new Button { Content = "Restore selected" }; bar.Children.Add(open); open.Click += (_, _) => { if (list.SelectedItem is ListBoxItem { Tag: RecoveryEntry entry }) { try { restore(entry); status.Text = "Restored. Save editable project when ready."; } catch (Exception ex) { status.Text = "Could not restore: " + ex.Message; } } };
        var remove = new Button { Content = "Discard selected recovery" }; bar.Children.Add(remove);
        remove.Click += (_, _) => { if (list.SelectedItem is not ListBoxItem { Tag: RecoveryEntry entry }) return; if (isOpen?.Invoke(entry.Id) == true) { status.Text = "This recovery is open in an editor. Close the editor before discarding it."; return; } if (MessageBox.Show(this, "Permanently discard this recovery copy?", "SnapMark", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { try { store.Remove(entry.Id); Refresh(); } catch (Exception ex) { status.Text = ex.Message; } } };
        Refresh(); if (list.Items.Count == 0) status.Text = "No recovery copies. Edited documents are autosaved when enabled in Settings.";
    }
}
