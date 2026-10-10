using System.IO;
using Microsoft.Win32;
namespace SnapMark.App;
public static class LocalIntegration
{
    public static void CreateShortcut(string versionFolder)
    {
        var location = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "SnapMark.lnk");
        var type = Type.GetTypeFromProgID("WScript.Shell") ?? throw new IOException("Shortcut service unavailable.");
        dynamic shell = Activator.CreateInstance(type)!; dynamic shortcut = shell.CreateShortcut(location);
        try { if (File.Exists(location) && !((string)shortcut.TargetPath).StartsWith(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SnapMark") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("An unrelated SnapMark shortcut already exists; installed files were preserved.");
            shortcut.TargetPath = Path.Combine(versionFolder, "SnapMark.exe"); shortcut.IconLocation = shortcut.TargetPath + ",0"; shortcut.WorkingDirectory = versionFolder; shortcut.Save();
        } finally { System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut); System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell); }
    }
    public static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        var command = "\"" + Path.Combine(AppContext.BaseDirectory, "SnapMark.exe") + "\" --tray";
        if (enabled) key.SetValue("SnapMark", command);
        else if (key.GetValue("SnapMark") as string == command) key.DeleteValue("SnapMark", false);
    }
    public static void RegisterExplorer()
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\SystemFileAssociations\image\shell\SnapMark");
        key.SetValue("", "Edit with SnapMark"); key.SetValue("Icon", Path.Combine(AppContext.BaseDirectory, "SnapMark.exe"));
        using var command = key.CreateSubKey("command"); command.SetValue("", "\"" + Path.Combine(AppContext.BaseDirectory, "SnapMark.exe") + "\" --open \"%1\"");
    }
}
