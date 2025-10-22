using System.Text;
using Common;
using Newtonsoft.Json;

namespace Publisher
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Publisher...");
            var sock = new PublisherSocket();
            sock.Connect(Settings.BROKER_IP, Settings.BROKER_PORT);
            if (!sock.IsConnected) return;

            while (true)
            {
                Console.Write("Topic (q=quit): ");
                var topic = Console.ReadLine();
                if (string.Equals(topic, "q", StringComparison.OrdinalIgnoreCase)) break;

                Console.Write("Message (JSON/XML). Dacă începi cu '<', se trimite ca XML: ");
                var msg = Console.ReadLine() ?? "";

                string wire;
                if (msg.TrimStart().StartsWith("<")) // XML raw (broker validează)
                {
                    wire = msg;
                }
                else
                {
                    var p = new Payload { Topic = topic ?? "", Message = msg };
                    wire = JsonConvert.SerializeObject(p);
                }

                sock.Send(Encoding.UTF8.GetBytes(wire));
            }
        }
    }
}
