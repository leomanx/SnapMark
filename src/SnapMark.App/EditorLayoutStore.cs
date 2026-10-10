using System.IO;
using System.Text.Json;

namespace SnapMark.App;

public sealed class EditorLayoutStore
{
    private readonly string? path;
    public EditorLayoutStore() { }
    public EditorLayoutStore(string path) { this.path = path; }
    public double ToolWidth { get; set; } = 256;
    public double ToolHeight { get; set; } = 240;
    public Dictionary<string, List<string>> Orders { get; set; } = [];
    public static EditorLayoutStore Load(string path)
    {
        var result = new EditorLayoutStore(path);
        try
        {
            var saved = JsonSerializer.Deserialize<EditorLayoutStore>(File.ReadAllText(path));
            if (saved is not null) { result.ToolWidth = double.IsFinite(saved.ToolWidth) ? Math.Clamp(saved.ToolWidth, 220, 600) : 256; result.ToolHeight = double.IsFinite(saved.ToolHeight) ? Math.Clamp(saved.ToolHeight, 100, 1600) : 240; result.Orders = saved.Orders ?? []; }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { }
        return result;
    }
    public void Save()
    {
        if (path is null) return;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this)); File.Move(temp, path, true);
    }
    public IEnumerable<string> Ordered(string group, IEnumerable<string> ids)
    {
        var available = ids.ToArray();
        return Orders.GetValueOrDefault(group, []).Where(available.Contains).Concat(available).Distinct();
    }
}
