namespace SnapMark.Editor;

public sealed record ToolCategoryDefinition(string Id, string Name, string Description);
public sealed record ToolDefinition(string Id, string CategoryId, EditorTool Tool, string Name, string Hint);
public sealed record StampDefinition(string Id, string Group, string Text)
{
    public string Caption => Group + " · " + Text;
}
public sealed record StylePreset(string Name, string Color, double Thickness, double FontSize);

public static class ToolCatalog
{
    public static IReadOnlyList<ToolCategoryDefinition> Categories { get; } = new ToolCategoryDefinition[]
    {
        new("edit", "Edit · จัดการภาพ", "Select, move, delete and crop"),
        new("draw", "Draw · เส้นและรูปทรง", "Arrows and shapes"),
        new("redact", "Highlight & Redact · เน้น/ปิดบัง", "Drag a region to highlight or obscure"),
        new("label", "Label & Callout · หมายเหตุ", "Text and numbered steps"),
        new("review", "Review · ตรวจแบบ", "Design stamps"),
        new("measure", "Dimension & Measure · มิติ", "Dimensions, calibration and image measurement"),
        new("symbols", "Symbols · สัญลักษณ์", "Drawing and coordination markers")
    };
    public static IReadOnlyList<ToolDefinition> Tools { get; } = new ToolDefinition[]
    {
        new("edit.select", "edit", EditorTool.Select, "Select · V", "Drag blank canvas to select: left→right enclosed, right→left crossing; Ctrl/Shift adds; Delete removes. Drag objects to move; corners/vertices reshape."),
        new("edit.crop", "edit", EditorTool.Crop, "Crop", "Drag a non-destructive crop"),
        new("edit.eyedropper", "edit", EditorTool.Eyedropper, "Eyedropper · pick image color", "Click the displayed image to pick HEX color, including markup"),
        new("draw.arrow", "draw", EditorTool.Arrow, "Arrow · A", "Drag; Shift snaps to 45°"),
        new("draw.rectangle", "draw", EditorTool.Rectangle, "Rectangle · R", "Drag; Shift for square"),
        new("redact.highlight", "redact", EditorTool.Highlight, "Highlight · H", "Transparent colored fill"),
        new("redact.blur", "redact", EditorTool.Blur, "Blur · B", "Softens detail; use Solid cover to fully cover data"),
        new("redact.pixelate", "redact", EditorTool.Pixelate, "Pixelate · P", "Mosaic blocks; original remains editable"),
        new("redact.solid", "redact", EditorTool.Redact, "Solid cover · X", "Opaque black cover in copied/saved output"),
        new("label.text", "label", EditorTool.Text, "Text · T", "Click to place text"),
        new("label.number", "label", EditorTool.Number, "Number · N", "Click to add a numbered step"),
        new("review.stamp", "review", EditorTool.Stamp, "Stamp · S", "Choose a label, then click to place; double-click to edit"),
        new("edit.eraser", "edit", EditorTool.Eraser, "Eraser · drag window", "Click to erase one object. Drag left→right: fully enclosed; right→left: crossing. Release to erase; Esc cancels; Ctrl+Z restores the batch."),
        new("draw.line", "draw", EditorTool.Line, "Line · L", "Drag a line; choose dash, weight and endpoint markers"),
        new("draw.rounded", "draw", EditorTool.RoundedRectangle, "Rounded rectangle", "Drag; choose fill/gradient/opacity"),
        new("draw.ellipse", "draw", EditorTool.Ellipse, "Ellipse", "Drag an ellipse"),
        new("draw.circle", "draw", EditorTool.Circle, "Circle", "Drag; width and height stay equal"),
        new("draw.triangle", "draw", EditorTool.Triangle, "Triangle", "Drag a triangle"),
        new("draw.polygon", "draw", EditorTool.Polygon, "Polygon · click vertices", "Click vertices; Enter/double-click to close; Esc cancels"),
        new("draw.regular", "draw", EditorTool.RegularPolygon, "Regular polygon", "Set sides (3–32), then drag"),
        new("draw.polyline", "draw", EditorTool.Polyline, "Continuous line / loop", "Click points; Enter finishes; choose closed loop"),
        new("draw.freehand", "draw", EditorTool.Freehand, "Free form / pen", "Drag freely; enable closed loop and fill for a freeform plane"),
        new("draw.open-dot", "draw", EditorTool.OpenDot, "Open dot", "Click to place a ring; text size controls diameter"),
        new("draw.filled-dot", "draw", EditorTool.FilledDot, "Filled dot", "Click to place a dot; text size controls diameter"),
        new("draw.plane", "draw", EditorTool.Plane, "Solid / gradient plane", "Drag; set opacity and optional gradient"),
        new("label.callout", "label", EditorTool.Callout, "Step leader / callout", "Drag from step bubble to target; auto-increments"),
        new("label.note", "label", EditorTool.Note, "Note box", "Click to add an editable note"),
        new("review.cloud", "review", EditorTool.RevisionCloud, "Revision cloud · box", "Drag a scalloped revision cloud"),
        new("review.cloud-free", "review", EditorTool.CloudFreeform, "Revision cloud · polygon", "Click points; Enter finishes"),
        new("measure.dimension", "measure", EditorTool.Dimension, "Dimension label", "Drag endpoints then enter a manual label"),
        new("measure.calibrate", "measure", EditorTool.Calibrate, "Calibrate scale", "Drag a known length, then enter real length and unit"),
        new("measure.distance", "measure", EditorTool.Distance, "Distance", "Drag endpoints; labels use this image's calibration"),
        new("measure.length", "measure", EditorTool.Length, "Length / perimeter", "Click points; click the start to close a loop, or Enter for an open route. Scale recalculates all lengths."),
        new("measure.area", "measure", EditorTool.Area, "Area + perimeter", "Click polygon vertices; click the start or Enter to finish. Scale updates area and perimeter."),
        new("measure.angle", "measure", EditorTool.Angle, "Angle", "Click first point, vertex, then final point"),
        new("symbols.marker", "symbols", EditorTool.Symbol, "Drawing symbol", "Choose a symbol and label, then drag its bounds")
    };
    public static IReadOnlyList<string> Symbols { get; } = new[] { "north", "section", "elevation", "detail", "grid", "level", "centerline", "axis", "break", "slope", "check", "cross", "warning", "issue" };
    public static IReadOnlyList<StampDefinition> Stamps { get; } = new StampDefinition[]
    {
        new("rfi", "Issue", "RFI"), new("markup", "Review", "MARKUP"),
        new("coordination", "Review", "FOR COORDINATION"), new("review", "Review", "FOR REVIEW"),
        new("coordinate", "Review", "COORDINATE"), new("verify", "Review", "VERIFY"), new("check", "Review", "CHECK"),
        new("rfi-number", "Issue", "RFI-01"), new("tbc", "Issue", "TBC"), new("issue", "Issue", "ISSUE"), new("clash", "Issue", "CLASH"),
        new("missing", "Issue", "MISSING INFORMATION"),
        new("add", "Action", "ADD"), new("move", "Action", "MOVE"), new("delete", "Action", "DELETE"),
        new("revise", "Action", "REVISE"), new("align", "Action", "ALIGN"), new("match", "Action", "MATCH"), new("offset", "Action", "OFFSET"),
        new("nts", "Drawing", "NOT TO SCALE"), new("reference", "Drawing", "REFERENCE ONLY"),
        new("existing", "Drawing", "EXISTING"), new("proposed", "Drawing", "PROPOSED"), new("revision", "Drawing", "REV-01"),
        new("arch", "Discipline", "ARCH"), new("struct", "Discipline", "STRUCT"), new("mep", "Discipline", "MEP"),
        new("id", "Discipline", "ID"), new("landscape", "Discipline", "LANDSCAPE")
    };
    public static IReadOnlyList<StylePreset> Presets { get; } = new StylePreset[]
    {
        new("Red review", "#EF4444", 3, 24), new("Blue coordination", "#38BDF8", 3, 24),
        new("Green proposal", "#22C55E", 3, 24), new("Yellow highlight", "#FACC15", 3, 24),
        new("Technical mono", "#000000", 2, 18)
    };
    public static ToolDefinition Find(EditorTool tool) => Tools.Single(t => t.Tool == tool);
}
