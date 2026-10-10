using System.IO;
using System.Text.Json;

namespace SnapMark.Export;

// Cache recognition by file identity and language. A replaced image cannot keep
// matching text from its previous contents. Recognition is explicit, local and cancellable.
public static class HistoryTextIndex
{
    private sealed record Entry(long Length, long ModifiedTicks, string Language, string Text);
    public static string Cached(string path, string language)
    {
        try
        {
            var file = new FileInfo(path); var cache = new FileInfo(path + ".ocr.json");
            if (!file.Exists || !cache.Exists || cache.Length > 2_000_000) return "";
            var entry = JsonSerializer.Deserialize<Entry>(File.ReadAllText(cache.FullName));
            return entry is not null && entry.Length == file.Length && entry.ModifiedTicks == file.LastWriteTimeUtc.Ticks && entry.Language == language ? entry.Text ?? "" : "";
        }
        catch { return ""; }
    }
    public static async Task<string> RecognizeAsync(string path, string language, CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        var file = new FileInfo(path); var length = file.Length; var modified = file.LastWriteTimeUtc.Ticks;
        var image = await Task.Run(() => HistoryService.LoadImage(path), cancellation);
        var text = await LocalOcr.ReadAsync(image, language);
        cancellation.ThrowIfCancellationRequested(); file.Refresh();
        if (!file.Exists || file.Length != length || file.LastWriteTimeUtc.Ticks != modified) throw new IOException("Image changed during text recognition. Retry indexing.");
        Write(path, new Entry(length, modified, language, text));
        return text;
    }
    public static void WriteCache(string path, string language, string text)
    {
        var file = new FileInfo(path); Write(path, new Entry(file.Length, file.LastWriteTimeUtc.Ticks, language, text));
    }
    private static void Write(string path, Entry entry)
    {
        var destination = path + ".ocr.json"; var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(entry)); File.Move(temporary, destination, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
