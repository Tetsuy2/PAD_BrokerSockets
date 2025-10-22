using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Common
{
    public class ConnectionInfo
    {
        public const int BUFFER_SIZE = 4096;
        public readonly byte[] Data = new byte[BUFFER_SIZE];

        public Socket Socket { get; set; } = default!;
        public string Address { get; set; } = "unknown";
        public HashSet<string> Topics { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
