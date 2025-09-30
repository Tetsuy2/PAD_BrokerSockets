namespace BrokerSockets.Core;

public static class EnvelopeValidator
{
    public static bool IsValid(MessageEnvelope e, out string? error)
    {
        if (string.IsNullOrWhiteSpace(e.Type))    { error = "Type required."; return false; }
        if (string.IsNullOrWhiteSpace(e.Subject)) { error = "Subject required."; return false; }
        error = null; return true;
    }
}
