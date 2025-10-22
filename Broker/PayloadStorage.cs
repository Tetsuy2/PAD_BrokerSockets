using System.Collections.Concurrent;
using Common;

namespace Broker
{
    // TRANSIENT: coadă în memorie pentru livrare imediată
    static class PayloadStorage
    {
        private static readonly BlockingCollection<Payload> _q =
            new(new ConcurrentQueue<Payload>());

        // pune un mesaj în coada transientă
        public static void Enqueue(Payload p) => _q.Add(p);

        // enumeratează blocant până la Complete()
        public static IEnumerable<Payload> GetConsuming() => _q.GetConsumingEnumerable();

        // închide coada (la oprirea brokerului)
        public static void Complete() => _q.CompleteAdding();
    }
}
