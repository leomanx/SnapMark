using System.Windows.Media;
using Microsoft.Win32;
namespace SnapMark.App;
public static class ThemeService
{
    public static void Apply(string theme)
    {
        var light = theme == "Light";
        if (theme == "System") { using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"); light = key?.GetValue("AppsUseLightTheme") is int value && value != 0; }
        var resources = System.Windows.Application.Current.Resources;
        var colors = light ? new[] { "#F7F9FC", "#E6ECF4", "#172539", "#526477", "#D9E2ED", "#E1E7EF" } : new[] { "#101827", "#182235", "#E5EDF7", "#A5B4C8", "#26364D", "#202936" };
        var names = new[] { "WindowBrush", "PanelBrush", "TextBrush", "SecondaryBrush", "ButtonBrush", "CanvasBrush" };
        for (var i = 0; i < names.Length; i++) resources[names[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i]));
        resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(light ? "#006C8B" : "#67D9F5"));
    }
}
