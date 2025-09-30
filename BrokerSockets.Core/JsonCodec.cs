using System.Text;
using System.Text.Json;

namespace BrokerSockets.Core;

public static class JsonCodec
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static byte[] Serialize(MessageEnvelope m) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(m, Opt) + "\n");

    public static bool TryDeserialize(string line, out MessageEnvelope? m)
    {
        try { m = JsonSerializer.Deserialize<MessageEnvelope>(line, Opt); return m is not null; }
        catch { m = null; return false; }
    }
}
