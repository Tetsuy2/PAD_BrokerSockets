using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace BrokerSockets.Core;

public static class XmlCodec
{
    private static readonly XNamespace Ns = "urn:broker:envelope:v1";

    // Generate XML (DOM) in schema namespace
    public static string ToXml(MessageEnvelope e) =>
        new XElement(Ns + "Envelope",
            new XElement(Ns + "Type", e.Type),
            new XElement(Ns + "Subject", e.Subject),
            new XElement(Ns + "Payload", e.Payload),
            new XElement(Ns + "Timestamp", e.Timestamp.ToUniversalTime().ToString("O")),
            new XElement(Ns + "Id", e.Id.ToString())
        ).ToString(SaveOptions.DisableFormatting);

    // Validate against XSD (SAX) + extract via DOM
    public static bool TryParseAndValidate(string xml, string xsdPath, out MessageEnvelope? e, out string? error)
    {
        e = null; error = null;

        // 1) SAX/XSD validation
        string? validationError = null;
        var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
        settings.Schemas.Add("urn:broker:envelope:v1", xsdPath);
        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
        settings.ValidationEventHandler += (_, a) => validationError ??= a.Message;

        using (var sr = new StringReader(xml))
        using (var xr = XmlReader.Create(sr, settings))
        { while (xr.Read()) { /* validate */ } }

        if (validationError is not null) { error = validationError; return false; }

        // 2) DOM extraction
        var doc = XDocument.Parse(xml);
        XNamespace ns = "urn:broker:envelope:v1";
        var root = doc.Root;
        if (root is null || root.Name != ns + "Envelope")
        { error = "Invalid root element."; return false; }

        string? type = root.Element(ns + "Type")?.Value;
        string? subject = root.Element(ns + "Subject")?.Value;
        string? payload = root.Element(ns + "Payload")?.Value ?? "";
        string? ts = root.Element(ns + "Timestamp")?.Value;
        string? id = root.Element(ns + "Id")?.Value;

        if (!Guid.TryParse(id, out var gid)) { error = "Invalid GUID."; return false; }
        if (!DateTime.TryParseExact(ts, "O", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
        { error = "Invalid timestamp."; return false; }

        var env = new MessageEnvelope(
            type!, subject!, payload,
            DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            gid
        );

        if (!EnvelopeValidator.IsValid(env, out var reason)) { error = reason; return false; }
        e = env;
        return true;
    }
}
