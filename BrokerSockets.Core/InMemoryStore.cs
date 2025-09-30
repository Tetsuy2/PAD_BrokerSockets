using System.Collections.Concurrent;

namespace BrokerSockets.Core;

public sealed class InMemoryStore : ITransientStore
{
    private readonly ConcurrentQueue<OutboxItem> _q = new();
    public void Enqueue(OutboxItem item) => _q.Enqueue(item);
    public bool TryDequeue(out OutboxItem item) => _q.TryDequeue(out item);
}
