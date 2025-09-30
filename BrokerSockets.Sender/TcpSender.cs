using System.Net;
using System.Net.Sockets;
using BrokerSockets.Core;
using static BrokerSockets.Core.JsonCodec;

namespace BrokerSockets.Sender;

public static class TcpSender
{
    public static async Task SendAsync(IPEndPoint broker, MessageEnvelope env, CancellationToken ct)
    {
        using var c = new TcpClient();
        await c.ConnectAsync(broker.Address, broker.Port, ct);
        using var s = c.GetStream();
        await s.WriteAsync(Serialize(env), ct);
        await s.FlushAsync(ct);
        Console.WriteLine($"[Sender/TCP] sent {env.Type}/{env.Subject} to {broker}");
    }
}
