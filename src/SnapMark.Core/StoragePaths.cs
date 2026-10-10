namespace SnapMark.Core;

public static class StoragePaths
{
    public static string History(AppSettings settings, string dataFolder) => Resolve(settings.HistoryFolder, Path.Combine(dataFolder, "history"));
    public static string Recovery(AppSettings settings, string dataFolder) => Resolve(settings.RecoveryFolder, Path.Combine(dataFolder, "recovery"));
    private static string Resolve(string? configured, string fallback) => Path.GetFullPath(string.IsNullOrWhiteSpace(configured) ? fallback : configured);
    public static string Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value.Trim())) throw new ArgumentException("Enter a full folder path, for example D:\\Captures.");
        var path = Path.GetFullPath(value.Trim()); if (File.Exists(path)) throw new ArgumentException("A folder path points to a file: " + path); return path;
    }
    public static string DataSetting(string value, string defaultPath) => Path.TrimEndingDirectorySeparator(value).Equals(Path.TrimEndingDirectorySeparator(defaultPath), StringComparison.OrdinalIgnoreCase) ? "" : value;
}
