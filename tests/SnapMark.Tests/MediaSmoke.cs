using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;
using SnapMark.Recording;

internal static class MediaSmoke
{
    public static void Run(Action<bool, string> check, string folder)
    {
        var encoder = RecordingService.FindEncoder() ?? throw new InvalidOperationException("FFmpeg is required for --recording checks.");
        var source = Path.Combine(folder, "tone-source.mp4");
        Execute(encoder, "-hide_banner", "-loglevel", "error", "-f", "lavfi", "-i", "testsrc2=size=160x120:rate=15", "-f", "lavfi", "-i", "sine=frequency=440:sample_rate=44100", "-t", "2", "-c:v", "libx264", "-pix_fmt", "yuv420p", "-c:a", "aac", source);
        var original = File.ReadAllBytes(source); var trimmed = Path.Combine(folder, "trim.mp4"); var gif = Path.Combine(folder, "clip.gif");
        VideoTools.Export(source, trimmed, .25, 1.25, false).GetAwaiter().GetResult();
        VideoTools.Export(source, gif, 0, 1, true).GetAwaiter().GetResult();
        Execute(encoder, "-hide_banner", "-loglevel", "error", "-i", trimmed, "-f", "null", "NUL");
        check(File.ReadAllBytes(source).SequenceEqual(original), "video trim/GIF preserve original source bytes");
        using (var stream = File.OpenRead(gif)) check(new GifBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames.Count >= 10, "GIF export contains animated decoded frames");
        var probe = Path.Combine(Path.GetDirectoryName(encoder)!, "ffprobe.exe");
        if (File.Exists(probe)) { using var metadata = JsonDocument.Parse(Execute(probe, "-v", "error", "-show_entries", "format=duration:stream=codec_type", "-of", "json", trimmed)); var duration = double.Parse(metadata.RootElement.GetProperty("format").GetProperty("duration").GetString()!, System.Globalization.CultureInfo.InvariantCulture); check(duration >= .9 && duration < 1.2 && metadata.RootElement.GetProperty("streams").EnumerateArray().Any(s => s.GetProperty("codec_type").GetString() == "audio"), "trimmed MP4 has requested duration and encoded audio"); }
        try { Console.WriteLine("Available legacy DirectShow audio inputs: " + VideoTools.AudioDevices().GetAwaiter().GetResult().Count); } catch (OperationCanceledException) { Console.WriteLine("UNAVAILABLE: legacy DirectShow enumeration timed out; hardware probe remains open."); }
    }
    private static string Execute(string executable, params string[] arguments)
    {
        var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true }; foreach (var argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info)!; var error = process.StandardError.ReadToEndAsync(); var output = process.StandardOutput.ReadToEndAsync();
        if (!process.WaitForExit(30000)) { process.Kill(true); process.WaitForExit(); throw new IOException("Media test timed out."); }
        var detail = error.GetAwaiter().GetResult(); var result = output.GetAwaiter().GetResult(); if (process.ExitCode != 0) throw new IOException(detail); return result;
    }
}
