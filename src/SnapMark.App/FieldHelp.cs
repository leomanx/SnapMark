using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace SnapMark.App;

public static class FieldHelp
{
    public static void Label(Panel panel, string text) => panel.Children.Add(new TextBlock { Text = text, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 8, 4, 0) });
    public static void Hint(Panel panel, TextBox input, string label, string help)
    {
        input.ToolTip = label + "\n" + help;
        AutomationProperties.SetName(input, label); AutomationProperties.SetHelpText(input, help);
        var hint = new TextBlock { Text = help, FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 0, 4, 6) };
        hint.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryBrush"); panel.Children.Add(hint);
    }
    public static void Add(Panel panel, TextBox input, string label, string help) { Label(panel, label); panel.Children.Add(input); Hint(panel, input, label, help); }
}
