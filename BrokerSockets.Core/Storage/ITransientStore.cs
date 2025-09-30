using System.Net;

namespace BrokerSockets.Core;

public readonly record struct OutboxItem(MessageEnvelope Envelope, IPEndPoint Target);

public interface ITransientStore
{
    void Enqueue(OutboxItem item);
    bool TryDequeue(out OutboxItem item);
}
