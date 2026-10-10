using System.Diagnostics;
using System.Text.Json;

namespace SnapMark.Recording;

public sealed record AudioWorkerReady(string[] Files, double Elapsed);
public sealed record AudioWorkerRequest(RecordingAudio Mode, string Prefix, string? SystemId, string? MicrophoneId);

// Native audio drivers can block inside IAudioClient.Initialize. Isolate that call
// so a stalled endpoint never prevents the editor from continuing or exiting.
public sealed class AudioWorker : IDisposable
{
    private Process? process;
    private double initialSeconds;
    private readonly Stopwatch clock = new();
    public double ElapsedSeconds => initialSeconds + clock.Elapsed.TotalSeconds;
    public IReadOnlyList<string> Files { get; private set; } = [];
    public static string Executable { get; set; } = Path.Combine(AppContext.BaseDirectory, "SnapMark.exe");
    public async Task Start(AudioWorkerRequest request)
    {
        var info = new ProcessStartInfo(Executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add("--audio-worker"); info.ArgumentList.Add(JsonSerializer.Serialize(request));
        process = Process.Start(info) ?? throw new IOException("Could not start audio worker.");
        try
        {
            var ready = await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(12)).ConfigureAwait(false);
            if (ready is null || !ready.StartsWith("READY ")) throw new IOException((ready ?? "Audio device did not start.") + "\n" + await process.StandardError.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false));
            var data = JsonSerializer.Deserialize<AudioWorkerReady>(ready[6..]) ?? throw new IOException("Invalid audio readiness."); initialSeconds = data.Elapsed; clock.Start(); Files = data.Files;
        }
        catch (TimeoutException) { Dispose(); throw new IOException("Audio driver did not respond within 12 seconds. Choose another system/microphone device or No audio."); }
        catch { Dispose(); throw; }
    }
    public async Task Stop()
    {
        if (process is null) return;
        try
        {
            await process.StandardInput.WriteLineAsync("STOP").ConfigureAwait(false); await process.StandardInput.FlushAsync().ConfigureAwait(false);
            var result = await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)).ConfigureAwait(false);
            if (result != "SAVED" || process.ExitCode != 0) throw new IOException(result ?? "Audio worker stopped unexpectedly; WAV files were retained.");
        }
        catch (TimeoutException) { throw new IOException("Audio driver did not stop in time; temporary video/WAV files were retained."); }
        finally { Dispose(); }
    }
    public void Dispose()
    {
        if (process is not null) { if (!process.HasExited) process.Kill(true); process.Dispose(); process = null; } clock.Stop();
    }
    public static int Run(string json)
    {
        try
        {
            var request = JsonSerializer.Deserialize<AudioWorkerRequest>(json) ?? throw new ArgumentException("Invalid audio request.");
            SynchronizationContext.SetSynchronizationContext(null);
            using var session = new AudioSession(); session.Start(request.Mode, request.Prefix, request.SystemId, request.MicrophoneId);
            Console.WriteLine("READY " + JsonSerializer.Serialize(new AudioWorkerReady(session.Files.ToArray(), session.ElapsedSeconds))); Console.Out.Flush();
            _ = Console.ReadLine(); // STOP or EOF: finalize and release devices.
            session.Stop(); Console.WriteLine("SAVED"); Console.Out.Flush(); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); Console.WriteLine("ERROR " + ex.Message.Replace('\r', ' ').Replace('\n', ' ')); Console.Out.Flush(); return 1; }
    }
}
