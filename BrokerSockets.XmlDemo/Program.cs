using BrokerSockets.Core;

var env = MessageEnvelope.Create("Test", "xml.sample", "payload");
var xml = XmlCodec.ToXml(env);

// scriem XSD-ul lângă exe ca să fie ușor de rulat oriunde
var xsdPath = Path.Combine(AppContext.BaseDirectory, "envelope.xsd");
if (!File.Exists(xsdPath))
{
    File.WriteAllText(xsdPath, """
<?xml version="1.0" encoding="utf-8"?>
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
           targetNamespace="urn:broker:envelope:v1"
           xmlns="urn:broker:envelope:v1"
           elementFormDefault="qualified"
           attributeFormDefault="unqualified">
  <xs:element name="Envelope">
    <xs:complexType>
      <xs:sequence>
        <xs:element name="Type"      type="xs:string"/>
        <xs:element name="Subject"   type="xs:string"/>
        <xs:element name="Payload"   type="xs:string" minOccurs="0"/>
        <xs:element name="Timestamp" type="xs:dateTime"/>
        <xs:element name="Id"        type="xs:string"/>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>
""");
}

Console.WriteLine(xml);
if (XmlCodec.TryParseAndValidate(xml, xsdPath, out var parsed, out var err))
    Console.WriteLine($"XML valid. Parsed: {parsed!.Type}/{parsed.Subject}");
else
    Console.WriteLine($"Invalid XML: {err}");
