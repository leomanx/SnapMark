using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using SnapMark.Core;

namespace SnapMark.App;
public sealed class PluginCatalog
{
    public List<ISnapMarkExporter> Exporters { get; } = [];
    private readonly HashSet<string> loaded = new(StringComparer.OrdinalIgnoreCase);
    public void Load(string path)
    {
        path = Path.GetFullPath(path); if (loaded.Contains(path)) return;
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        var exporters = assembly.GetExportedTypes().Where(t => !t.IsAbstract && typeof(ISnapMarkExporter).IsAssignableFrom(t))
            .Select(t => (ISnapMarkExporter)(Activator.CreateInstance(t) ?? throw new InvalidDataException("Plugin requires a public parameterless constructor."))).ToArray();
        if (exporters.Length == 0) throw new InvalidDataException("No ISnapMarkExporter v1 implementation found.");
        if (Exporters.Count + exporters.Length > 50) throw new InvalidDataException("Maximum 50 exporters.");
        foreach (var exporter in exporters) if (string.IsNullOrWhiteSpace(exporter.Name) || exporter.Name.Length > 80 || !Regex.IsMatch(exporter.Extension, @"^\.[a-zA-Z0-9]{1,12}$")) throw new InvalidDataException("Invalid exporter name/extension.");
        Exporters.AddRange(exporters); loaded.Add(path);
    }
}
