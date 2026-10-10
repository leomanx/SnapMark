namespace SnapMark.Core;

public static class TeamFolder
{
    public static string ValidateLink(string value, bool google)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length > 0 || !uri.IsDefaultPort) throw new ArgumentException("Enter an HTTPS link to the provider's shared folder.");
        var host = uri.IdnHost.ToLowerInvariant();
        var allowed = google ? host == "drive.google.com" : host is "onedrive.live.com" or "1drv.ms" || host.EndsWith(".sharepoint.com", StringComparison.Ordinal) || host == "onedrive.com";
        if (!allowed) throw new ArgumentException("The shared-folder link must belong to the selected provider.");
        return uri.AbsoluteUri;
    }
    public static string NewFile(string folder, bool project)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) throw new DirectoryNotFoundException("Browse to an existing synced team folder first. Ensure Drive/OneDrive is signed in.");
        return Path.Combine(Path.GetFullPath(folder), $"SnapMark_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{Guid.NewGuid():N}" + (project ? ".snapmark" : ".png"));
    }
    public static async Task<string> CopyFileAsync(string source, string folder, CancellationToken cancellation = default)
    {
        var extension = Path.GetExtension(source).ToLowerInvariant();
        if (!new[] { ".png", ".jpg", ".jpeg", ".mp4", ".pdf", ".snapmark" }.Contains(extension)) throw new ArgumentException("Choose an image, MP4, PDF or SnapMark project.");
        var destination = Path.ChangeExtension(NewFile(folder, false), extension); var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            { await input.CopyToAsync(output, cancellation); await output.FlushAsync(cancellation); }
            cancellation.ThrowIfCancellationRequested(); File.Move(temporary, destination, false); return destination;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
