using System.Collections.Concurrent;
using System.Net;

namespace BrokerSockets.Core;

public sealed class Router
{
    private readonly IPEndPoint? _knownReceiver;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _subs = new();

    public Router(IPEndPoint? knownReceiver = null) => _knownReceiver = knownReceiver;

    public void Subscribe(string subject, IPEndPoint endpoint)
    {
        var bag = _subs.GetOrAdd(subject, _ => new ConcurrentDictionary<string, byte>());
        bag.TryAdd(endpoint.ToString(), 1);
    }

    public void Unsubscribe(string subject, IPEndPoint endpoint)
    {
        if (_subs.TryGetValue(subject, out var bag))
            bag.TryRemove(endpoint.ToString(), out _);
    }

    public IEnumerable<IPEndPoint> ResolveTargets(MessageEnvelope env)
    {
        if (_knownReceiver is not null) { yield return _knownReceiver; yield break; }
        if (_subs.TryGetValue(env.Subject, out var bag))
            foreach (var key in bag.Keys)
                if (TryParse(key, out var ep)) yield return ep!;
    }

    private static bool TryParse(string key, out IPEndPoint? ep)
    {
        ep = null;
        var parts = key.Split(':', 2);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var port)) return false;
        ep = new IPEndPoint(IPAddress.Parse(parts[0]), port); return true;
    }
}
