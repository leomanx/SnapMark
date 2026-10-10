using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace SnapMark.Capture;

// One frozen overlay per monitor: layout DIPs never become desktop pixel coordinates.
public sealed class RegionOverlay : Window
{
    private sealed class Session
    {
        public readonly TaskCompletionSource<RegionSelection?> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly List<RegionOverlay> Windows = [];
        public PixelPoint? Start;
        public PixelRect? Selection;
        public PixelPoint Cursor = NativeDesktop.Cursor;
        public PixelRect Desktop = NativeDesktop.VirtualBounds;
        public IReadOnlyList<PixelRect> Candidates = NativeDesktop.VisibleWindowBounds();
        public bool WindowMode;
        public bool KeyboardSelection;
        public bool Freeform, AllowFreeform;
        public bool ConfirmRegion, Editing;
        public RegionHandle Handle;
        public PixelRect Original;
        public readonly List<PixelPoint> Points = [];
        public void Finish(PixelRect? rect)
        {
            if (!Completion.TrySetResult(rect is { } bounds ? new(bounds, Freeform ? Points.ToArray() : null) : null)) return;
            foreach (var w in Windows) w.Close();
        }
        public void Redraw() { foreach (var w in Windows) { w.view.InvalidateVisual(); w.confirm.Visibility = ConfirmRegion && Selection is { Width: >= 2, Height: >= 2 } ? Visibility.Visible : Visibility.Collapsed; } }
    }
    private readonly Session session;
    private readonly PixelRect monitor;
    private readonly OverlayView view;
    private readonly System.Windows.Controls.Button confirm;
    private RegionOverlay(Session session, BitmapSource image, PixelRect monitor)
    {
        this.session = session; this.monitor = monitor;
        Title = session.ConfirmRegion ? "SnapMark · Adjust recording area" : "SnapMark · Select region";
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; Topmost = true;
        ShowInTaskbar = true; Background = Brushes.Black; Cursor = Cursors.Cross;
        Left = monitor.X; Top = monitor.Y; Width = monitor.Width; Height = monitor.Height;
        view = new OverlayView(image, session, monitor);
        var layout = new System.Windows.Controls.Grid(); layout.Children.Add(view); Content = layout;
        confirm = new System.Windows.Controls.Button { Content = "Use this area → Next / ใช้กรอบนี้ · Enter", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(12), Padding = new Thickness(16, 10, 16, 10), Background = Brushes.Black, Foreground = Brushes.White, Visibility = Visibility.Collapsed };
        confirm.Click += (_, _) => { if (session.Selection is { Width: >= 2, Height: >= 2 } selected) session.Finish(selected); };
        layout.Children.Add(confirm);
        SourceInitialized += (_, _) => NativeDesktop.Position(this, monitor);
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { session.Finish(null); return; }
            if (e.Key == Key.F && session.AllowFreeform) { session.Freeform = !session.Freeform; session.WindowMode = false; session.KeyboardSelection = false; session.Start = null; session.Selection = null; session.Points.Clear(); session.Redraw(); e.Handled = true; return; }
            if (session.Freeform && e.Key == Key.Enter && session.Points.Count >= 3) { var points = session.Points; var rect = new PixelRect(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y)); if (rect.Width >= 2 && rect.Height >= 2) session.Finish(rect); return; }
            if (e.Key == Key.W) { session.Freeform = false; session.Points.Clear(); session.WindowMode = !session.WindowMode; session.Start = null; session.KeyboardSelection = false; session.Selection = null; session.Redraw(); e.Handled = true; return; }
            if (e.Key == Key.Enter && session.Selection is { Width: >= 2, Height: >= 2 } selected) { session.Finish(selected); return; }
            if (e.Key == Key.Space) { session.Freeform = false; session.Points.Clear(); session.WindowMode = false; session.KeyboardSelection = true; session.Selection = new PixelRect(Math.Min(session.Cursor.X, session.Desktop.Right - 2), Math.Min(session.Cursor.Y, session.Desktop.Bottom - 2), 200, 120).Intersect(session.Desktop); session.Redraw(); e.Handled = true; return; }
            if (session.KeyboardSelection && session.Selection is { } current && e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
            {
                var step = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) ? 10 : 1;
                session.Selection = SelectionGeometry.Adjust(current, session.Desktop, e.Key == Key.Left ? -step : e.Key == Key.Right ? step : 0, e.Key == Key.Up ? -step : e.Key == Key.Down ? step : 0, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)); session.Redraw(); e.Handled = true;
            }
        };
        MouseLeftButtonDown += (_, e) => {
            if (e.OriginalSource is System.Windows.Controls.Button) return;
            if (session.Freeform) { if (session.Points.Count < 1000) session.Points.Add(NativeDesktop.Cursor); session.Redraw(); e.Handled = true; return; }
            if (session.WindowMode && session.Selection is { } window) { if (!session.ConfirmRegion) { session.Finish(window); return; } session.WindowMode = false; session.Editing = true; }
            session.Handle = session.ConfirmRegion && session.Editing && session.Selection is { } existing ? RecordingRegionGeometry.Hit(existing, NativeDesktop.Cursor, 10 * VisualTreeHelper.GetDpi(this).DpiScaleX) : RegionHandle.None;
            if (session.Selection is { } current) session.Original = current;
            session.KeyboardSelection = session.ConfirmRegion; session.Start = NativeDesktop.Cursor; CaptureMouse(); e.Handled = true;
        };
        MouseMove += (_, _) =>
        {
            var end = NativeDesktop.Cursor;
            session.Cursor = end;
            if (session.Freeform) { session.Redraw(); return; }
            if (session.WindowMode) { session.Selection = session.Candidates.FirstOrDefault(b => b.Contains(end)); session.Redraw(); return; }
            if (session.Start is not { } start) { session.Redraw(); return; }
            if (session.ConfirmRegion && session.Handle != RegionHandle.None) { session.Selection = RecordingRegionGeometry.Drag(session.Original, session.Handle, start, end, session.Desktop); session.Redraw(); return; }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                var xs = session.Candidates.SelectMany(b => new[] { b.X, b.Right }).Where(x => Math.Abs(x - end.X) <= 10).ToArray();
                var ys = session.Candidates.SelectMany(b => new[] { b.Y, b.Bottom }).Where(y => Math.Abs(y - end.Y) <= 10).ToArray();
                end = new(xs.Length > 0 ? xs.MinBy(x => Math.Abs(x - end.X)) : end.X, ys.Length > 0 ? ys.MinBy(y => Math.Abs(y - end.Y)) : end.Y);
            }
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                var size = Math.Min(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
                end = new(start.X + Math.Sign(end.X - start.X) * size, start.Y + Math.Sign(end.Y - start.Y) * size);
            }
            session.Selection = PixelRect.Between(start, end); session.Redraw();
        };
        MouseLeftButtonUp += (_, _) =>
        {
            if (session.Freeform) return;
            ReleaseMouseCapture();
            var r = session.Selection;
            if (r is { Width: >= 2, Height: >= 2 }) { if (session.ConfirmRegion) { session.Editing = true; session.Start = null; session.Handle = RegionHandle.None; session.Redraw(); } else session.Finish(r); }
            else { session.Start = null; session.Selection = null; session.Redraw(); }
        };
    }
    public static Task<RegionSelection?> SelectAsync(BitmapSource desktop, PixelRect desktopBounds, bool windowMode = false, bool allowFreeform = true, bool confirmRegion = false, PixelRect? initialBounds = null)
    {
        var session = new Session();
        session.WindowMode = windowMode;
        session.AllowFreeform = allowFreeform;
        session.ConfirmRegion = confirmRegion; session.Selection = initialBounds; session.Editing = initialBounds is not null; session.KeyboardSelection = confirmRegion;
        if (windowMode) session.Selection = session.Candidates.FirstOrDefault(b => b.Contains(session.Cursor));
        try
        {
            foreach (var monitor in NativeDesktop.Monitors)
            {
                var image = NativeDesktop.Crop(desktop, new(monitor.X - desktopBounds.X, monitor.Y - desktopBounds.Y, monitor.Width, monitor.Height));
                session.Windows.Add(new RegionOverlay(session, image, monitor));
            }
            foreach (var window in session.Windows) window.Show();
            session.Redraw();
            session.Windows.FirstOrDefault(w => w.monitor.Contains(NativeDesktop.Cursor))?.Activate();
        }
        catch { session.Finish(null); throw; }
        return session.Completion.Task;
    }
    private sealed class OverlayView(BitmapSource image, Session session, PixelRect monitor) : FrameworkElement
    {
        private readonly BitmapSource pixelImage = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        protected override void OnRender(DrawingContext dc)
        {
            var full = new Rect(0, 0, ActualWidth, ActualHeight);
            dc.DrawImage(image, full);
            var dim = new SolidColorBrush(Color.FromArgb(145, 0, 0, 0));
            if (session.Selection is { } r)
            {
                var sx = ActualWidth / monitor.Width; var sy = ActualHeight / monitor.Height;
                var selection = new Rect((r.X - monitor.X) * sx, (r.Y - monitor.Y) * sy, r.Width * sx, r.Height * sy);
                var mask = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(full), new RectangleGeometry(selection));
                dc.DrawGeometry(dim, null, mask);
                dc.DrawRectangle(null, new Pen(Brushes.DeepSkyBlue, 2), selection);
                if (session.ConfirmRegion) foreach (var handle in RecordingRegionGeometry.Handles(r)) { var p = new Point((handle.Point.X - monitor.X) * sx, (handle.Point.Y - monitor.Y) * sy); dc.DrawRectangle(Brushes.White, new Pen(Brushes.DeepSkyBlue, 2), new Rect(p.X - 5, p.Y - 5, 10, 10)); }
                var text = new FormattedText($"{r.Width:0} × {r.Height:0} px" + (session.ConfirmRegion ? " · Drag handles / move frame · Enter next · Esc cancel" : ""), System.Globalization.CultureInfo.InvariantCulture,
                    System.Windows.FlowDirection.LeftToRight, new Typeface("Segoe UI"), 16, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                var x = Math.Clamp(selection.X, 12, Math.Max(12, ActualWidth - text.Width - 28));
                var y = Math.Clamp(selection.Bottom + 12, 12, Math.Max(12, ActualHeight - 45));
                dc.DrawRoundedRectangle(Brushes.Black, null, new Rect(x - 8, y - 5, text.Width + 16, 32), 5, 5);
                dc.DrawText(text, new Point(x, y));
            }
            else
            {
                dc.DrawRectangle(dim, null, full);
                var text = new FormattedText(session.Freeform ? "Freeform: click vertices · Enter finishes · F rectangle · Esc cancel" : "Drag · Shift square · Ctrl snap · W window · Space keyboard frame" + (session.AllowFreeform ? " · F freeform" : "") + " · Esc cancel", System.Globalization.CultureInfo.InvariantCulture,
                    System.Windows.FlowDirection.LeftToRight, new Typeface("Segoe UI"), 18, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                dc.DrawText(text, new Point(24, 24));
            }
            if (session.Freeform && session.Points.Count > 0)
            {
                Point Local(PixelPoint p) => new((p.X - monitor.X) * ActualWidth / monitor.Width, (p.Y - monitor.Y) * ActualHeight / monitor.Height);
                var pen = new Pen(Brushes.DeepSkyBlue, 2);
                for (var i = 1; i < session.Points.Count; i++) dc.DrawLine(pen, Local(session.Points[i - 1]), Local(session.Points[i]));
                dc.DrawLine(pen, Local(session.Points[^1]), Local(session.Cursor));
                foreach (var point in session.Points) dc.DrawEllipse(Brushes.DeepSkyBlue, null, Local(point), 3, 3);
            }
            var localX = (session.Cursor.X - monitor.X) * ActualWidth / monitor.Width;
            var localY = (session.Cursor.Y - monitor.Y) * ActualHeight / monitor.Height;
            if (!monitor.Contains(session.Cursor)) return;
            var px = Math.Clamp((int)(session.Cursor.X - monitor.X), 0, image.PixelWidth - 1);
            var py = Math.Clamp((int)(session.Cursor.Y - monitor.Y), 0, image.PixelHeight - 1);
            var size = 11; var x0 = Math.Clamp(px - size / 2, 0, Math.Max(0, image.PixelWidth - size)); var y0 = Math.Clamp(py - size / 2, 0, Math.Max(0, image.PixelHeight - size));
            var crop = new CroppedBitmap(image, new Int32Rect(x0, y0, Math.Min(size, image.PixelWidth), Math.Min(size, image.PixelHeight)));
            var mx = Math.Clamp(localX > 220 ? localX - 216 : localX + 24, 0, Math.Max(0, ActualWidth - 196)); var my = Math.Clamp(localY + 24, 0, Math.Max(0, ActualHeight - 176));
            dc.DrawRectangle(Brushes.Black, new Pen(Brushes.White, 1), new Rect(mx, my, 192, 172));
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            dc.DrawImage(crop, new Rect(mx + 4, my + 4, 110, 110));
            var crossX = mx + 4 + (px - x0 + .5) * 110 / crop.PixelWidth; var crossY = my + 4 + (py - y0 + .5) * 110 / crop.PixelHeight;
            dc.DrawLine(new Pen(Brushes.DeepSkyBlue, 1), new Point(crossX, my + 4), new Point(crossX, my + 114));
            dc.DrawLine(new Pen(Brushes.DeepSkyBlue, 1), new Point(mx + 4, crossY), new Point(mx + 114, crossY));
            var pixel = new byte[4]; pixelImage.CopyPixels(new Int32Rect(px, py, 1, 1), pixel, 4, 0);
            var caption = new FormattedText($"X {session.Cursor.X:0} · Y {session.Cursor.Y:0}\n#{pixel[2]:X2}{pixel[1]:X2}{pixel[0]:X2}\nArrows move · Shift resize", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(caption, new Point(mx + 4, my + 118));
        }
    }
}
