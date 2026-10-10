using System.IO;
using System.IO.Pipes;
using System.Text;
namespace SnapMark.App;
public sealed class InstanceMessages : IDisposable
{
    private readonly CancellationTokenSource cancel = new();
    public void Start(Action<string> received) => _ = Task.Run(async () =>
    {
        while (!cancel.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream("SnapMark.Open.v1", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancel.Token);
                var header = new byte[4]; await server.ReadExactlyAsync(header, cancel.Token); var length = BitConverter.ToInt32(header);
                if (length is < 1 or > 65536) continue;
                var bytes = new byte[length]; await server.ReadExactlyAsync(bytes, cancel.Token); received(Encoding.UTF8.GetString(bytes));
            }
            catch (OperationCanceledException) { return; }
            catch (IOException) { }
        }
    });
    public static bool Send(string message)
    {
        try { using var client = new NamedPipeClientStream(".", "SnapMark.Open.v1", PipeDirection.Out, PipeOptions.CurrentUserOnly); client.Connect(1000); var bytes = Encoding.UTF8.GetBytes(message); if (bytes.Length > 65536) return false; client.Write(BitConverter.GetBytes(bytes.Length)); client.Write(bytes); return true; } catch { return false; }
    }
    public void Dispose() { cancel.Cancel(); }
}
