using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Export;

namespace SnapMark.App;

/// <summary>Local history previews share the editor viewport without replacing its editable document.</summary>
public sealed class EditorHistoryPanel : DockPanel, IDisposable
{
    private readonly Func<IReadOnlyList<HistoryEntry>> entries;
    private readonly Action<BitmapSource> insert;
    private readonly Func<HistoryEntry, Task>? recycle;
    private readonly Action<HistoryEntry, string>? rename;
    private readonly List<(Grid Tile, Image Image, double Ratio)> tileViews = [];
    private readonly DockPanel heading;
    private bool recycling;
    public HistoryEntry? SelectedEntry { get; private set; }
    private readonly StackPanel thumbnails = new() { Orientation = Orientation.Horizontal };
    private readonly TextBlock caption = new() { Margin = new Thickness(8), TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock summary = new() { Margin = new Thickness(8, 4, 8, 4) };
    private readonly Image previewImage = new() { Stretch = Stretch.Uniform, Margin = new Thickness(12) };
    private readonly Button insertButton = new() { Content = "Insert into current image / แทรกลงงาน" };
    private bool disposed;
    private int refreshVersion, previewVersion;
    public Border Preview { get; }
    public BitmapSource? SelectedImage { get; private set; }
    public bool IsPreviewing => Preview.Visibility == Visibility.Visible;
    public event Action<bool>? PreviewChanged;

    public EditorHistoryPanel(Func<IReadOnlyList<HistoryEntry>> entries, Action<BitmapSource> insert, Func<HistoryEntry, Task>? recycle = null, Action<HistoryEntry, string>? rename = null)
    {
        this.entries = entries; this.insert = insert; this.recycle = recycle; this.rename = rename; Height = 160; MinHeight = 76;
        SetResourceReference(BackgroundProperty, "PanelBrush");
        var divider = new Thumb { Height = 8, Cursor = Cursors.SizeNS, ToolTip = "Drag to resize History / ลากปรับความสูง", HorizontalAlignment = HorizontalAlignment.Stretch };
        divider.SetResourceReference(BackgroundProperty, "AccentBrush"); DockPanel.SetDock(divider, Dock.Top); Children.Add(divider);
        divider.DragDelta += (_, e) => ResizeHistory(Height - e.VerticalChange);
        heading = new DockPanel(); DockPanel.SetDock(heading, Dock.Top); Children.Add(heading);
        var actions = new StackPanel { Orientation = Orientation.Horizontal }; DockPanel.SetDock(actions, Dock.Right); heading.Children.Add(actions);
        var back = new Button { Content = "Current edit / กลับมางาน · Esc" }; back.Click += (_, _) => Back(); actions.Children.Add(back);
        var refresh = new Button { Content = "Refresh" }; refresh.Click += async (_, _) => await RefreshAsync(); actions.Children.Add(refresh);
        var collapse = new Button { Content = "Hide thumbnails" };
        var expandedHeight = Height;
        collapse.Click += (_, _) => { var hidden = thumbnails.Visibility == Visibility.Visible; if (hidden) expandedHeight = Height; thumbnails.Visibility = hidden ? Visibility.Collapsed : Visibility.Visible; Height = hidden ? MinHeight : expandedHeight; collapse.Content = hidden ? "Show thumbnails" : "Hide thumbnails"; };
        actions.Children.Add(collapse); heading.Children.Add(summary);
        Children.Add(new ScrollViewer { Content = thumbnails, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled });

        var preview = new DockPanel(); var controls = new StackPanel(); DockPanel.SetDock(controls, Dock.Top); preview.Children.Add(controls);
        controls.Children.Add(caption);
        var row = new WrapPanel(); controls.Children.Add(row);
        var returnButton = new Button { Content = "Back to current edit / กลับมางาน · Esc" }; returnButton.Click += (_, _) => Back(); row.Children.Add(returnButton);
        insertButton.IsEnabled = false; insertButton.Click += (_, _) => InsertSelected(); row.Children.Add(insertButton);
        preview.Children.Add(previewImage);
        Preview = new Border { Child = preview, Visibility = Visibility.Collapsed };
        Preview.SetResourceReference(BackgroundProperty, "CanvasBrush");
        SizeChanged += (_, _) => ScaleThumbnails();
    }

    public void ResizeHistory(double height)
    {
        var owner = Window.GetWindow(this);
        var maximum = Math.Max(MinHeight, (owner?.ActualHeight > 0 ? owner.ActualHeight : owner?.Height ?? 800) * .65);
        Height = Math.Clamp(height, MinHeight, maximum); ScaleThumbnails();
    }
    private void ScaleThumbnails()
    {
        var imageHeight = Math.Max(32, (ActualHeight > 0 ? ActualHeight : Height) - Math.Max(heading.ActualHeight, 40) - 56);
        foreach (var item in tileViews) { item.Image.Height = imageHeight; item.Tile.Width = Math.Clamp(imageHeight * item.Ratio + 12, 100, 640); }
    }
    public async Task RecycleAsync(HistoryEntry? entry = null)
    {
        entry ??= SelectedEntry;
        if (disposed || recycling || entry is null || recycle is null) return;
        recycling = true; IsEnabled = false;
        try
        {
            await recycle(entry);
            if (disposed) return;
            if (SelectedEntry?.Path == entry.Path) Back();
            await RefreshAsync(); summary.Text = "Moved to Recycle Bin / ส่งภาพไปถังขยะแล้ว";
        }
        catch (Exception ex) { if (!disposed) summary.Text = "Could not recycle history: " + ex.Message; }
        finally { recycling = false; if (!disposed) IsEnabled = true; }
    }

    public async Task RefreshAsync()
    {
        if (disposed) return;
        var request = ++refreshVersion;
        summary.Text = "History / ภาพย้อนหลัง · Loading…";
        try
        {
            // Decode off the UI thread; retain small thumbnails rather than full capture bitmaps.
            var items = await Task.Run(() => entries().Select(entry => (Entry: entry, Thumbnail: TryThumbnail(entry.Path))).ToArray());
            if (disposed || request != refreshVersion) return;
            thumbnails.Children.Clear(); tileViews.Clear();
            foreach (var item in items)
            {
                var content = new StackPanel(); var image = new Image { Source = item.Thumbnail, Height = 66, Stretch = Stretch.Uniform }; content.Children.Add(image);
                content.Children.Add(new TextBlock { Text = item.Entry.DisplayName, FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Stretch, TextAlignment = TextAlignment.Center });
                var button = new Button { Content = content, Padding = new Thickness(4), ToolTip = $"{item.Entry.DisplayName} · {item.Entry.Timestamp.LocalDateTime:g}\n{item.Entry.Width} × {item.Entry.Height} px · {item.Entry.SourceApp}\n{item.Entry.WindowTitle}\n{item.Entry.Path}\nคลิกขวา → Rename · Delete / × → Recycle Bin" };
                var menu = new ContextMenu(); var renameItem = new MenuItem { Header = "Rename / เปลี่ยนชื่อ…", IsEnabled = rename is not null }; menu.Items.Add(renameItem); button.ContextMenu = menu;
                renameItem.Click += (_, _) => RenamePrompt(item.Entry);
                button.Click += async (_, _) => await PreviewAsync(item.Entry);
                var tile = new Grid { Width = 120 }; tile.Children.Add(button);
                var remove = new Button { Content = "×", Width = 24, Height = 24, Padding = new Thickness(0), FontSize = 18, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, ToolTip = "Move this capture to Recycle Bin / ส่งภาพนี้ไปถังขยะ", IsEnabled = recycle is not null };
                remove.Click += async (_, e) => { e.Handled = true; await RecycleAsync(item.Entry); }; tile.Children.Add(remove);
                thumbnails.Children.Add(tile); tileViews.Add((tile, image, Math.Max(.1, (double)item.Entry.Width / Math.Max(1, item.Entry.Height))));
            }
            summary.Text = items.Length == 0 ? "History / ภาพย้อนหลัง · ยังไม่มีภาพที่บันทึก" : $"History / ภาพย้อนหลัง · {items.Length} images · คลิกเพื่อ preview · คลิกขวาเปลี่ยนชื่อ";
            ScaleThumbnails();
        }
        catch (Exception ex) { if (!disposed && request == refreshVersion) summary.Text = "History unavailable: " + ex.Message; }
    }

    private void RenamePrompt(HistoryEntry entry)
    {
        if (rename is null || disposed) return;
        var dialog = new Window { Title = "Rename history / เปลี่ยนชื่อภาพ", Width = 460, Height = 230, Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner, Style = (Style)FindResource(typeof(Window)), ResizeMode = ResizeMode.NoResize };
        var panel = new StackPanel { Margin = new Thickness(16) }; dialog.Content = panel;
        var input = new TextBox { Text = entry.Name, MaxLength = 160 };
        FieldHelp.Add(panel, input, "ชื่อภาพสำหรับค้นหา / Image name", "เช่น RFI ห้องประชุม ชั้น 3 · เว้นว่างเพื่อใช้วันที่เดิม · ชื่อนี้ไม่เปลี่ยน Path ของภาพ");
        var row = new WrapPanel(); panel.Children.Add(row); var save = new Button { Content = "Save / บันทึก", IsDefault = true }; var cancel = new Button { Content = "Cancel", IsCancel = true }; row.Children.Add(save); row.Children.Add(cancel);
        save.Click += (_, _) => { try { rename(entry, input.Text); dialog.DialogResult = true; } catch (Exception ex) { MessageBox.Show(dialog, ex.Message, "Rename failed"); } };
        dialog.Loaded += (_, _) => { input.Focus(); input.SelectAll(); };
        if (dialog.ShowDialog() == true) { if (SelectedEntry?.Path == entry.Path) { SelectedEntry = entry with { Name = input.Text.Trim() }; caption.Text = $"HISTORY PREVIEW · {SelectedEntry.DisplayName} · {entry.Timestamp.LocalDateTime:g}"; } _ = RefreshAsync(); }
    }

    public async Task PreviewAsync(HistoryEntry entry)
    {
        if (disposed || recycling) return;
        var request = ++previewVersion;
        SelectedEntry = entry;
        SelectedImage = null; previewImage.Source = null; insertButton.IsEnabled = false;
        caption.Text = "Loading history preview…";
        Preview.Visibility = Visibility.Visible; PreviewChanged?.Invoke(true);
        try
        {
            var image = await Task.Run(() =>
            {
                // A frozen decoder frame still owns thread-affine metadata. Detach its pixels
                // so clipboard/export/InsertImage may create an encoder frame on the UI thread.
                var detached = new WriteableBitmap(HistoryService.LoadImage(entry.Path)); detached.Freeze(); return detached;
            });
            if (disposed || request != previewVersion) return;
            SelectedImage = image; previewImage.Source = image; insertButton.IsEnabled = true;
            caption.Text = $"HISTORY PREVIEW · {entry.DisplayName} · {entry.Timestamp.ToLocalTime():dd MMM yyyy HH:mm:ss} · {image.PixelWidth} × {image.PixelHeight} px\nงานปัจจุบันยังเก็บไว้ · กลับมางานเพื่อแก้ไข หรือแทรกภาพนี้ลงงาน";
        }
        catch (Exception ex) { if (!disposed && request == previewVersion) caption.Text = "Could not preview this image: " + ex.Message + "\nกลับมางานเพื่อแก้ไขต่อได้"; }
    }

    public void InsertSelected()
    {
        if (SelectedImage is not { } image) return;
        Back(); insert(image);
    }
    public void Back()
    {
        previewVersion++; SelectedEntry = null; SelectedImage = null; previewImage.Source = null; insertButton.IsEnabled = false;
        Preview.Visibility = Visibility.Collapsed; PreviewChanged?.Invoke(false);
    }
    public static BitmapSource? TryThumbnail(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 480; image.StreamSource = stream; image.EndInit(); image.Freeze(); return image;
        }
        catch { return null; }
    }
    public void Dispose() { disposed = true; refreshVersion++; Back(); thumbnails.Children.Clear(); }
}
