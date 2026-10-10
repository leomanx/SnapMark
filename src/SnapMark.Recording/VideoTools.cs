using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SnapMark.Recording;

public sealed record GifOptions(int FramesPerSecond = 12, int MaximumWidth = 960, int Loop = 0);

public static class VideoTools
{
    public static IReadOnlyList<string> MuxArguments(string video, IReadOnlyList<string> audio, string output, double preroll = 0)
    {
        if (audio.Count is < 1 or > 2) throw new ArgumentException("One or two audio tracks are required.");
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-nostdin", "-n", "-i", video };
        if (!double.IsFinite(preroll) || preroll < 0) throw new ArgumentException("Invalid audio timing.");
        foreach (var path in audio) args.AddRange(new[] { "-ss", preroll.ToString(CultureInfo.InvariantCulture), "-i", path });
        var filter = string.Join(";", Enumerable.Range(1, audio.Count).Select(i => $"[{i}:a]aresample=48000:async=1:first_pts=0,apad[a{i}]"));
        filter += ";" + string.Concat(Enumerable.Range(1, audio.Count).Select(i => $"[a{i}]")) + $"amix=inputs={audio.Count}:duration=longest:normalize=1[mix]";
        args.AddRange(new[] { "-filter_complex", filter, "-map", "0:v:0", "-map", "[mix]", "-c:v", "copy", "-c:a", "aac", "-b:a", "192k", "-ac", "2", "-shortest", "-movflags", "+faststart", output });
        return args;
    }
    public static async Task MuxAudio(string video, IReadOnlyList<string> audio, string output, double preroll = 0)
    {
        using var process = Process.Start(StartInfo(RecordingService.FindEncoder()!, MuxArguments(video, audio, output, preroll)))!;
        var error = process.StandardError.ReadToEndAsync(); var stdout = process.StandardOutput.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); } catch { if (!process.HasExited) process.Kill(true); throw; }
        await stdout.ConfigureAwait(false);
        if (process.ExitCode != 0) throw new IOException("Audio finalization failed. Video/WAV files were retained. " + await error.ConfigureAwait(false));
        await error.ConfigureAwait(false);
    }
    public static async Task<IReadOnlyList<string>> AudioDevices()
    {
        var encoder = RecordingService.FindEncoder(); if (encoder is null) return Array.Empty<string>();
        var info = StartInfo(encoder, new[] { "-hide_banner", "-list_devices", "true", "-f", "dshow", "-i", "dummy" });
        using var process = Process.Start(info)!; var error = process.StandardError.ReadToEndAsync(); var output = process.StandardOutput.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        try { await process.WaitForExitAsync(timeout.Token); } catch { if (!process.HasExited) process.Kill(true); await process.WaitForExitAsync(); throw; }
        await output; return Regex.Matches(await error, "\"([^\"]+)\" \\(audio\\)").Select(m => m.Groups[1].Value).Distinct().ToArray();
    }
    public static IReadOnlyList<string> Arguments(string input, string output, double start, double end, bool gif, GifOptions? options = null)
    {
        if (!double.IsFinite(start) || !double.IsFinite(end) || start < 0 || end <= start || end - start > 3600) throw new ArgumentException("Enter a valid start/end range, up to one hour.");
        if (string.Equals(Path.GetFullPath(input), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Choose a different output file; source video is preserved.");
        options ??= new();
        if (options.FramesPerSecond is < 1 or > 30 || options.MaximumWidth is < 64 or > 3840 || options.Loop is < -1 or > 100) throw new ArgumentException("Invalid GIF options.");
        var args = new List<string> { "-hide_banner", "-loglevel", "error", "-nostdin", "-n", "-ss", start.ToString(CultureInfo.InvariantCulture), "-i", input, "-t", (end - start).ToString(CultureInfo.InvariantCulture) };
        if (gif) args.AddRange(new[] { "-an", "-filter_complex", $"[0:v]fps={options.FramesPerSecond},scale='min({options.MaximumWidth},iw)':-1:flags=lanczos,split[a][b];[a]palettegen[p];[b][p]paletteuse", "-loop", options.Loop.ToString(CultureInfo.InvariantCulture) });
        else args.AddRange(new[] { "-map", "0:v:0", "-map", "0:a?", "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2", "-c:v", "libx264", "-preset", "fast", "-crf", "23", "-pix_fmt", "yuv420p", "-c:a", "aac", "-movflags", "+faststart" });
        args.Add(output); return args;
    }
    public static async Task Export(string input, string output, double start, double end, bool gif, CancellationToken token = default, GifOptions? options = null)
    {
        var encoder = RecordingService.FindEncoder() ?? throw new InvalidOperationException("FFmpeg is required.");
        if (!File.Exists(input)) throw new FileNotFoundException("Video source does not exist.");
        if (!Path.GetExtension(output).Equals(gif ? ".gif" : ".mp4", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException(gif ? "Choose a .gif output filename." : "Choose a .mp4 output filename.");
        _ = Arguments(input, output, start, end, gif, options);
        var destination = Path.GetFullPath(output); Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = Path.Combine(Path.GetDirectoryName(destination)!, ".SnapMark-video-" + Guid.NewGuid().ToString("N") + (gif ? ".gif" : ".mp4"));
        try
        {
            using var process = Process.Start(StartInfo(encoder, Arguments(input, temporary, start, end, gif, options))) ?? throw new IOException("Could not start FFmpeg.");
            var error = process.StandardError.ReadToEndAsync(); var stdout = process.StandardOutput.ReadToEndAsync();
            try { await process.WaitForExitAsync(token); } catch { if (!process.HasExited) process.Kill(true); await process.WaitForExitAsync(); throw; }
            await stdout; var detail = await error;
            if (process.ExitCode != 0 || !File.Exists(temporary) || new FileInfo(temporary).Length < 100) throw new IOException("Video export failed: " + detail);
            File.Move(temporary, destination, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static ProcessStartInfo StartInfo(string executable, IEnumerable<string> arguments)
    {
        var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) info.ArgumentList.Add(argument); return info;
    }
}
