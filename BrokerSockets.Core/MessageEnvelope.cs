namespace BrokerSockets.Core;

public sealed record MessageEnvelope(
    string Type,
    string Subject,
    string Payload,
    DateTime Timestamp,
    Guid Id
)
{
    public static MessageEnvelope Create(string type, string subject, string payload) =>
        new(type, subject, payload, DateTime.UtcNow, Guid.NewGuid());
}
