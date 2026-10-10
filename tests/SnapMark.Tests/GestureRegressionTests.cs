using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;
using SnapMark.Editor;

internal static class GestureRegressionTests
{
    public static void Run(Action<bool, string> check)
    {
        var image = BitmapSource.Create(120, 100, 96, 96, PixelFormats.Bgra32, null, new byte[48000], 480); image.Freeze();
        var surface = new EditorSurface(image);
        void Begin(EditorTool tool, AnnotationKind kind)
        {
            surface.Tool = tool;
            // Supply the exact state left by dragging, without OS input in unit tests.
            typeof(EditorSurface).GetProperty(nameof(EditorSurface.State))!.SetValue(surface, ToolState.Drawing);
            typeof(EditorSurface).GetField("draft", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(surface, new Annotation(Guid.NewGuid(), kind, new(10, 10), new(110, 10)));
        }
        var idleBeforePrompt = false;
        surface.RequestText = _ => { idleBeforePrompt = surface.State == ToolState.Idle && !surface.IsMouseCaptured; surface.CancelGesture(); surface.CompleteGesture(new(110, 10)); return "3500 mm"; };
        Begin(EditorTool.Dimension, AnnotationKind.Dimension); surface.CompleteGesture(new(110, 10));
        check(idleBeforePrompt && surface.Document.Annotations.Single().Text == "3500 mm", "dimension modal releases gesture before prompt and survives lost capture/reentrant mouse-up");
        surface.History.Undo(); check(surface.Document.Annotations.Count == 0, "dimension modal commit stays a single undo operation");
        surface.RequestText = _ => null; Begin(EditorTool.Dimension, AnnotationKind.Dimension); surface.CompleteGesture(new(110, 10));
        check(surface.Document.Annotations.Count == 0 && surface.State == ToolState.Idle, "cancel dimension prompt leaves no draft or captured gesture");
        surface.RequestCalibration = _ => { idleBeforePrompt = surface.State == ToolState.Idle && !surface.IsMouseCaptured; surface.CancelGesture(); return (3500, "mm"); };
        Begin(EditorTool.Calibrate, AnnotationKind.Line); surface.CompleteGesture(new(110, 10));
        check(idleBeforePrompt && surface.Document.UnitsPerPixel == 35 && surface.Document.Unit == "mm", "calibration survives modal capture loss and converts 100px reference to 3500mm");
        surface.History.Undo(); check(surface.Document.UnitsPerPixel == 1 && surface.Document.Unit == "px", "calibration undo restores previous scale and unit");
        surface.RequestCalibration = _ => null; Begin(EditorTool.Calibrate, AnnotationKind.Line); surface.CompleteGesture(new(110, 10));
        check(surface.Document.Unit == "px" && surface.State == ToolState.Idle, "cancel calibration preserves scale and ends gesture");
        foreach (var unit in new[] { "mm", "cm", "m", "in", "ft" }) check(CalibrationInput.TryRead("3500.5", unit, 100, out var length) && length == 3500.5, "calibration accepts finite length in " + unit);
        check(!CalibrationInput.TryRead("NaN", "mm", 100, out _) && !CalibrationInput.TryRead("Infinity", "mm", 100, out _) && !CalibrationInput.TryRead("0", "mm", 100, out _) && !CalibrationInput.TryRead("3500", "", 100, out _), "calibration rejects non-finite zero and missing units");
    }
}
