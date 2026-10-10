using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace SnapMark.Export;
public sealed record PackageFile(string Path, string Sha256);
public sealed record PackageManifest(string Version, List<PackageFile> Files);
public static class PackageInstaller
{
    private static string Child(string root, string relative)
    {
        if (Path.IsPathRooted(relative) || relative.Split('/', '\\').Any(p => p is ".." or "" || p.Contains(':')) || relative.Equals("portable.flag", StringComparison.OrdinalIgnoreCase) || relative.Split('/', '\\')[0].Equals("data", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsafe package path: " + relative);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Package path escapes folder.");
        for (var current = path; current.Length >= root.Length; current = Path.GetDirectoryName(current)!)
        { if ((File.Exists(current) || Directory.Exists(current)) && File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint)) throw new InvalidDataException("Package links are not supported."); if (current == root) break; }
        return path;
    }
    public static PackageManifest Verify(string folder)
    {
        folder = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar);
        var manifestPath = Path.Combine(folder, "release-manifest.json"); if (new FileInfo(manifestPath).Length > 2_000_000) throw new InvalidDataException("Manifest exceeds 2 MB.");
        var manifest = JsonSerializer.Deserialize<PackageManifest>(File.ReadAllText(manifestPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Invalid manifest.");
        if (!Version.TryParse(manifest.Version, out _) || manifest.Files is null || manifest.Files.Count is < 1 or > 2000) throw new InvalidDataException("Invalid package version / file list.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.Files)
        {
            if (!names.Add(file.Path)) throw new InvalidDataException("Duplicate package path.");
            using var stream = File.OpenRead(Child(folder, file.Path)); var hash = Convert.ToHexString(SHA256.HashData(stream));
            if (!hash.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Package hash mismatch: " + file.Path);
        }
        if (!names.Contains("SnapMark.exe")) throw new InvalidDataException("Missing SnapMark.exe.");
        return manifest;
    }
    public static string Install(string source, string destination)
    {
        source = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar); destination = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar);
        var manifest = Verify(source); Directory.CreateDirectory(destination);
        var final = Child(destination, "version-" + manifest.Version);
        if (Directory.Exists(final)) { var existing = Verify(final); if (existing.Version != manifest.Version || !existing.Files.OrderBy(f => f.Path).SequenceEqual(manifest.Files.OrderBy(f => f.Path))) throw new IOException("Installed version differs. No files were overwritten."); return final; }
        var stage = Child(destination, "staging-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(stage);
        foreach (var file in manifest.Files) { var target = Child(stage, file.Path); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.Copy(Child(source, file.Path), target, false); }
        File.Copy(Path.Combine(source, "release-manifest.json"), Path.Combine(stage, "release-manifest.json"));
        Verify(stage); Directory.Move(stage, final); return final;
    }
}
