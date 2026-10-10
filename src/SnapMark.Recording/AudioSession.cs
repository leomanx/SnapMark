using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace SnapMark.Recording;

public enum RecordingAudio { None, System, Microphone, Both }
public sealed record AudioEndpoint(string Id, string Name) { public override string ToString() => Name; }

// Capture files share one monotonic timeline. Silence between loopback packets is
// retained, so a sound played after a quiet interval does not move earlier in MP4.
public sealed class AudioSession : IDisposable
{
    private sealed class Track(WasapiCapture capture, MMDevice device, string path)
    {
        public WasapiCapture Capture = capture;
        public readonly TaskCompletionSource Finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Started;
        public MMDevice Device = device;
        public string Path = path;
        public WaveFileWriter Writer = new(path, capture.WaveFormat);
        public Exception? Error;
        public long Frames;
    }
    private readonly List<Track> tracks = [];
    private readonly Stopwatch clock = new();
    private bool stopped;
    private bool stopping;
    public double ElapsedSeconds => clock.Elapsed.TotalSeconds;
    public IReadOnlyList<string> Files => tracks.Select(t => t.Path).ToArray();
    public static IReadOnlyList<AudioEndpoint> Devices(bool microphone)
    {
        using var enumerator = new MMDeviceEnumerator();
        return enumerator.EnumerateAudioEndPoints(microphone ? DataFlow.Capture : DataFlow.Render, DeviceState.Active)
            .Select(d => { using (d) return new AudioEndpoint(d.ID, d.FriendlyName); }).ToArray();
    }
    public static string DefaultDeviceId(bool microphone)
    {
        using var enumerator = new MMDeviceEnumerator(); using var device = enumerator.GetDefaultAudioEndpoint(microphone ? DataFlow.Capture : DataFlow.Render, Role.Console); return device.ID;
    }
    public void Start(RecordingAudio mode, string prefix, string? systemId, string? microphoneId)
    {
        using var enumerator = new MMDeviceEnumerator();
        void Add(bool loopback, string? id)
        {
            var device = id is null ? enumerator.GetDefaultAudioEndpoint(loopback ? DataFlow.Render : DataFlow.Capture, Role.Console) : enumerator.GetDevice(id);
            WasapiCapture capture;
            try { capture = loopback ? new WasapiLoopbackCapture(device) : new WasapiCapture(device); }
            catch (Exception ex) { var name = device.FriendlyName; device.Dispose(); throw new IOException($"{(loopback ? "System output" : "Microphone")} '{name}' could not open its Windows audio format (0x{ex.HResult:X8}). {ex.Message}", ex); }
            var track = new Track(capture, device, prefix + (loopback ? ".system.wav" : ".microphone.wav")); tracks.Add(track);
            capture.RecordingStopped += (_, e) => { if (e.Exception is not null) track.Error = e.Exception; else if (!stopping) track.Error = new IOException("Audio device stopped unexpectedly."); track.Finished.TrySetResult(); };
            capture.DataAvailable += (_, e) =>
            {
                if (e.BytesRecorded == 0) return;
                try
                {
                    var format = capture.WaveFormat;
                    var frames = e.BytesRecorded / format.BlockAlign;
                    var expectedStart = Math.Max(0, (long)(clock.Elapsed.TotalSeconds * format.SampleRate) - frames);
                    // Allow the capture buffer's normal scheduling jitter; fill substantial gaps.
                    if (expectedStart - track.Frames > format.SampleRate / 5) WriteSilence(track, expectedStart - track.Frames);
                    track.Writer.Write(e.Buffer, 0, e.BytesRecorded); track.Frames += frames;
                }
                catch (Exception ex) { track.Error = ex; capture.StopRecording(); }
            };
        }
        try
        {
            if (mode is RecordingAudio.System or RecordingAudio.Both) Add(true, systemId);
            if (mode is RecordingAudio.Microphone or RecordingAudio.Both) Add(false, microphoneId);
            clock.Start(); foreach (var track in tracks) { try { track.Capture.StartRecording(); track.Started = true; } catch (Exception ex) { throw new IOException($"Audio device '{track.Device.FriendlyName}' could not start (0x{ex.HResult:X8}). Reconnect or select another device. {ex.Message}", ex); } }
        }
        catch { Dispose(); throw; }
    }
    private static void WriteSilence(Track track, long frames)
    {
        var block = track.Capture.WaveFormat.BlockAlign;
        var silence = new byte[4096 * block];
        while (frames > 0) { var count = (int)Math.Min(frames, 4096); track.Writer.Write(silence, 0, count * block); track.Frames += count; frames -= count; }
    }
    public void Stop()
    {
        if (stopped) return;
        stopping = true;
        // Dispose joins NAudio's capture thread; construct on a worker to keep its
        // RecordingStopped callback off the WPF synchronization context.
        foreach (var track in tracks) track.Capture.StopRecording();
        foreach (var track in tracks)
        {
            if (track.Started && !track.Finished.Task.Wait(TimeSpan.FromSeconds(5))) throw new IOException("Audio device did not stop; temporary WAV files were retained.");
            track.Capture.Dispose();
            var target = (long)(clock.Elapsed.TotalSeconds * track.Writer.WaveFormat.SampleRate);
            if (target > track.Frames) WriteSilence(track, target - track.Frames);
            track.Writer.Dispose(); track.Device.Dispose();
        }
        clock.Stop(); stopped = true;
        if (tracks.Any(t => t.Error is not null)) throw new IOException("Audio capture failed; temporary WAV files were retained.", tracks.First(t => t.Error is not null).Error);
    }
    public void Dispose()
    {
        stopping = true;
        if (!stopped) foreach (var track in tracks) { track.Capture.StopRecording(); if (track.Started) track.Finished.Task.Wait(TimeSpan.FromSeconds(5)); track.Capture.Dispose(); track.Writer.Dispose(); track.Device.Dispose(); }
        tracks.Clear(); clock.Stop();
    }
}
