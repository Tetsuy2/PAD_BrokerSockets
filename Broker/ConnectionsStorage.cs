using System.Collections.Concurrent;
using Common;

namespace Broker
{
    static class ConnectionsStorage
    {
        private static readonly ConcurrentDictionary<string, ConnectionInfo> _byAddr = new();

        public static void AddOrUpdate(ConnectionInfo c) => _byAddr[c.Address] = c;

        public static void Remove(string address) => _byAddr.TryRemove(address, out _);

        public static IReadOnlyList<ConnectionInfo> ByTopic(string topic) =>
            _byAddr.Values.Where(c => c.Topics.Contains(topic)).ToList();

        public static IEnumerable<ConnectionInfo> All => _byAddr.Values;
    }
}
