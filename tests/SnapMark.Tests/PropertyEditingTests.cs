using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SnapMark.Core;
using SnapMark.Editor;
using SnapMark.Export;

internal static class PropertyEditingTests
{
    public static void Run(Action<bool, string> check, string folder)
    {
        check(SymbolSequence.Label(SymbolLabelMode.Alphabet, 27, "", false) == "AA", "symbol alphabet continues after Z");
        check(SymbolSequence.Label(SymbolLabelMode.Numeric, 42, "", false) == "42", "symbol numeric sequence supports custom start");
        check(SymbolSequence.Label(SymbolLabelMode.Prefix, 3, "RFI-", false) == "RFI-3" && SymbolSequence.Label(SymbolLabelMode.Prefix, 2, "GRID-", true) == "GRID-B", "symbol prefix composes numeric or alphabet labels");
        var bytes = new byte[120 * 120 * 4]; for (var i = 3; i < bytes.Length; i += 4) bytes[i] = 255;
        var image = BitmapSource.Create(120, 120, 96, 96, PixelFormats.Bgra32, null, bytes, 480); image.Freeze();
        var surface = new EditorSurface(image); var rectangle = new Annotation(Guid.NewGuid(), AnnotationKind.Rectangle, new(10, 10), new(100, 100), Color: "#FF0000", Fill: ShapeFill.Solid, FillColor: "#0000FF", FillOpacity: .5, StrokeOpacity: 0);
        var line = new Annotation(Guid.NewGuid(), AnnotationKind.Line, new(0, 0), new(30, 30), Color: "#00FF00", Thickness: 8, LinePattern: LinePattern.Dot);
        surface.Document.Annotations.AddRange([rectangle, line]); surface.SelectObject(rectangle.Id); surface.SelectObject(line.Id, true);
        surface.ChangeSelectedProperty(a => a with { Color = "#FFFF00" }, AnnotationProperties.Color);
        check(surface.Document.Annotations[0].FillColor == "#0000FF" && surface.Document.Annotations[1].Thickness == 8 && surface.Document.Annotations[1].LinePattern == LinePattern.Dot, "multi selection color edit preserves independent fill, weight and pattern");
        surface.History.Undo(); check(surface.Document.Annotations[0] == rectangle && surface.Document.Annotations[1] == line, "one undo restores heterogeneous selection properties");
        surface.ChangeSelectedProperty(a => a with { FillOpacity = .2 }, AnnotationProperties.Fill);
        check(surface.Document.Annotations[0].FillOpacity == .2 && surface.Document.Annotations[1] == line, "fill edit applies only to objects with a fill property"); surface.History.Undo();
        var doc = new EditorDocument(120, 120); doc.Annotations.Add(rectangle); var rendered = new AnnotationRenderer().Render(image, doc); var sample = new byte[4]; rendered.CopyPixels(new Int32Rect(30, 30, 1, 1), sample, 4, 0);
        check(sample[0] is >= 120 and <= 135 && sample[2] == 0, "fill opacity renders independently of transparent red border");
        rendered.CopyPixels(new Int32Rect(10, 50, 1, 1), sample, 4, 0); check(sample[2] == 0, "zero border opacity removes stroke pixels without hiding fill");
        var project = Path.Combine(folder, "properties.snapmark"); ProjectStorage.Save(project, image, doc); check(ProjectStorage.Load(project).Document.Annotations.Single() == rectangle, "separate fill color and stroke opacity survive editable project storage");
        var symbolSurface = new EditorSurface(image) { Tool = EditorTool.Symbol, SymbolId = "grid", SymbolLabelMode = SymbolLabelMode.Prefix, SymbolPrefix = "D-", NextSymbol = 7, FontSize = 32 };
        var state = typeof(EditorSurface).GetProperty("State")!; var draft = typeof(EditorSurface).GetField("draft", BindingFlags.Instance | BindingFlags.NonPublic)!;
        state.SetValue(symbolSurface, ToolState.Drawing); draft.SetValue(symbolSurface, new Annotation(Guid.NewGuid(), AnnotationKind.Symbol, new(10, 10), new(50, 50), Text: "D-7", FontSize: 32, Variant: "grid")); symbolSurface.CompleteGesture(new(50, 50));
        check(symbolSurface.NextSymbol == 8 && symbolSurface.Document.Annotations.Single().FontSize == 32, "placed sequenced symbol increments counter and retains font size");
        symbolSurface.SelectObject(symbolSurface.Document.Annotations.Single().Id); symbolSurface.ChangeSelectedProperty(a => a with { FontSize = 48 }, AnnotationProperties.Font);
        check(symbolSurface.Document.Annotations.Single().FontSize == 48 && symbolSurface.Document.Annotations.Single().Text == "D-7" && symbolSurface.NextSymbol == 8, "existing symbol font edit does not renumber labels or change creation sequence");
        symbolSurface.History.Undo(); check(symbolSurface.Document.Annotations.Single().FontSize == 32, "symbol font size edit is undoable");
        symbolSurface.FillColor = "#123456"; symbolSurface.FillOpacity = .3; symbolSurface.StrokeOpacity = .8; symbolSurface.TextColor = "#00FF00";
        var presets = new PresetStore(Path.Combine(folder, "independent-style.json")); presets.Save(symbolSurface.SaveStyle("Properties")); var restored = new EditorSurface(image); restored.LoadStyle(presets.Load().Single());
        check(restored.FillColor == "#123456" && restored.FillOpacity == .3 && restored.StrokeOpacity == .8 && restored.TextColor == "#00FF00", "independent object properties survive saved style round trip");
    }
}
