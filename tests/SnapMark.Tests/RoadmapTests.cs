using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using SnapMark.Capture;
using SnapMark.Core;
using SnapMark.Editor;
using SnapMark.Export;
using SnapMark.Recording;

internal static class RoadmapTests
{
    public static void Run(Action<bool, string> check, string folder)
    {
        using (var worker = new AudioWorker()) { worker.Start(new(RecordingAudio.None, Path.Combine(folder, "worker-check"), null, null)).GetAwaiter().GetResult(); check(worker.Files.Count == 0, "isolated audio worker readiness protocol starts without activating devices"); worker.Stop().GetAwaiter().GetResult(); check(worker.ElapsedSeconds >= 0, "isolated audio worker finalizes and exits on STOP"); }
        using (var worker = new AudioWorker()) { var rejectedDevice = false; try { worker.Start(new(RecordingAudio.System, Path.Combine(folder, "worker-invalid"), "missing-device", null)).GetAwaiter().GetResult(); } catch (IOException) { rejectedDevice = true; } check(rejectedDevice, "audio worker reports unavailable device without blocking calling process"); }
        GestureRegressionTests.Run(check);
        TeamWorkflowTests.Run(check, folder);
        PropertyEditingTests.Run(check, folder);
        var screen = new PixelRect(-1920, 0, 3840, 1080);
        check(SelectionGeometry.Adjust(new(-1920, 0, 200, 120), screen, -10, -10, false) == new PixelRect(-1920, 0, 200, 120), "keyboard selection clamps at negative monitor origin");
        check(SelectionGeometry.Adjust(new(1700, 900, 200, 120), screen, 1000, 1000, true) == new PixelRect(1700, 900, 220, 180), "keyboard selection resize stays within desktop");
        var request = new RecordingRequest(new(0, 0, 3840, 2160), "out.mp4", MaximumHeight: 1080);
        check(RecordingService.OutputSize(request) == (1920, 1080), "4K capture scales to 1920 by 1080 MP4");
        check(RecordingService.OutputSize(request with { Bounds = new(0, 0, 800, 600) }) == (800, 600), "video resolution never upscales smaller region");
        check(RecordingService.OutputSize(request with { Bounds = new(0, 0, 101, 99), MaximumHeight = 0 }) == (102, 100), "native odd MP4 dimensions add only one pixel padding");
        check(RecordingService.Arguments(request, "tmp.mp4").Contains("scale=1920:1080:flags=lanczos"), "encoder resolution uses aspect preserving scale");
        var gifArgs = VideoTools.Arguments("in.mp4", "out.gif", 0, 2, true, new GifOptions(24, 1280, -1));
        check(gifArgs.Any(a => a.Contains("fps=24,scale='min(1280,iw)'")) && gifArgs.Contains("-1"), "GIF frame rate, maximum width and single play options reach encoder");
        var mix = VideoTools.MuxArguments("video.mp4", new[] { "system.wav", "mic.wav" }, "mixed.mp4", .25);
        check(mix.Contains("copy") && mix.Any(a => a.Contains("amix=inputs=2")) && mix.Contains("-shortest") && mix.Contains("0.25"), "two-source audio mix retains video pixels and offsets audio preroll");
        check(OutputNaming.Create("{app}_{counter}_{kind}", "image", DateTime.Now, "Revit", 42) == "Revit_0042_image", "app/counter filename tokens");
        var image = BitmapSource.Create(120, 100, 96, 96, PixelFormats.Bgra32, null, Enumerable.Repeat((byte)255, 120 * 100 * 4).ToArray(), 480); image.Freeze();
        var masked = FreeformMask.Apply(image, new PixelPoint[] { new(0, 0), new(120, 0), new(0, 100) }); var maskPixels = new byte[120 * 100 * 4]; masked.CopyPixels(maskPixels, 480, 0);
        check(maskPixels[(10 * 120 + 10) * 4 + 3] == 255 && maskPixels[(90 * 120 + 110) * 4 + 3] == 0 && image.Format == PixelFormats.Bgra32, "freeform screenshot mask preserves transparent outside and original bitmap");
        var surface = new EditorSurface(image); surface.Document.Annotations.Add(new(Guid.NewGuid(), AnnotationKind.Redact, new(10, 10), new(50, 50)));
        check(surface.SampleColor(new(20, 20)) == "#000000" && surface.SampleColor(new(100, 80)) == "#FFFFFF", "eyedropper samples flattened visible redaction instead of original pixels");
        var text = new Annotation(Guid.NewGuid(), AnnotationKind.Text, new(2, 2), new(2, 2), Text: "Test", FontFamily: "Arial", Bold: true); surface.Document.Annotations.Add(text);
        var path = Path.Combine(folder, "font.snapmark"); ProjectStorage.Save(path, image, surface.Document);
        check(ProjectStorage.Load(path).Document.Annotations[^1].FontFamily == "Arial" && ProjectStorage.Load(path).Document.Annotations[^1].Bold, "editable document retains text family and bold");
        var left = new EditorDocument(120, 100); left.Annotations.Add(text with { FontSize = 16, Text = "Long line\nX" }); var right = new EditorDocument(120, 100); right.Annotations.Add(left.Annotations[0] with { TextAlign = TextAlign.Right });
        var leftPixels = new byte[120 * 100 * 4]; var rightPixels = new byte[leftPixels.Length]; new AnnotationRenderer().Render(image, left).CopyPixels(leftPixels, 480, 0); new AnnotationRenderer().Render(image, right).CopyPixels(rightPixels, 480, 0);
        check(!leftPixels.SequenceEqual(rightPixels) && AnnotationRenderer.GetBounds(left.Annotations[0]).Width == AnnotationRenderer.GetBounds(right.Annotations[0]).Width, "multiline text alignment changes rendered placement while retaining bounds");
        var store = new SettingsService(Path.Combine(folder, "profiles")); store.Save(new() { Profiles = [new("BIM", "Revit,acad", "#EF4444", "{app}_{counter}", "")], ActiveProfile = "BIM", OutputCounter = 42, HistoryDays = 7 });
        check(store.Load().Profiles.Single().Name == "BIM" && store.Load().OutputCounter == 42 && store.Load().HistoryDays == 7, "project profiles counter and age retention persist");
        var history = new HistoryService(Path.Combine(folder, "search-history"), new ExportService()); history.Add(image, 10, "Revit", "Level 01");
        check(history.Entries().Single().SourceApp == "Revit" && history.Entries().Single().WindowTitle == "Level 01", "capture history retains searchable source metadata");
        var entry = history.Entries().Single(); File.SetLastWriteTimeUtc(entry.Path, DateTime.UtcNow.AddDays(-10)); history.Add(image, 10, days: 7);
        check(!File.Exists(entry.Path) && !File.Exists(entry.Path + ".json"), "history age retention removes only owned expired capture and sidecar");
        var packageFolder = Path.Combine(folder, "package"); Directory.CreateDirectory(packageFolder); var payload = Path.Combine(packageFolder, "SnapMark.exe"); File.WriteAllText(payload, "synthetic installer payload");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(payload)));
        File.WriteAllText(Path.Combine(packageFolder, "release-manifest.json"), JsonSerializer.Serialize(new PackageManifest("0.5.0", [new("SnapMark.exe", hash)])));
        var installed = PackageInstaller.Install(packageFolder, Path.Combine(folder, "installed"));
        check(File.ReadAllText(Path.Combine(installed, "SnapMark.exe")) == "synthetic installer payload" && !File.Exists(Path.Combine(installed, "portable.flag")), "GUI installer backend verifies and stages versioned package without portable mode");
        check(PackageInstaller.Install(packageFolder, Path.Combine(folder, "installed")) == installed, "same local version install is idempotent");
        File.WriteAllText(payload, "corrupted payload"); var rejected = false; try { PackageInstaller.Verify(packageFolder); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "GUI installer rejects altered package bytes");
        File.WriteAllText(Path.Combine(packageFolder, "release-manifest.json"), JsonSerializer.Serialize(new PackageManifest("0.5.0", [new("../SnapMark.exe", hash)])));
        rejected = false; try { PackageInstaller.Verify(packageFolder); } catch (InvalidDataException) { rejected = true; }
        check(rejected, "GUI installer rejects traversal before reading external payload");
        var corruptHistory = Path.Combine(history.FolderPath, "Capture_20000101_000000_000_" + Guid.NewGuid().ToString("N") + ".png"); File.WriteAllText(corruptHistory, "broken image");
        check(history.Entries().Count == 1, "corrupt owned history image does not block valid history entries");
        var hugeRejected = false; try { NativeDesktop.Snapshot(new(0, 0, 16000, 16000)); } catch (InvalidOperationException) { hugeRejected = true; }
        check(hugeRejected, "oversized capture is rejected before allocating bitmap memory");
        var locked = Path.Combine(folder, "locked.png"); File.WriteAllText(locked, "original");
        using (var handle = new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None)) { var lockRejected = false; try { ImageFormats.Save(image, locked); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { lockRejected = true; } check(lockRejected, "locked image destination reports failure instead of replacing existing data"); }
        check(File.ReadAllText(locked) == "original" && !Directory.EnumerateFiles(folder, "locked.png.*.tmp").Any(), "failed atomic image export preserves original bytes and removes temporary output");
    }
    public static void AudioSmoke(Action<bool, string> check, string folder)
    {
        Console.WriteLine("WASAPI output endpoints: " + AudioSession.Devices(false).Count + "; microphone endpoints: " + AudioSession.Devices(true).Count);
        var encoder = RecordingService.FindEncoder()!;
        var tone = Path.Combine(folder, "loopback-tone.wav"); Execute(encoder, "-v", "error", "-f", "lavfi", "-i", "sine=frequency=880:sample_rate=48000", "-t", "2", tone);
        if (AudioSession.Devices(false).Count == 0 || AudioSession.Devices(true).Count == 0) throw new IOException("Audio hardware probe unavailable: Windows reports no active system/microphone endpoints.");
        var audioMode = Enum.TryParse<RecordingAudio>(Environment.GetEnvironmentVariable("SNAPMARK_TEST_AUDIO_MODE"), out var selectedMode) ? selectedMode : RecordingAudio.Both;
        var outputId = Environment.GetEnvironmentVariable("SNAPMARK_TEST_SYSTEM") ?? AudioSession.DefaultDeviceId(false);
        var micId = Environment.GetEnvironmentVariable("SNAPMARK_TEST_MIC") ?? AudioSession.DefaultDeviceId(true);
        using var session = new AudioWorker(); session.Start(new(audioMode, Path.Combine(folder, "actual-audio"), outputId, micId)).GetAwaiter().GetResult();
        Console.WriteLine("Audio capture started; playing loopback tone.");
        Thread.Sleep(350);
        if (audioMode != RecordingAudio.Microphone) using (var reader = new WaveFileReader(tone)) using (var player = new WasapiOut(new MMDeviceEnumerator().GetDevice(outputId), AudioClientShareMode.Shared, true, 100)) { player.Init(reader); player.Play(); Thread.Sleep(2300); }
        Console.WriteLine("Stopping simultaneous audio capture.");
        session.Stop().GetAwaiter().GetResult();
        check(session.Files.Count == (audioMode == RecordingAudio.Both ? 2 : 1) && session.Files.All(f => new FileInfo(f).Length > 10000), $"real WASAPI {audioMode} writes audio frames");
        if (audioMode != RecordingAudio.Microphone) using (var reader = new WaveFileReader(session.Files[0]))
        { var samples = new byte[(int)reader.Length]; reader.Read(samples, 0, samples.Length); check(samples.Any(b => b != 0), "real loopback captures played test tone after initial silence"); }
        var monitor = NativeDesktop.Monitors.First(); var recorder = new RecordingService();
        recorder.StartAsync(new(new(monitor.X, monitor.Y, 65, 63), Path.Combine(folder, "both-audio.mp4"), 10, false, Audio: audioMode, SystemDeviceId: outputId, MicrophoneDeviceId: micId)).GetAwaiter().GetResult();
        Thread.Sleep(1000); var mp4 = recorder.StopAsync().GetAwaiter().GetResult();
        Execute(encoder, "-v", "error", "-i", mp4, "-f", "null", "NUL");
        var probe = Path.Combine(Path.GetDirectoryName(encoder)!, "ffprobe.exe");
        using var metadata = JsonDocument.Parse(Execute(probe, "-v", "error", "-show_entries", "stream=codec_type,width,height", "-of", "json", mp4));
        var streams = metadata.RootElement.GetProperty("streams").EnumerateArray().ToArray();
        check(streams.Any(s => s.GetProperty("codec_type").GetString() == "audio") && streams.Any(s => s.GetProperty("codec_type").GetString() == "video" && s.GetProperty("width").GetInt32() == 66 && s.GetProperty("height").GetInt32() == 64), $"real {audioMode} recording produces decodable AAC plus MP4 at exact padded pixel size");
    }
    public static void PluginSmoke(Action<bool, string> check, string folder, string assembly)
    {
        var catalog = new SnapMark.App.PluginCatalog(); catalog.Load(assembly); catalog.Load(assembly);
        check(catalog.Exporters.Count == 1, "exporter SDK loads actual external assembly once");
        var image = System.Windows.Media.Imaging.BitmapSource.Create(3, 2, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, Enumerable.Repeat((byte)255, 24).ToArray(), 12);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
        using var encoded = new MemoryStream(); encoder.Save(encoded); var data = encoded.ToArray();
        var path = Path.Combine(folder, "plugin-output.png"); catalog.Exporters.Single().Export(new(data, 3, 2), path);
        check(File.ReadAllBytes(path).SequenceEqual(data), "loaded sample exporter receives only supplied flattened bytes");
        var decoded = HistoryService.LoadImage(path); check(decoded.PixelWidth == 3 && decoded.PixelHeight == 2, "external exporter output is a decodable PNG with expected dimensions");
    }
    private static string Execute(string executable, params string[] args)
    {
        var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true }; foreach (var arg in args) info.ArgumentList.Add(arg);
        using var process = Process.Start(info)!; var error = process.StandardError.ReadToEndAsync(); var output = process.StandardOutput.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45)); try { process.WaitForExitAsync(timeout.Token).GetAwaiter().GetResult(); } catch { process.Kill(true); throw; }
        if (process.ExitCode != 0) throw new IOException(error.GetAwaiter().GetResult()); return output.GetAwaiter().GetResult();
    }
}
