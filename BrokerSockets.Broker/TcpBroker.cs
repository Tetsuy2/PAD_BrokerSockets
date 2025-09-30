using System.Net;
using System.Net.Sockets;
using System.Text;
using BrokerSockets.Core;
using static BrokerSockets.Core.JsonCodec;
using static BrokerSockets.Core.EnvelopeValidator;

namespace BrokerSockets.Broker;

public sealed class TcpBroker
{
    private readonly int _port;
    private readonly Router _router;
    private readonly ITransientStore? _store;

    public TcpBroker(int port, Router router, ITransientStore? store = null)
    { _port = port; _router = router; _store = store; }

    public async Task RunAsync(CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        Console.WriteLine($"[Broker/TCP] listening {_port} (queue={_store is not null})");

        var retryTask = _store is null ? Task.CompletedTask : Task.Run(() => RetryLoop(ct), ct);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
        }
        catch (OperationCanceledException) { }
        finally { listener.Stop(); await retryTask; }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var c = client;
        using var stream = c.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        while (!ct.IsCancellationRequested && (line = await reader.ReadLineAsync()) is not null)
        {
            if (!TryDeserialize(line, out var env) || env is null) { Console.WriteLine("[Broker/TCP] invalid json"); continue; }
            if (!IsValid(env, out var reason)) { Console.WriteLine($"[Broker/TCP] rejected: {reason}"); continue; }

            var targets = _router.ResolveTargets(env).ToArray();
            if (targets.Length == 0) { Console.WriteLine($"[Broker/TCP] no subscribers for '{env.Subject}'"); continue; }

            foreach (var ep in targets) await TryDeliverAsync(env, ep, ct);
        }
    }

    private async Task TryDeliverAsync(MessageEnvelope env, IPEndPoint ep, CancellationToken ct)
    {
        try
        {
            using var rc = new TcpClient();
            await rc.ConnectAsync(ep.Address, ep.Port, ct);
            using var s = rc.GetStream();
            await s.WriteAsync(Serialize(env), ct);
            await s.FlushAsync(ct);
            Console.WriteLine($"[Broker/TCP] {env.Type}/{env.Subject} -> {ep}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Broker/TCP] delivery failed -> {ep} :: {ex.Message}");
            _store?.Enqueue(new OutboxItem(env, ep));
        }
    }

    private async Task RetryLoop(CancellationToken ct)
    {
        var delayMs = 1000;
        while (!ct.IsCancellationRequested)
        {
            if (_store is null) return;
            if (_store.TryDequeue(out var item))
            {
                try
                {
                    using var rc = new TcpClient();
                    await rc.ConnectAsync(item.Target.Address, item.Target.Port, ct);
                    using var s = rc.GetStream();
                    await s.WriteAsync(Serialize(item.Envelope), ct);
                    await s.FlushAsync(ct);
                    Console.WriteLine($"[Broker/TCP][retry] {item.Envelope.Subject} -> {item.Target}");
                    delayMs = 1000;
                }
                catch
                {
                    _store.Enqueue(item);
                    delayMs = Math.Min(delayMs + 1000, 5000);
                    await Task.Delay(delayMs, ct);
                }
            }
            else await Task.Delay(500, ct);
        }
    }
}
