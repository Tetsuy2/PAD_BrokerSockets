using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using BrokerSockets.Core;
using static BrokerSockets.Core.JsonCodec;

namespace BrokerSockets.Receiver;

public sealed class TcpReceiver
{
    private readonly int _port;
    private readonly string? _inboxDir;

    public TcpReceiver(int port, string? inboxDir = null)
    {
        _port = port;
        _inboxDir = string.IsNullOrWhiteSpace(inboxDir) ? null : inboxDir;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        Console.WriteLine($"[Receiver/TCP] listening {_port}");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
        }
        catch (OperationCanceledException) { }
        finally { listener.Stop(); }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var c = client;
        using var stream = c.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        while (!ct.IsCancellationRequested && (line = await reader.ReadLineAsync()) is not null)
        {
            if (TryDeserialize(line, out var env) && env is not null)
            {
                Console.WriteLine($"[Receiver/TCP] {env.Type}/{env.Subject} -> {env.Payload}");

                if (_inboxDir is not null)
                {
                    Directory.CreateDirectory(_inboxDir);
                    var path = Path.Combine(_inboxDir, $"{env.Subject}.jsonl");
                    await File.AppendAllTextAsync(path,
                        JsonSerializer.Serialize(env) + Environment.NewLine, ct);
                }
            }
        }
    }
}
