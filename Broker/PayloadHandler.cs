using System.Text;
using Common;
using Newtonsoft.Json;

namespace Broker
{
    static class PayloadHandler
    {
        public static void Handle(byte[] bytes, ConnectionInfo conn, IMessageStore store)
        {
            var s = Encoding.UTF8.GetString(bytes);

            // abonare
            if (s.StartsWith("subscribe#", StringComparison.OrdinalIgnoreCase))
            {
                var topic = s.Substring("subscribe#".Length).Trim();
                if (!string.IsNullOrWhiteSpace(topic))
                {
                    conn.Topics.Add(topic);
                    ConnectionsStorage.AddOrUpdate(conn);
                }
                return;
            }

            // mesaj publicat: JSON sau XML
            Payload payload;
            if (s.TrimStart().StartsWith("<"))
                payload = XmlValidator.ParseAndValidate(s);    // XML + XSD ✔
            else
                payload = JsonConvert.DeserializeObject<Payload>(s)!; // JSON

            // persistă + pune în coada transientă
            store.Append(payload);          // PERSISTENT ✔
            PayloadStorage.Enqueue(payload); // TRANSIENT ✔
        }
    }
}
