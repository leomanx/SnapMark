using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Export;

namespace SnapMark.App;

public sealed class HistoryPickerWindow : Window
{
    public BitmapSource? SelectedImage { get; private set; }
    public HistoryPickerWindow(IReadOnlyList<HistoryEntry> entries, IClipboardService clipboard, Action<BitmapSource>? insert = null, Action<BitmapSource>? open = null, bool picker = false, string? sourceFolder = null)
    {
        Style = (Style)FindResource(typeof(Window));
        Title = "SnapMark · Capture history"; Width = 740; Height = 650; MinHeight = 540; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new DockPanel { Margin = new Thickness(16) }; Content = root;
        var search = new TextBox { Margin = new Thickness(4) };
        var searchGroup = new StackPanel(); DockPanel.SetDock(searchGroup, Dock.Top); root.Children.Add(searchGroup);
        FieldHelp.Add(searchGroup, search, "ค้นหาภาพย้อนหลัง / Search history", "พิมพ์คำค้น ไม่ใช่ Path เช่น Revit, ชื่อหน้าต่าง หรือ 2026-10-11 · เว้นว่างเพื่อดูทั้งหมด");
        var indexBar = new WrapPanel(); DockPanel.SetDock(indexBar, Dock.Top); root.Children.Add(indexBar);
        var language = new ComboBox { Width = 175, ItemsSource = new[] { new OcrLanguage("auto", "Auto · installed OCR") }.Concat(LocalOcr.Languages()), DisplayMemberPath = "Name", SelectedIndex = 0 };
        var index = new Button { Content = "Index image text" }; var cancel = new Button { Content = "Cancel indexing", IsEnabled = false };
        indexBar.Children.Add(language); indexBar.Children.Add(index); indexBar.Children.Add(cancel);
        language.ToolTip = "ภาษาที่ใช้อ่านข้อความจากภาพ (OCR)"; index.ToolTip = "อ่านข้อความในภาพเพื่อให้ค้นหาได้ · ทำครั้งแรกก่อนค้นหาคำที่อยู่ในภาพ";
        var recognized = new Dictionary<string, string>(); CancellationTokenSource? indexing = null;
        if (sourceFolder is not null) { var source = new TextBox { Text = sourceFolder, IsReadOnly = true, TextWrapping = TextWrapping.Wrap }; var folderGroup = new StackPanel(); DockPanel.SetDock(folderGroup, Dock.Top); root.Children.Add(folderGroup); FieldHelp.Add(folderGroup, source, "โฟลเดอร์ที่เก็บ History / แสดงอย่างเดียว", "นี่คือที่เก็บภาพต้นฉบับ ไม่ต้องกรอก · เปลี่ยนได้ที่ Settings → STORAGE → Capture history"); }
        var bar = new WrapPanel(); DockPanel.SetDock(bar, Dock.Bottom); root.Children.Add(bar);
        var status = new TextBlock { Margin = new Thickness(4), TextWrapping = TextWrapping.Wrap }; DockPanel.SetDock(status, Dock.Bottom); root.Children.Add(status);
        var list = new ListBox { Background = Brushes.Transparent, Foreground = (System.Windows.Media.Brush)FindResource("TextBrush") }; root.Children.Add(list);
        foreach (var entry in entries)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4) };
            try
            {
                using var stream = System.IO.File.OpenRead(entry.Path);
                var thumbnail = new BitmapImage(); thumbnail.BeginInit(); thumbnail.CacheOption = BitmapCacheOption.OnLoad; thumbnail.DecodePixelWidth = 240; thumbnail.StreamSource = stream; thumbnail.EndInit(); thumbnail.Freeze();
                panel.Children.Add(new Image { Source = thumbnail, Width = 120, Height = 75, Margin = new Thickness(0, 0, 16, 0) });
            }
            catch { panel.Children.Add(new TextBlock { Text = "Image unavailable", Width = 136, VerticalAlignment = VerticalAlignment.Center }); }
            panel.Children.Add(new TextBlock { Text = $"{entry.Name}\n{entry.Timestamp.LocalDateTime:g} · {entry.SourceApp}\n{entry.Width} × {entry.Height}\n{entry.WindowTitle}", VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 460 });
            list.Items.Add(new ListBoxItem { Content = panel, Tag = entry.Path, ToolTip = entry.WindowTitle });
        }
        var byPath = entries.ToDictionary(e => e.Path);
        void Filter() { foreach (ListBoxItem item in list.Items) { var entry = byPath[(string)item.Tag]; item.Visibility = ($"{entry.Name} {entry.SourceApp} {entry.WindowTitle} {entry.Timestamp.LocalDateTime:yyyy-MM-dd} {System.IO.Path.GetFileName(entry.Path)} {recognized.GetValueOrDefault(entry.Path)}").Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed; } if (list.SelectedItem is ListBoxItem selected && selected.Visibility != Visibility.Visible) list.SelectedItem = null; }
        void LoadCache() { recognized.Clear(); foreach (var entry in entries) recognized[entry.Path] = HistoryTextIndex.Cached(entry.Path, ((OcrLanguage)language.SelectedItem).Tag); Filter(); }
        search.TextChanged += (_, _) => Filter(); language.SelectionChanged += (_, _) => LoadCache(); LoadCache();
        cancel.Click += (_, _) => indexing?.Cancel(); Closed += (_, _) => indexing?.Cancel();
        index.Click += async (_, _) =>
        {
            using var token = new CancellationTokenSource(); indexing = token; index.IsEnabled = language.IsEnabled = false; cancel.IsEnabled = true;
            var count = 0; var failures = 0;
            try
            {
                foreach (var entry in entries)
                {
                    token.Token.ThrowIfCancellationRequested(); status.Text = $"Reading image text {count + 1}/{entries.Count}…";
                    try { recognized[entry.Path] = await HistoryTextIndex.RecognizeAsync(entry.Path, ((OcrLanguage)language.SelectedItem).Tag, token.Token); }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex) { failures++; status.ToolTip = ex.Message; }
                    count++; Filter();
                }
                status.Text = $"Indexed {count - failures}/{count} images locally. {failures} unavailable. Search now includes recognized text.";
            }
            catch (OperationCanceledException) { status.Text = $"Indexing cancelled after {count} images. Completed text is retained."; }
            finally { indexing = null; index.IsEnabled = language.IsEnabled = true; cancel.IsEnabled = false; }
        };
        BitmapSource? Read() => list.SelectedItem is ListBoxItem item ? HistoryService.LoadImage((string)item.Tag) : null;
        var copy = new Button { Content = "Copy selected image" }; bar.Children.Add(copy);
        copy.Click += async (_, _) => { try { if (Read() is { } image) { await clipboard.CopyAsync(image); status.Text = "Copied. Ctrl+V in an editor inserts it as an image layer."; } } catch (Exception ex) { status.Text = ex.Message; } };
        var add = new Button { Content = "Insert into editor", IsEnabled = picker || insert is not null }; bar.Children.Add(add);
        void Insert() { try { if (Read() is { } image) { SelectedImage = image; if (picker) DialogResult = true; else insert?.Invoke(image); } } catch (Exception ex) { status.Text = ex.Message; } }
        add.Click += (_, _) => Insert();
        if (open is not null)
        {
            var edit = new Button { Content = "Open separately" }; bar.Children.Add(edit);
            edit.Click += (_, _) => { try { if (Read() is { } image) open(image); } catch (Exception ex) { status.Text = ex.Message; } };
        }
        list.MouseDoubleClick += (_, _) => Insert();
        if (entries.Count == 0) status.Text = "No captures yet. History must be enabled before capturing.";
    }
}
