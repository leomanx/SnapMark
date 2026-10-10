namespace SnapMark.Core;

// Domain coordinates are image pixels; screen bounds are physical desktop pixels.
public readonly record struct PixelPoint(double X, double Y);
public readonly record struct PixelRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public bool Contains(PixelPoint p) => p.X >= X && p.Y >= Y && p.X <= Right && p.Y <= Bottom;
    public static PixelRect Between(PixelPoint a, PixelPoint b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    public PixelRect Intersect(PixelRect r)
    {
        var x = Math.Max(X, r.X); var y = Math.Max(Y, r.Y);
        return new(x, y, Math.Max(0, Math.Min(Right, r.Right) - x), Math.Max(0, Math.Min(Bottom, r.Bottom) - y));
    }
}
public enum CaptureMode { Region, ActiveWindow, Monitor, AllScreens, RepeatRegion, WindowPick }
public enum AnnotationKind { Arrow, Rectangle, Text, Highlight, Number, Blur, Pixelate, Redact, Stamp,
    Line, RoundedRectangle, Ellipse, Circle, Triangle, Polygon, Freehand, Polyline, OpenDot, FilledDot, Plane,
    Callout, Note, RevisionCloud, Dimension, Distance, Length, Area, Angle, Symbol, Image }
public enum LinePattern { Solid, Dash, Dot, DashDot, LongDash }
public enum ShapeFill { None, Solid, Gradient }
public enum EndpointMarker { None, OpenDot, FilledDot, Arrow }
public enum TextAlign { Left, Center, Right }
public sealed record Annotation(Guid Id, AnnotationKind Kind, PixelPoint Start, PixelPoint End,
    string Color = "#EF4444", double Thickness = 3, string Text = "", double FontSize = 24, double EffectSize = 12,
    LinePattern LinePattern = LinePattern.Solid, ShapeFill Fill = ShapeFill.None, string SecondColor = "#38BDF8",
    double Opacity = 1, double Rotation = 0, IReadOnlyList<PixelPoint>? Points = null, bool Closed = false,
    string Variant = "", Guid? AssetId = null, int Sides = 5,
    EndpointMarker StartMarker = EndpointMarker.None, EndpointMarker EndMarker = EndpointMarker.None,
    string FontFamily = "Segoe UI", bool Bold = false, TextAlign TextAlign = TextAlign.Left,
    bool TextBackground = false, string TextBackgroundColor = "#FFFFFF", double TextBackgroundOpacity = 1, double TextPadding = 8,
    string? FillColor = null, double? FillOpacity = null, double StrokeOpacity = 1, string? TextColor = null,
    bool Italic = false, bool Underline = false, int MeasureDecimals = 2, bool MeasureShowUnit = true, string MeasurePrefix = "", string MeasureSuffix = "",
    bool MirrorHorizontal = false, bool MirrorVertical = false, double TextWidth = 0)
{
    public PixelRect Bounds => Kind is AnnotationKind.Text or AnnotationKind.Number or AnnotationKind.Stamp
        ? new(Start.X, Start.Y, Math.Max(40, Text.Length * FontSize * .7) + (Kind == AnnotationKind.Stamp ? 20 : 0), FontSize * 1.5 + (Kind == AnnotationKind.Stamp ? 20 : 0))
        : Points is { Count: > 0 } ? GeometryMath.Bounds(Points) : PixelRect.Between(Start, End);
    public Annotation Move(double x, double y) => this with
    { Start = new(Start.X + x, Start.Y + y), End = new(End.X + x, End.Y + y), Points = Points?.Select(p => new PixelPoint(p.X + x, p.Y + y)).ToArray() };
    public Annotation Resize(PixelRect target)
    {
        var source = Bounds; var sx = target.Width / Math.Max(1, source.Width); var sy = target.Height / Math.Max(1, source.Height);
        PixelPoint Map(PixelPoint p) => new(target.X + (p.X - source.X) * sx, target.Y + (p.Y - source.Y) * sy);
        return this with { Start = Map(Start), End = Map(End), Points = Points?.Select(Map).ToArray(), FontSize = FontSize * Math.Min(sx, sy) };
    }
}
public sealed class EditorDocument(int width, int height)
{
    public int Width { get; internal set; } = width;
    public int Height { get; internal set; } = height;
    public List<Annotation> Annotations { get; } = [];
    public PixelRect? Crop { get; set; }
    public double UnitsPerPixel { get; set; } = 1;
    public string Unit { get; set; } = "px";
    public Dictionary<Guid, ImageAsset> Assets { get; } = [];
}
public sealed record ImageAsset(byte[] Png, int Width, int Height);
public static class GeometryMath
{
    public static PixelRect Bounds(IReadOnlyList<PixelPoint> points) => new(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y));
    public static double Distance(PixelPoint a, PixelPoint b) => Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2));
    public static double Length(IReadOnlyList<PixelPoint> points) => Enumerable.Range(1, Math.Max(0, points.Count - 1)).Sum(i => Distance(points[i - 1], points[i]));
    public static double PathLength(IReadOnlyList<PixelPoint> points, bool closed) => Length(points) + (closed && points.Count > 1 ? Distance(points[^1], points[0]) : 0);
    public static double Area(IReadOnlyList<PixelPoint> points) => Math.Abs(Enumerable.Range(0, points.Count).Sum(i => points[i].X * points[(i + 1) % points.Count].Y - points[(i + 1) % points.Count].X * points[i].Y)) / 2;
    public static double Angle(PixelPoint a, PixelPoint vertex, PixelPoint b)
    {
        var d = Distance(a, vertex) * Distance(b, vertex);
        return d == 0 ? 0 : Math.Acos(Math.Clamp(((a.X - vertex.X) * (b.X - vertex.X) + (a.Y - vertex.Y) * (b.Y - vertex.Y)) / d, -1, 1)) * 180 / Math.PI;
    }
    public static string StepLabel(int value, bool alphabetic)
    {
        value = Math.Max(1, value); if (!alphabetic) return value.ToString();
        var text = ""; while (value > 0) { value--; text = (char)('A' + value % 26) + text; value /= 26; } return text;
    }
    public static int ParseStep(string text, bool alphabetic)
    {
        if (!alphabetic) return int.TryParse(text, out var n) && n > 0 ? n : 1;
        long value = 0; foreach (var c in text.Trim().ToUpperInvariant()) { if (c is < 'A' or > 'Z') return 1; value = value * 26 + c - 'A' + 1; if (value > int.MaxValue) return 1; }
        return (int)Math.Max(1, value);
    }
}
public sealed class CalibrationCommand(EditorDocument document, double scale, string unit) : IEditorCommand
{
    private readonly double oldScale = document.UnitsPerPixel; private readonly string oldUnit = document.Unit;
    public void Execute() { document.UnitsPerPixel = scale; document.Unit = unit; }
    public void Undo() { document.UnitsPerPixel = oldScale; document.Unit = oldUnit; }
}
public sealed class CanvasSizeCommand(EditorDocument document, int width, int height) : IEditorCommand
{
    private readonly int oldWidth = document.Width, oldHeight = document.Height;
    private readonly PixelRect? oldCrop = document.Crop;
    public void Execute() { document.Width = width; document.Height = height; document.Crop = null; }
    public void Undo() { document.Width = oldWidth; document.Height = oldHeight; document.Crop = oldCrop; }
}
public sealed class ReorderAnnotationCommand(EditorDocument document, Guid id, bool forward) : IEditorCommand
{
    private readonly int before = document.Annotations.FindIndex(a => a.Id == id);
    private readonly int after = forward ? document.Annotations.Count - 1 : 0;
    private void Apply(int index) { var a = document.Annotations.Find(a => a.Id == id); if (a is null) return; document.Annotations.Remove(a); document.Annotations.Insert(index, a); }
    public void Execute() => Apply(after);
    public void Undo() => Apply(before);
}
public sealed class AppSettings
{
    public bool AutoCopy { get; set; } = true;
    public bool KeepHistory { get; set; } = true;
    public bool StartInTray { get; set; }
    public bool Autosave { get; set; } = true;
    public int CaptureDelaySeconds { get; set; }
    public string FileNamePattern { get; set; } = "SnapMark_{date}_{time}";
    public Dictionary<string, string> Hotkeys { get; set; } = new()
    {
        ["Region"] = "PrintScreen", ["Quick"] = "Shift+PrintScreen", ["Window"] = "Alt+PrintScreen", ["Monitor"] = "Ctrl+PrintScreen",
        ["Repeat"] = "Ctrl+Shift+R", ["History"] = "Ctrl+Shift+V", ["Fallback"] = "Ctrl+Shift+S", ["StopVideo"] = "Ctrl+Shift+F10"
    };
    public string ExportFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "SnapMark");
    public string VideoFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "SnapMark");
    public string ProjectFolder { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SnapMark");
    public string HistoryFolder { get; set; } = "";
    public string RecoveryFolder { get; set; } = "";
    public string FfmpegPath { get; set; } = "";
    public string DefaultColor { get; set; } = "#EF4444";
    public int HistoryLimit { get; set; } = 20;
    public int HistoryDays { get; set; }
    public long OutputCounter { get; set; }
    public List<ProjectProfile> Profiles { get; set; } = [];
    public string ActiveProfile { get; set; } = "";
    public bool DateSubfolders { get; set; }
    public bool RunAtStartup { get; set; }
    public string Theme { get; set; } = "Dark";
    public bool SoundFeedback { get; set; }
    public string GoogleDriveFolder { get; set; } = "";
    public string GoogleDriveShareUrl { get; set; } = "";
    public string OneDriveFolder { get; set; } = "";
    public string OneDriveShareUrl { get; set; } = "";
}
public sealed record ProjectProfile(string Name, string Applications, string Color, string FilenamePattern, string ExportFolder, string StylePresetName = "");
public interface ISettingsService { AppSettings Load(); void Save(AppSettings settings); }
public interface IEditorCommand { void Execute(); void Undo(); }
public sealed class CompositeCommand(IReadOnlyList<IEditorCommand> commands) : IEditorCommand
{
    public void Execute() { foreach (var command in commands) command.Execute(); }
    public void Undo() { foreach (var command in commands.Reverse()) command.Undo(); }
}
public sealed class CommandHistory
{
    private readonly Stack<IEditorCommand> undo = new();
    private readonly Stack<IEditorCommand> redo = new();
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public event Action? Changed;
    public void Execute(IEditorCommand command) { command.Execute(); undo.Push(command); redo.Clear(); Changed?.Invoke(); }
    public void Undo() { if (!undo.TryPop(out var c)) return; c.Undo(); redo.Push(c); Changed?.Invoke(); }
    public void Redo() { if (!redo.TryPop(out var c)) return; c.Execute(); undo.Push(c); Changed?.Invoke(); }
}
public sealed class AddAnnotationCommand(EditorDocument document, Annotation annotation) : IEditorCommand
{
    public void Execute() => document.Annotations.Add(annotation);
    public void Undo() => document.Annotations.Remove(annotation);
}
public sealed class DeleteAnnotationCommand(EditorDocument document, Annotation annotation) : IEditorCommand
{
    private readonly int index = document.Annotations.IndexOf(annotation);
    public void Execute() => document.Annotations.Remove(annotation);
    public void Undo() => document.Annotations.Insert(Math.Clamp(index, 0, document.Annotations.Count), annotation);
}
public sealed class ChangeAnnotationCommand(EditorDocument document, Annotation before, Annotation after) : IEditorCommand
{
    private void Replace(Annotation value) { var i = document.Annotations.FindIndex(a => a.Id == value.Id); if (i >= 0) document.Annotations[i] = value; }
    public void Execute() => Replace(after);
    public void Undo() => Replace(before);
}
public sealed class CropCommand(EditorDocument document, PixelRect? crop) : IEditorCommand
{
    private readonly PixelRect? before = document.Crop;
    public void Execute() => document.Crop = crop;
    public void Undo() => document.Crop = before;
}
