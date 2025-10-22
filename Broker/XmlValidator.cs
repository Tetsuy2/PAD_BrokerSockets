using System.Xml;
using System.Xml.Schema;
using Common;

namespace Broker
{
    public static class XmlValidator
    {
        // XSD minim pentru Payload
        private const string Xsd = @"
            <xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
              <xs:element name='payload'>
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name='topic' type='xs:string'/>
                    <xs:element name='message' type='xs:string'/>
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:schema>";

        public static Payload ParseAndValidate(string xmlContent)
        {
            var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
            using var xsdReader = XmlReader.Create(new StringReader(Xsd));
            settings.Schemas.Add(null, xsdReader);

            var errors = new List<string>();
            settings.ValidationEventHandler += (s, e) => errors.Add(e.Message);

            using var r = XmlReader.Create(new StringReader(xmlContent), settings);
            string topic = "", message = "";
            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.Element && r.Name == "topic")
                    topic = r.ReadElementContentAsString();
                if (r.NodeType == XmlNodeType.Element && r.Name == "message")
                    message = r.ReadElementContentAsString();
            }

            if (errors.Count > 0)
                throw new InvalidDataException("XML invalid: " + string.Join("; ", errors));

            return new Payload { Topic = topic, Message = message };
        }
    }
}
