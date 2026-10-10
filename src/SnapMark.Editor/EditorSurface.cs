using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;

namespace SnapMark.Editor;

public enum EditorTool { Select, Arrow, Rectangle, Text, Highlight, Number, Crop, Blur, Pixelate, Redact, Stamp,
    Line, RoundedRectangle, Ellipse, Circle, Triangle, Polygon, RegularPolygon, Polyline, Freehand, OpenDot, FilledDot, Plane,
    Callout, Note, RevisionCloud, CloudFreeform, Dimension, Calibrate, Distance, Length, Area, Angle, Symbol, Eraser, Eyedropper }
public enum ToolState { Idle, Drawing, Dragging, Resizing, Collecting, Erasing, VertexDragging, Selecting }
public sealed class EditorSurface : FrameworkElement
{
    public BitmapSource Image { get; }
    public EditorDocument Document { get; }
    public CommandHistory History { get; } = new();
    public EditorTool Tool { get; set; } = EditorTool.Arrow;
    public ToolState State { get; private set; }
    public string Color { get; set; } = "#EF4444";
    public double Thickness { get; set; } = 3;
    public double FontSize { get; set; } = 24;
    public string TextFont { get; set; } = "Segoe UI";
    public bool BoldText { get; set; }
    public TextAlign TextAlignment { get; set; }
    public bool TextBackground { get; set; }
    public string TextBackgroundColor { get; set; } = "#FFFFFF";
    public double TextBackgroundOpacity { get; set; } = 1;
    public double TextPadding { get; set; } = 8;
    public bool ShowGuides { get; set; }
    public event Action<string>? ColorPicked;
    public double EffectSize { get; set; } = 12;
    public string StampText { get; set; } = "RFI";
    public LinePattern LinePattern { get; set; }
    public ShapeFill Fill { get; set; }
    public string SecondColor { get; set; } = "#38BDF8";
    public double ToolOpacity { get; set; } = 1;
    public string? FillColor { get; set; }
    public double? FillOpacity { get; set; }
    public double StrokeOpacity { get; set; } = 1;
    public string? TextColor { get; set; }
    public double Rotation { get; set; }
    public double TextWidth { get; set; }
    public bool ClosedPath { get; set; }
    public bool EditVertices { get; set; }
    public int PolygonSides { get; set; } = 5;
    public EndpointMarker StartMarker { get; set; }
    public EndpointMarker EndMarker { get; set; }
    public string SymbolId { get; set; } = "north";
    public string SymbolText { get; set; } = "";
    public SymbolLabelMode SymbolLabelMode { get; set; }
    public int NextSymbol { get; set; } = 1;
    public string SymbolPrefix { get; set; } = "A-";
    public bool AlphabeticSymbols { get; set; }
    public event Action? SymbolSequenceChanged;
    public int NextStep { get; set; } = 1;
    public bool AlphabeticSteps { get; set; }
    public Func<double, (double Length, string Unit)?>? RequestCalibration { get; set; }
    public event Action? StepChanged;
    public event Action? SelectionChanged;
    public Func<string, string?>? RequestText { get; set; }
    public Guid? SelectedId { get; private set; }
    private PixelPoint start;
    private Annotation? draft, moving;
    private PixelRect? cropDraft;
    private readonly AnnotationRenderer renderer = new();
    private readonly List<PixelPoint> path = [];
    private readonly HashSet<Guid> selection = [];
    public IReadOnlyCollection<Guid> Selection => selection;
    private PixelPoint dragDelta;
    private PixelPoint eraserEnd;
    private int vertexIndex;
    private bool IsPathTool => Tool is EditorTool.Polygon or EditorTool.Polyline or EditorTool.CloudFreeform or EditorTool.Area or EditorTool.Angle or EditorTool.Length;
    private bool marqueeAdditive;
    private Action<string>? pendingColorPick;
    public bool IsPickingColor => pendingColorPick is not null;
    public event Action? ColorPickEnded;
    public void BeginColorPick(Action<string> apply) { CancelGesture(); pendingColorPick = apply; Cursor = Cursors.Cross; Focus(); }
    public void CancelColorPick() { if (pendingColorPick is null) return; pendingColorPick = null; Cursor = Tool == EditorTool.Select ? Cursors.Arrow : Cursors.Cross; ColorPickEnded?.Invoke(); }
    public void PickColorAt(PixelPoint point)
    {
        if (pendingColorPick is not { } apply) return;
        var color = SampleColor(point); pendingColorPick = null;
        try { apply(color); } finally { Cursor = Tool == EditorTool.Select ? Cursors.Arrow : Cursors.Cross; ColorPickEnded?.Invoke(); }
    }
    private Annotation Create(AnnotationKind kind, PixelPoint end, string text = "", IReadOnlyList<PixelPoint>? points = null) =>
        new(Guid.NewGuid(), kind, start, end, Color, Thickness, text, FontSize, EffectSize, LinePattern, Fill, SecondColor, ToolOpacity, Rotation, points, ClosedPath, SymbolId, Sides: PolygonSides, StartMarker: StartMarker, EndMarker: EndMarker, FontFamily: TextFont, Bold: BoldText, TextAlign: TextAlignment, TextBackground: TextBackground, TextBackgroundColor: TextBackgroundColor, TextBackgroundOpacity: TextBackgroundOpacity, TextPadding: TextPadding, FillColor: FillColor, FillOpacity: FillOpacity, StrokeOpacity: StrokeOpacity, TextColor: TextColor, TextWidth: TextWidth);
    public void ApplyTextBackgroundToSelection()
    {
        var changes = Document.Annotations.Where(a => selection.Contains(a.Id) && a.Kind == AnnotationKind.Text).Select(a => (a, a with { TextBackground = TextBackground, TextBackgroundColor = TextBackgroundColor, TextBackgroundOpacity = TextBackgroundOpacity, TextPadding = TextPadding, FillColor = FillColor, FillOpacity = FillOpacity, StrokeOpacity = StrokeOpacity, TextColor = TextColor })).ToArray();
        if (changes.Length > 0) History.Execute(new CompositeCommand(changes.Select(c => new ChangeAnnotationCommand(Document, c.a, c.Item2)).ToArray()));
    }
    public EditorSurface(BitmapSource image, EditorDocument? document = null)
    {
        Image = image; Document = document ?? new(image.PixelWidth, image.PixelHeight);
        Width = Document.Width; Height = Document.Height; Focusable = true;
        History.Changed += () => { Width = Document.Width; Height = Document.Height; selection.RemoveWhere(id => !Document.Annotations.Any(a => a.Id == id)); if (!Document.Annotations.Any(a => a.Id == SelectedId)) SelectedId = selection.Count > 0 ? selection.First() : null; SelectionChanged?.Invoke(); InvalidateVisual(); };
        LostMouseCapture += (_, _) => { if (State == ToolState.Collecting) return; State = ToolState.Idle; draft = moving = null; cropDraft = null; InvalidateVisual(); };
    }
    private PixelPoint Position(MouseEventArgs e)
    {
        var p = e.GetPosition(this);
        return new(Math.Clamp(p.X, 0, Width), Math.Clamp(p.Y, 0, Height));
    }
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        Focus(); start = Position(e); e.Handled = true;
        if (IsPickingColor) { PickColorAt(start); return; }
        if (Tool == EditorTool.Eyedropper) { ColorPicked?.Invoke(SampleColor(start)); return; }
        if (IsPathTool)
        {
            if (Tool != EditorTool.Angle && path.Count >= 3 && GeometryMath.Distance(start, path[0]) <= 8) { FinishPath(true); return; }
            if (e.ClickCount == 2) { FinishPath(); return; }
            path.Add(start); State = ToolState.Collecting;
            if (Tool == EditorTool.Angle && path.Count == 3) FinishPath();
            InvalidateVisual(); return;
        }
        if (Tool == EditorTool.Eraser)
        {
            eraserEnd = start; State = ToolState.Erasing; CaptureMouse(); InvalidateVisual(); return;
        }
        if (Tool == EditorTool.Select)
        {
            var current = Document.Annotations.Find(a => a.Id == SelectedId);
            if (current is not null)
            {
                var handles = SelectionHandles(current, EditVertices);
                var nearest = Enumerable.Range(0, handles.Count).OrderBy(i => GeometryMath.Distance(start, handles[i])).First();
                if (GeometryMath.Distance(start, handles[nearest]) < HandleRadius)
                {
                    moving = current; vertexIndex = handles.Count == 1 ? 2 : nearest;
                    State = UsesVertices(current, EditVertices) ? ToolState.VertexDragging : ToolState.Resizing;
                    CaptureMouse(); return;
                }
            }
            moving = Document.Annotations.Where(PixelEffects.IsEffect).Concat(Document.Annotations.Where(a => !PixelEffects.IsEffect(a))).LastOrDefault(a => Hit(a, start));
            if (moving is null)
            {
                marqueeAdditive = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
                eraserEnd = start; dragDelta = new(0, 0); State = ToolState.Selecting; CaptureMouse(); InvalidateVisual(); return;
            }
            SelectedId = moving.Id;
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) selection.Clear();
            if (moving is not null && !selection.Add(moving.Id) && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { selection.Remove(moving.Id); moving = null; SelectedId = selection.Count > 0 ? selection.Last() : null; }
            SelectionChanged?.Invoke();
            dragDelta = new(0, 0);
            if (moving is not null && e.ClickCount == 2 && moving.Kind is AnnotationKind.Text or AnnotationKind.Stamp or AnnotationKind.Number or AnnotationKind.Note or AnnotationKind.Callout or AnnotationKind.Dimension or AnnotationKind.Symbol)
            {
                var editTarget = moving; moving = null; State = ToolState.Idle; ReleaseMouseCapture();
                var text = RequestText?.Invoke(editTarget.Text);
                if (!string.IsNullOrWhiteSpace(text)) History.Execute(new ChangeAnnotationCommand(Document, editTarget, editTarget with { Text = text }));
                moving = null;
            }
            else if (moving is not null) { State = ToolState.Dragging; CaptureMouse(); }
        }
        else if (Tool is EditorTool.Text or EditorTool.Number or EditorTool.Stamp or EditorTool.Note or EditorTool.OpenDot or EditorTool.FilledDot)
        {
            var text = Tool is EditorTool.Text or EditorTool.Note ? RequestText?.Invoke("") : Tool == EditorTool.Stamp ? StampText : Tool == EditorTool.Number ? GeometryMath.StepLabel(NextStep, AlphabeticSteps) : "";
            if (text is not null && (text.Length > 0 || Tool is EditorTool.OpenDot or EditorTool.FilledDot))
            {
                History.Execute(new AddAnnotationCommand(Document, Create(Enum.Parse<AnnotationKind>(Tool.ToString()), start, text)));
                if (Tool == EditorTool.Number) { NextStep++; StepChanged?.Invoke(); }
            }
        }
        else { path.Clear(); path.Add(start); State = ToolState.Drawing; CaptureMouse(); }
        InvalidateVisual();
    }
    private double HandleRadius => 10 / Math.Max(.05, Math.Abs(LayoutTransform.Value.M11));
    private static PixelPoint ToPixel(Point point) => new(point.X, point.Y);
    private static bool Hit(Annotation a, PixelPoint p)
    {
        var local = AnnotationRenderer.ObjectTransform(a).Inverse!.Transform(new Point(p.X, p.Y)); p = new(local.X, local.Y);
        if (a.Kind is AnnotationKind.Arrow or AnnotationKind.Line or AnnotationKind.Dimension or AnnotationKind.Distance)
        {
            var dx = a.End.X - a.Start.X; var dy = a.End.Y - a.Start.Y;
            var length = dx * dx + dy * dy;
            var t = length == 0 ? 0 : Math.Clamp(((p.X - a.Start.X) * dx + (p.Y - a.Start.Y) * dy) / length, 0, 1);
            return Math.Pow(p.X - a.Start.X - t * dx, 2) + Math.Pow(p.Y - a.Start.Y - t * dy, 2) <= Math.Pow(Math.Max(8, a.Thickness), 2);
        }
        if (a.Kind == AnnotationKind.Number) return Math.Pow(p.X - a.Start.X, 2) + Math.Pow(p.Y - a.Start.Y, 2) <= Math.Pow(a.FontSize, 2);
        var b = AnnotationRenderer.GetBounds(a);
        return new PixelRect(b.X - 5, b.Y - 5, b.Width + 10, b.Height + 10).Contains(p);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (State == ToolState.Idle) return;
        var end = Position(e);
        if (State is ToolState.Erasing or ToolState.Selecting) { eraserEnd = end; InvalidateVisual(); return; }
        if (State == ToolState.VertexDragging && moving is not null) { draft = MoveVertex(moving, vertexIndex, end); InvalidateVisual(); return; }
        if (State == ToolState.Collecting)
        { if (path.Count > 0) draft = Create(PathKind(), end, points: path.Append(end).ToArray()) with { Start = path[0], Closed = Tool is EditorTool.Polygon or EditorTool.Area or EditorTool.CloudFreeform || ClosedPath }; InvalidateVisual(); return; }
        if (State == ToolState.Dragging && moving is not null) { dragDelta = new(end.X - start.X, end.Y - start.Y); draft = moving.Move(dragDelta.X, dragDelta.Y); }
        else if (State == ToolState.Resizing && moving is not null)
        {
            draft = ResizeCorner(moving, vertexIndex, end);
        }
        else if (Tool == EditorTool.Crop)
        {
            var rect = PixelRect.Between(start, end);
            cropDraft = new(Math.Floor(rect.X), Math.Floor(rect.Y), Math.Floor(rect.Width), Math.Floor(rect.Height));
        }
        else
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (Tool is EditorTool.Arrow or EditorTool.Line or EditorTool.Dimension or EditorTool.Distance or EditorTool.Calibrate)
                {
                    var angle = Math.Round(Math.Atan2(end.Y - start.Y, end.X - start.X) / (Math.PI / 4)) * Math.PI / 4;
                    var length = Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
                    end = new(start.X + Math.Cos(angle) * length, start.Y + Math.Sin(angle) * length);
                }
                else { var size = Math.Min(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y)); end = new(start.X + Math.Sign(end.X - start.X) * size, start.Y + Math.Sign(end.Y - start.Y) * size); }
            }
            if (Tool == EditorTool.Circle)
            { var size = Math.Min(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y)); end = new(start.X + Math.Sign(end.X - start.X) * size, start.Y + Math.Sign(end.Y - start.Y) * size); }
            var kind = Tool switch
            {
                EditorTool.Rectangle => AnnotationKind.Rectangle, EditorTool.Highlight => AnnotationKind.Highlight,
                EditorTool.Blur => AnnotationKind.Blur, EditorTool.Pixelate => AnnotationKind.Pixelate,
                EditorTool.Redact => AnnotationKind.Redact, EditorTool.RegularPolygon => AnnotationKind.Polygon,
                EditorTool.Calibrate => AnnotationKind.Line, _ => Enum.Parse<AnnotationKind>(Tool.ToString())
            };
            IReadOnlyList<PixelPoint>? points = null;
            if (Tool == EditorTool.Freehand) { if (GeometryMath.Distance(path[^1], end) >= 1) path.Add(end); points = path.ToArray(); }
            if (Tool == EditorTool.RegularPolygon)
            {
                var bounds = PixelRect.Between(start, end); var count = Math.Clamp(PolygonSides, 3, 32);
                points = Enumerable.Range(0, count).Select(i => new PixelPoint(bounds.X + bounds.Width / 2 + Math.Cos(i * Math.PI * 2 / count - Math.PI / 2) * bounds.Width / 2, bounds.Y + bounds.Height / 2 + Math.Sin(i * Math.PI * 2 / count - Math.PI / 2) * bounds.Height / 2)).ToArray();
            }
            draft = Create(kind, end, Tool == EditorTool.Callout ? GeometryMath.StepLabel(NextStep, AlphabeticSteps) : Tool == EditorTool.Symbol ? SymbolSequence.Label(SymbolLabelMode, NextSymbol, SymbolPrefix, AlphabeticSymbols, SymbolText) : "", points);
        }
        InvalidateVisual();
    }
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || State is ToolState.Idle or ToolState.Collecting) return;
        CompleteGesture(Position(e)); e.Handled = true;
    }
    public void CompleteGesture(PixelPoint end)
    {
        if (State is ToolState.Idle or ToolState.Collecting) return;
        // A modal dialog runs a nested input loop. Snapshot the completed gesture
        // and release capture before showing it; LostMouseCapture can clear fields.
        var completedState = State; var completedTool = Tool;
        var completedDraft = draft; var completedMoving = moving; var completedCrop = cropDraft;
        var completedStart = start; var completedDelta = dragDelta;
        State = ToolState.Idle; draft = moving = null; cropDraft = null;
        ReleaseMouseCapture(); InvalidateVisual();
        if (completedState == ToolState.Selecting) SelectRegion(completedStart, end, marqueeAdditive);
        else if (completedState == ToolState.Erasing)
        {
            if (GeometryMath.Distance(completedStart, end) >= 3) EraseRegion(completedStart, end);
            else
            {
                var hit = Document.Annotations.LastOrDefault(a => Hit(a, completedStart));
                if (hit is not null) History.Execute(new DeleteAnnotationCommand(Document, hit));
            }
        }
        else if (completedState is ToolState.Resizing or ToolState.VertexDragging && completedMoving is not null && completedDraft is not null && completedDraft != completedMoving)
            History.Execute(new ChangeAnnotationCommand(Document, completedMoving, completedDraft));
        else if (completedState == ToolState.Dragging && completedMoving is not null && completedDraft is not null && completedDraft != completedMoving)
            History.Execute(new CompositeCommand(Document.Annotations.Where(a => selection.Contains(a.Id)).Select(a => (IEditorCommand)new ChangeAnnotationCommand(Document, a, a.Move(completedDelta.X, completedDelta.Y))).ToArray()));
        else if (completedTool == EditorTool.Calibrate && completedDraft is not null)
        {
            var pixels = GeometryMath.Distance(completedDraft.Start, completedDraft.End);
            if (pixels >= 2 && RequestCalibration?.Invoke(pixels) is { } reference && double.IsFinite(reference.Length) && reference.Length > 0 && double.IsFinite(reference.Length / pixels))
                History.Execute(new CalibrationCommand(Document, reference.Length / pixels, reference.Unit));
        }
        else if (completedDraft is not null && (PixelEffects.IsEffect(completedDraft)
            ? completedDraft.Bounds.Width >= 2 && completedDraft.Bounds.Height >= 2
            : Math.Abs(completedDraft.Start.X - completedDraft.End.X) >= 2 || Math.Abs(completedDraft.Start.Y - completedDraft.End.Y) >= 2))
        {
            if (completedTool == EditorTool.Dimension) { var text = RequestText?.Invoke("3500 mm"); if (text is null) return; completedDraft = completedDraft with { Text = text }; }
            History.Execute(new AddAnnotationCommand(Document, completedDraft));
            if (completedTool == EditorTool.Symbol && SymbolLabelMode != SymbolLabelMode.Custom) { NextSymbol = Math.Min(1_000_000, NextSymbol + 1); SymbolSequenceChanged?.Invoke(); }
            if (completedTool == EditorTool.Callout) { NextStep++; StepChanged?.Invoke(); }
        }
        else if (completedCrop is { Width: >= 2, Height: >= 2 }) History.Execute(new CropCommand(Document, completedCrop));
        InvalidateVisual();
    }
    private AnnotationKind PathKind() => Tool == EditorTool.CloudFreeform ? AnnotationKind.RevisionCloud : Enum.Parse<AnnotationKind>(Tool.ToString());
    public void FinishPath(bool close = false)
    {
        var required = Tool is EditorTool.Polygon or EditorTool.Area or EditorTool.CloudFreeform or EditorTool.Angle ? 3 : 2;
        if (path.Count >= required)
        {
            start = path[0]; var annotation = Create(PathKind(), path[^1], points: path.ToArray()) with { Closed = close || Tool is EditorTool.Polygon or EditorTool.Area or EditorTool.CloudFreeform || ClosedPath };
            History.Execute(new AddAnnotationCommand(Document, annotation));
        }
        CancelGesture();
    }
    public void MeasureSelectedPath(bool area)
    {
        ChangeSelectedProperty(a => a.Points is null || a.Points.Count < (area ? 3 : 2) ? a : a with { Kind = area ? AnnotationKind.Area : AnnotationKind.Length, Closed = area || a.Closed || a.Kind is AnnotationKind.Polygon or AnnotationKind.Area }, k => k is AnnotationKind.Polyline or AnnotationKind.Polygon or AnnotationKind.Freehand or AnnotationKind.Length or AnnotationKind.Area);
    }
    public void CancelGesture() { CancelColorPick(); State = ToolState.Idle; path.Clear(); draft = moving = null; cropDraft = null; ReleaseMouseCapture(); InvalidateVisual(); }
    public void SelectRegion(PixelPoint from, PixelPoint to, bool additive = false)
    {
        Tool = EditorTool.Select;
        var matches = Document.Annotations.Where(a => MarqueeHitTest.Matches(a, from, to)).Select(a => a.Id).ToArray();
        if (!additive) selection.Clear(); foreach (var id in matches) selection.Add(id);
        SelectedId = matches.Length > 0 ? matches[^1] : selection.Count > 0 ? selection.Last() : null;
        SelectionChanged?.Invoke(); InvalidateVisual();
    }
    public void DeleteSelection()
    {
        var items = Document.Annotations.Where(a => selection.Contains(a.Id)).ToArray();
        if (items.Length > 0) History.Execute(new CompositeCommand(items.Reverse().Select(a => (IEditorCommand)new DeleteAnnotationCommand(Document, a)).ToArray()));
    }
    public void EraseRegion(PixelPoint from, PixelPoint to)
    {
        var items = Document.Annotations.Where(a => MarqueeHitTest.Matches(a, from, to)).ToArray();
        if (items.Length > 0) History.Execute(new CompositeCommand(items.Reverse().Select(a => (IEditorCommand)new DeleteAnnotationCommand(Document, a)).ToArray()));
    }
    private static bool UsesVertices(Annotation a, bool edit) => a.Kind is AnnotationKind.Triangle or AnnotationKind.Arrow or AnnotationKind.Line or AnnotationKind.Dimension or AnnotationKind.Distance or AnnotationKind.Callout || a.Points is { Count: > 0 } && (a.Kind != AnnotationKind.Freehand || a.Points.Count <= 100 || edit);
    private static PixelPoint[] LocalVertices(Annotation a)
    {
        if (a.Points is { Count: > 0 }) return a.Points.ToArray();
        if (a.Kind == AnnotationKind.Triangle) { var b = a.Bounds; return [new(b.X + b.Width / 2, b.Y), new(b.Right, b.Bottom), new(b.X, b.Bottom)]; }
        return [a.Start, a.End];
    }
    public static IReadOnlyList<PixelPoint> SelectionHandles(Annotation a, bool edit = false)
    {
        if (UsesVertices(a, edit)) return VertexPoints(a);
        var b = AnnotationRenderer.GetBounds(a);
        var corners = new[] { new Point(b.X, b.Y), new Point(b.Right, b.Y), new Point(b.Right, b.Bottom), new Point(b.X, b.Bottom) };
        if (a.Kind is AnnotationKind.Text or AnnotationKind.Note or AnnotationKind.Stamp or AnnotationKind.Number or AnnotationKind.OpenDot or AnnotationKind.FilledDot) corners = [corners[2]];
        var transform = AnnotationRenderer.ObjectTransform(a); return corners.Select(transform.Transform).Select(ToPixel).ToArray();
    }
    public static Annotation ResizeCorner(Annotation a, int corner, PixelPoint target)
    {
        if (corner < 0 || corner > 3) throw new ArgumentOutOfRangeException(nameof(corner));
        var b = AnnotationRenderer.GetBounds(a); var transform = AnnotationRenderer.ObjectTransform(a);
        var local = transform.Inverse!.Transform(new Point(target.X, target.Y));
        var left = corner is 0 or 3; var top = corner is 0 or 1;
        var fixedLocal = new Point(left ? b.Right : b.X, top ? b.Bottom : b.Y); var fixedWorld = transform.Transform(fixedLocal);
        var width = Math.Max(8, left ? fixedLocal.X - local.X : local.X - fixedLocal.X); var height = Math.Max(8, top ? fixedLocal.Y - local.Y : local.Y - fixedLocal.Y);
        if (a.Kind == AnnotationKind.Circle) width = height = Math.Min(width, height);
        var box = new PixelRect(left ? fixedLocal.X - width : fixedLocal.X, top ? fixedLocal.Y - height : fixedLocal.Y, width, height);
        var resized = a.Kind is AnnotationKind.Text or AnnotationKind.Note or AnnotationKind.Stamp or AnnotationKind.Number or AnnotationKind.OpenDot or AnnotationKind.FilledDot
            ? a with { FontSize = Math.Max(6, a.FontSize * Math.Min(width / Math.Max(1, b.Width), height / Math.Max(1, b.Height))) }
            : a.Resize(box);
        var next = AnnotationRenderer.GetBounds(resized); var opposite = new Point(left ? next.Right : next.X, top ? next.Bottom : next.Y);
        var after = AnnotationRenderer.ObjectTransform(resized).Transform(opposite);
        return resized.Move(fixedWorld.X - after.X, fixedWorld.Y - after.Y);
    }
    private static PixelPoint[] VertexPoints(Annotation a)
    {
        var rotate = AnnotationRenderer.ObjectTransform(a);
        return LocalVertices(a).Select(p => rotate.Transform(new Point(p.X, p.Y))).Select(p => new PixelPoint(p.X, p.Y)).ToArray();
    }
    public static Annotation MoveVertex(Annotation a, int index, PixelPoint target)
    {
        var points = VertexPoints(a); if (index < 0 || index >= points.Length) throw new ArgumentOutOfRangeException(nameof(index));
        points[index] = target;
        return a with { Kind = a.Kind == AnnotationKind.Triangle ? AnnotationKind.Polygon : a.Kind, Start = points[0], End = points[^1], Points = a.Points is not null || a.Kind == AnnotationKind.Triangle ? points : null, Rotation = 0, MirrorHorizontal = false, MirrorVertical = false };
    }
    public void MoveSelection(double x, double y)
    {
        var items = Document.Annotations.Where(a => selection.Contains(a.Id)).ToArray();
        if (items.Length > 0) History.Execute(new CompositeCommand(items.Select(a => (IEditorCommand)new ChangeAnnotationCommand(Document, a, a.Move(x, y))).ToArray()));
    }
    public void DuplicateSelection()
    {
        var copies = Document.Annotations.Where(a => selection.Contains(a.Id)).Select(a => a.Move(12, 12) with { Id = Guid.NewGuid() }).ToArray();
        if (copies.Length == 0) return;
        History.Execute(new CompositeCommand(copies.Select(a => (IEditorCommand)new AddAnnotationCommand(Document, a)).ToArray()));
        selection.Clear(); foreach (var copy in copies) selection.Add(copy.Id); SelectedId = copies[^1].Id; SelectionChanged?.Invoke(); InvalidateVisual();
    }
    public void ApplyStyleToSelection()
    {
        var a = Document.Annotations.Find(a => a.Id == SelectedId);
        if (a is null) return;
        var changes = Document.Annotations.Where(a => selection.Contains(a.Id)).Select(a => (IEditorCommand)new ChangeAnnotationCommand(Document, a, a with
        { Color = Color, Thickness = Thickness, FontSize = FontSize, EffectSize = EffectSize, LinePattern = LinePattern, Fill = Fill, SecondColor = SecondColor, Opacity = ToolOpacity, Rotation = Rotation, StartMarker = StartMarker, EndMarker = EndMarker, FontFamily = TextFont, Bold = BoldText, TextAlign = TextAlignment, TextWidth = TextWidth > 0 ? TextWidth : a.TextWidth, TextBackground = TextBackground, TextBackgroundColor = TextBackgroundColor, TextBackgroundOpacity = TextBackgroundOpacity, TextPadding = TextPadding, FillColor = FillColor, FillOpacity = FillOpacity, StrokeOpacity = StrokeOpacity, TextColor = TextColor })).ToArray();
        if (changes.Length > 0) History.Execute(new CompositeCommand(changes));
    }
    public void SelectObject(Guid id, bool additive = false)
    {
        if (!Document.Annotations.Any(a => a.Id == id)) return;
        CancelGesture(); Tool = EditorTool.Select; if (!additive) selection.Clear(); selection.Add(id); SelectedId = id; SelectionChanged?.Invoke(); InvalidateVisual();
    }
    public void ChangeSelectedProperty(Func<Annotation, Annotation> change, Func<AnnotationKind, bool>? supports = null)
    {
        var commands = Document.Annotations.Where(a => selection.Contains(a.Id) && (supports?.Invoke(a.Kind) ?? true)).Select(a => (Before: a, After: change(a))).Where(p => p.Before != p.After).Select(p => (IEditorCommand)new ChangeAnnotationCommand(Document, p.Before, p.After)).ToArray();
        if (commands.Length > 0) History.Execute(new CompositeCommand(commands));
    }
    public UserStylePreset SaveStyle(string name) => new(name, Color, SecondColor, Thickness, FontSize, ToolOpacity, LinePattern, Fill, Rotation, StartMarker, EndMarker, EffectSize, StampText, PolygonSides, ClosedPath, TextFont, BoldText, TextAlignment, TextBackground, TextBackgroundColor, TextBackgroundOpacity, TextPadding, FillColor, FillOpacity, StrokeOpacity, TextColor);
    public void LoadStyle(UserStylePreset p)
    {
        Color = p.Color; SecondColor = p.SecondColor; Thickness = p.Thickness; FontSize = p.FontSize; ToolOpacity = p.Opacity; LinePattern = p.LinePattern; Fill = p.Fill; Rotation = p.Rotation;
        StartMarker = p.StartMarker; EndMarker = p.EndMarker; EffectSize = p.EffectSize; StampText = p.StampText; PolygonSides = p.PolygonSides; ClosedPath = p.ClosedPath;
        TextFont = p.FontFamily; BoldText = p.Bold; TextAlignment = p.TextAlign;
        TextBackground = p.TextBackground; TextBackgroundColor = p.TextBackgroundColor; TextBackgroundOpacity = p.TextBackgroundOpacity; TextPadding = p.TextPadding;
        FillColor = p.FillColor; FillOpacity = p.FillOpacity; StrokeOpacity = p.StrokeOpacity; TextColor = p.TextColor;
    }
    public string SampleColor(PixelPoint point)
    {
        var full = new EditorDocument(Document.Width, Document.Height); full.Annotations.AddRange(Document.Annotations); foreach (var asset in Document.Assets) full.Assets.Add(asset.Key, asset.Value);
        var displayed = new AnnotationRenderer().Render(Image, full); var pixel = new byte[4];
        displayed.CopyPixels(new Int32Rect(Math.Clamp((int)point.X, 0, displayed.PixelWidth - 1), Math.Clamp((int)point.Y, 0, displayed.PixelHeight - 1), 1, 1), pixel, 4, 0);
        return $"#{pixel[2]:X2}{pixel[1]:X2}{pixel[0]:X2}";
    }
    public void EditSelectedText()
    {
        var a = Document.Annotations.Find(a => a.Id == SelectedId);
        if (a?.Kind is not (AnnotationKind.Text or AnnotationKind.Stamp or AnnotationKind.Number or AnnotationKind.Callout or AnnotationKind.Note or AnnotationKind.Dimension or AnnotationKind.Symbol)) return;
        var text = RequestText?.Invoke(a.Text);
        if (!string.IsNullOrWhiteSpace(text) && text != a.Text) History.Execute(new ChangeAnnotationCommand(Document, a, a with { Text = text }));
    }
    public void ReorderSelection(bool forward) { if (SelectedId is { } id) History.Execute(new ReorderAnnotationCommand(Document, id, forward)); }
    public void ResizeCanvas(int width, int height)
    {
        if (width < Image.PixelWidth || height < Image.PixelHeight || width > 16000 || height > 16000 || (long)width * height > 64_000_000) throw new ArgumentException("Canvas must fit the original image and stay within 64 megapixels / 16000 pixels per side.");
        History.Execute(new CanvasSizeCommand(Document, width, height));
    }
    public void InsertImage(BitmapSource bitmap)
    {
        var id = Guid.NewGuid(); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new System.IO.MemoryStream(); encoder.Save(stream); Document.Assets[id] = new(stream.ToArray(), bitmap.PixelWidth, bitmap.PixelHeight);
        var scale = Math.Min(1, Math.Min(Width * .6 / bitmap.PixelWidth, Height * .6 / bitmap.PixelHeight));
        var annotation = new Annotation(Guid.NewGuid(), AnnotationKind.Image, new(20, 20), new(20 + bitmap.PixelWidth * scale, 20 + bitmap.PixelHeight * scale), AssetId: id);
        History.Execute(new AddAnnotationCommand(Document, annotation)); Tool = EditorTool.Select; SelectedId = annotation.Id; selection.Clear(); selection.Add(annotation.Id); SelectionChanged?.Invoke(); InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        // Rulers/grid are editor guides only; exports continue to use the document renderer.
        IEnumerable<Annotation> all = Document.Annotations;
        if (draft is not null) all = moving is not null ? all.Select(a => State == ToolState.Dragging && selection.Contains(a.Id) ? a.Move(dragDelta.X, dragDelta.Y) : a.Id == moving.Id ? draft : a) : all.Append(draft);
        renderer.DrawDocument(dc, Image, all, Document);
        if (ShowGuides)
        {
            var pen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(90, 0, 150, 210)), .5);
            void Label(double value, Point position) => dc.DrawText(new FormattedText($"{value * Document.UnitsPerPixel:0.##}", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10, Brushes.DeepSkyBlue, 1), position);
            for (var x = 0; x < Width; x += 100) { dc.DrawLine(pen, new(x, 0), new(x, Height)); Label(x, new(x + 2, 2)); }
            for (var y = 100; y < Height; y += 100) { dc.DrawLine(pen, new(0, y), new(Width, y)); Label(y, new(2, y + 2)); }
        }
        if (State is ToolState.Erasing or ToolState.Selecting)
        {
            var crossing = eraserEnd.X < start.X; var color = crossing ? Brushes.LimeGreen : Brushes.DeepSkyBlue;
            var area = PixelRect.Between(start, eraserEnd);
            dc.DrawRectangle(new SolidColorBrush(crossing ? System.Windows.Media.Color.FromArgb(30, 50, 205, 50) : System.Windows.Media.Color.FromArgb(30, 0, 191, 255)),
                new Pen(color, 1) { DashStyle = crossing ? DashStyles.Dash : DashStyles.Solid }, new Rect(area.X, area.Y, area.Width, area.Height));
            foreach (var item in Document.Annotations.Where(a => MarqueeHitTest.Matches(a, start, eraserEnd)))
            {
                var b = AnnotationRenderer.GetBounds(item);
                dc.PushTransform(AnnotationRenderer.ObjectTransform(item));
                dc.DrawRectangle(null, new Pen(State == ToolState.Erasing ? Brushes.OrangeRed : color, 2), new Rect(b.X - 3, b.Y - 3, b.Width + 6, b.Height + 6)); dc.Pop();
            }
        }
        if ((cropDraft ?? Document.Crop) is { } crop)
        {
            var r = new Rect(crop.X, crop.Y, crop.Width, crop.Height);
            dc.DrawGeometry(new SolidColorBrush(System.Windows.Media.Color.FromArgb(145, 0, 0, 0)), null,
                new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(0, 0, Width, Height)), new RectangleGeometry(r)));
            dc.DrawRectangle(null, new Pen(Brushes.DeepSkyBlue, 1), r);
        }
        if (Tool == EditorTool.Select)
            foreach (var other in Document.Annotations.Where(a => selection.Contains(a.Id) && a.Id != SelectedId))
            {
                var b = AnnotationRenderer.GetBounds(other.Move(dragDelta.X, dragDelta.Y));
                dc.PushTransform(AnnotationRenderer.ObjectTransform(other.Move(dragDelta.X, dragDelta.Y)));
                dc.DrawRectangle(null, new Pen(Brushes.DeepSkyBlue, 1) { DashStyle = DashStyles.Dash }, new Rect(b.X - 5, b.Y - 5, b.Width + 10, b.Height + 10)); dc.Pop();
            }
        var selected = draft ?? Document.Annotations.Find(a => a.Id == SelectedId);
        if (selected is not null && Tool == EditorTool.Select)
        {
            var b = AnnotationRenderer.GetBounds(selected);
            dc.PushTransform(AnnotationRenderer.ObjectTransform(selected));
            dc.DrawRectangle(null, new Pen(Brushes.DeepSkyBlue, 1) { DashStyle = DashStyles.Dash }, new Rect(b.X - 5, b.Y - 5, b.Width + 10, b.Height + 10));
            dc.Pop();
            var half = HandleRadius / 2;
            foreach (var point in SelectionHandles(selected, EditVertices)) dc.DrawRectangle(UsesVertices(selected, EditVertices) ? Brushes.Orange : Brushes.DeepSkyBlue, new Pen(Brushes.White, 1 / Math.Max(.05, Math.Abs(LayoutTransform.Value.M11))), new Rect(point.X - half, point.Y - half, half * 2, half * 2));
        }
    }
}
