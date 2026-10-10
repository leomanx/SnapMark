namespace SnapMark.Core;

public enum SymbolLabelMode { Custom, Alphabet, Numeric, Prefix }
public static class SymbolSequence
{
    public static string Label(SymbolLabelMode mode, int number, string prefix, bool alphabetic, string custom = "") => mode switch
    {
        SymbolLabelMode.Alphabet => GeometryMath.StepLabel(number, true),
        SymbolLabelMode.Numeric => GeometryMath.StepLabel(number, false),
        SymbolLabelMode.Prefix => prefix + GeometryMath.StepLabel(number, alphabetic),
        _ => custom
    };
}
