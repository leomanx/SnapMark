using System.Diagnostics;
using SnapMark.Core;

namespace SnapMark.Recording;

public sealed record RecordingRequest(PixelRect Bounds, string Path, int FramesPerSecond = 15, bool ShowCursor = true, string? AudioDevice = null,
    RecordingAudio Audio = RecordingAudio.None, string? SystemDeviceId = null, string? MicrophoneDeviceId = null, int MaximumHeight = 0, int Quality = 23);
public interface IRecordingService : IAsyncDisposable
{
    bool IsRecording { get; }
    Task StartAsync(RecordingRequest request);
    Task<string> StopAsync();
}
public sealed class RecordingService : IRecordingService
{
    private Process? process;
    private Task? outputPump, errorPump;
    private readonly Queue<string> errors = new();
    private string? temporary, destination;
    private readonly SemaphoreSlim stopGate = new(1, 1);
    private string? lastSaved;
    private AudioWorker? audio;
    private double audioPreroll;
    public bool IsRecording => process is { HasExited: false };
    public static string EncoderPath { get; set; } = "";
    public static string? FindEncoder()
    {
        // An explicit preference must not silently fall back to another executable.
        if (!string.IsNullOrWhiteSpace(EncoderPath))
            return Path.IsPathFullyQualified(EncoderPath) && Path.GetFileName(EncoderPath).Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase) && File.Exists(EncoderPath) ? EncoderPath : null;
        var candidates = new[] { Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe"), Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg.exe") }
            .Concat((Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator).Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => Path.Combine(p.Trim('"'), "ffmpeg.exe")));
        return candidates.FirstOrDefault(File.Exists);
    }
    public static IReadOnlyList<string> Arguments(RecordingRequest request, string output)
    {
        if (request.Bounds.Width < 2 || request.Bounds.Height < 2 || request.Bounds.Width > 16000 || request.Bounds.Height > 16000) throw new ArgumentException("Recording bounds must be 2–16000 pixels per side.");
        if (!double.IsFinite(request.Bounds.X) || !double.IsFinite(request.Bounds.Y) || !double.IsFinite(request.Bounds.Width) || !double.IsFinite(request.Bounds.Height) || request.Bounds.X % 1 != 0 || request.Bounds.Y % 1 != 0 || request.Bounds.Width % 1 != 0 || request.Bounds.Height % 1 != 0 || request.Bounds.Width * request.Bounds.Height > 64_000_000) throw new ArgumentException("Recording requires whole physical pixels within 64 MP.");
        if (request.FramesPerSecond is not (10 or 15 or 24 or 30)) throw new ArgumentException("Supported frame rates: 10, 15, 24, 30.");
        _ = OutputSize(request);
        if (request.Quality is < 16 or > 30) throw new ArgumentException("Video quality must be CRF 16–30.");
        if (request.Audio != RecordingAudio.None && request.AudioDevice is not null) throw new ArgumentException("Choose WASAPI or legacy DirectShow audio, not both.");
        var arguments = new List<string> { "-hide_banner", "-loglevel", "warning", "-n", "-f", "gdigrab", "-framerate", request.FramesPerSecond.ToString(),
            "-draw_mouse", request.ShowCursor ? "1" : "0", "-offset_x", ((int)request.Bounds.X).ToString(), "-offset_y", ((int)request.Bounds.Y).ToString(),
            "-video_size", $"{(int)request.Bounds.Width}x{(int)request.Bounds.Height}", "-i", "desktop" };
        if (string.IsNullOrWhiteSpace(request.AudioDevice)) arguments.Add("-an");
        else { if (request.AudioDevice.Length > 1000 || request.AudioDevice.Contains('\n')) throw new ArgumentException("Invalid audio device."); arguments.AddRange(new[] { "-thread_queue_size", "512", "-f", "dshow", "-i", "audio=" + request.AudioDevice, "-map", "0:v:0", "-map", "1:a:0", "-c:a", "aac", "-b:a", "128k", "-af", "aresample=async=1:first_pts=0" }); }
        var size = OutputSize(request);
        var filter = request.MaximumHeight > 0 && request.Bounds.Height > request.MaximumHeight ? $"scale={size.Width}:{size.Height}:flags=lanczos" : "pad=ceil(iw/2)*2:ceil(ih/2)*2";
        arguments.AddRange(new[] { "-vf", filter, "-c:v", "libx264", "-preset", "ultrafast", "-crf", request.Quality.ToString(), "-pix_fmt", "yuv420p", "-movflags", "+faststart", "-progress", "pipe:1", "-nostats", output });
        return arguments;
    }
    public static (int Width, int Height) OutputSize(RecordingRequest request)
    {
        if (request.MaximumHeight is not (0 or 720 or 1080 or 1440 or 2160)) throw new ArgumentException("Choose Native, 720p, 1080p, 1440p or 2160p.");
        var ratio = request.MaximumHeight == 0 ? 1 : Math.Min(1, request.MaximumHeight / request.Bounds.Height);
        return ratio == 1 ? (((int)request.Bounds.Width + 1) / 2 * 2, ((int)request.Bounds.Height + 1) / 2 * 2)
            : (Math.Max(2, (int)(request.Bounds.Width * ratio) / 2 * 2), Math.Max(2, (int)(request.Bounds.Height * ratio) / 2 * 2));
    }
    public async Task StartAsync(RecordingRequest request)
    {
        if (process is not null) throw new InvalidOperationException("A recording is already active.");
        var encoder = FindEncoder() ?? throw new InvalidOperationException("FFmpeg is required for video. Choose ffmpeg.exe in Settings, or clear that preference to search beside SnapMark.exe, tools, and PATH. Still-image tools work without it.");
        destination = Path.GetFullPath(request.Path); Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        temporary = Path.Combine(Path.GetDirectoryName(destination)!, $".SnapMark-recording-{Guid.NewGuid():N}.mp4");
        _ = Arguments(request, temporary); // Validate before activating any audio device.
        if (request.Audio != RecordingAudio.None)
        {
            audio = new AudioWorker();
            try { await audio.Start(new(request.Audio, temporary, request.SystemDeviceId, request.MicrophoneDeviceId)).ConfigureAwait(false); }
            catch { audio.Dispose(); audio = null; throw; }
        }
        var info = new ProcessStartInfo(encoder) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in Arguments(request, temporary)) info.ArgumentList.Add(argument);
        errors.Clear(); var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lastSaved = null;
        try { audioPreroll = audio?.ElapsedSeconds ?? 0; process = Process.Start(info) ?? throw new InvalidOperationException("Could not start video encoder."); }
        catch { audio?.Dispose(); audio = null; throw; }
        var current = process;
        outputPump = Task.Run(async () => { while (await current.StandardOutput.ReadLineAsync() is { } line) if (line.StartsWith("frame=") && int.TryParse(line[6..], out var frames) && frames > 0) ready.TrySetResult(); });
        errorPump = Task.Run(async () => { while (await current.StandardError.ReadLineAsync() is { } line) { lock (errors) { if (errors.Count >= 20) errors.Dequeue(); errors.Enqueue(line); } } });
        var completed = await Task.WhenAny(ready.Task, current.WaitForExitAsync(), Task.Delay(10000)).ConfigureAwait(false);
        if (completed != ready.Task || current.HasExited)
        {
            var detail = "Encoder did not produce frames.";
            try { await StopAsync().ConfigureAwait(false); } catch (Exception ex) { detail = ex.Message; }
            throw new InvalidOperationException(detail);
        }
    }
    public async Task<string> StopAsync()
    {
        await stopGate.WaitAsync().ConfigureAwait(false);
        var current = process;
        if (current is null) { stopGate.Release(); return lastSaved ?? throw new InvalidOperationException("No recording is active."); }
        try
        {
            var audioFiles = audio?.Files ?? [];
            Exception? audioError = null;
            if (audio is not null) { try { await audio.Stop().ConfigureAwait(false); } catch (Exception ex) { audioError = ex; } finally { audio = null; } }
            if (!current.HasExited) { await current.StandardInput.WriteLineAsync("q").ConfigureAwait(false); await current.StandardInput.FlushAsync().ConfigureAwait(false); }
            var finished = current.WaitForExitAsync();
            if (await Task.WhenAny(finished, Task.Delay(15000)).ConfigureAwait(false) != finished) { current.Kill(true); await current.WaitForExitAsync().ConfigureAwait(false); throw new IOException("Encoder did not finalize in time. Temporary recording was retained at " + temporary); }
            if (outputPump is not null) await outputPump.ConfigureAwait(false); if (errorPump is not null) await errorPump.ConfigureAwait(false);
            if (current.ExitCode != 0 || temporary is null || !File.Exists(temporary) || new FileInfo(temporary).Length < 100)
            { string detail; lock (errors) detail = string.Join("\n", errors); throw new IOException("Video recording failed: " + detail); }
            if (audioError is not null) throw new IOException("Audio failed. Video and WAV recovery files remain at " + temporary, audioError);
            if (audioFiles.Count > 0)
            {
                var muxed = temporary + ".mixed.mp4";
                await VideoTools.MuxAudio(temporary, audioFiles, muxed, audioPreroll).ConfigureAwait(false);
                File.Move(muxed, destination!, true);
                File.Delete(temporary); foreach (var file in audioFiles) File.Delete(file);
            }
            else File.Move(temporary, destination!, true);
            lastSaved = destination; return destination!;
        }
        finally { current.Dispose(); process = null; stopGate.Release(); }
    }
    public async ValueTask DisposeAsync() { if (process is not null) await StopAsync().ConfigureAwait(false); }
}
