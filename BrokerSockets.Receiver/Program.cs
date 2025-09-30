using System.Net;
using System.Net.Sockets;
using System.Text;
using BrokerSockets.Core;
using static BrokerSockets.Core.JsonCodec;
using static BrokerSockets.Core.EnvelopeValidator;

static string Arg(string[] args, string key, string def = "")
{
    var i = Array.IndexOf(args, key);
    return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : def;
}
static int ArgI(string[] args, string key, int def = 0)
    => int.TryParse(Arg(args, key), out var v) ? v : def;
static bool Has(string[] args, string key) => Array.IndexOf(args, key) >= 0;

if (args.Length == 0) { Show(); return; }

var port = ArgI(args, "--port", 6001);
var inbox = Arg(args, "--inbox", "data\\inbox"); // director unde salvăm
var saveXml = Has(args, "--save-xml");
var xsdPath = Arg(args, "--xsd", "");             // calea către envelope.xsd (opțional)
Directory.CreateDirectory(inbox);

Console.WriteLine($"Usage: dotnet run -- --port 6001 [--inbox data\\inbox] [--save-xml] [--xsd C:\\path\\envelope.xsd]");
Console.WriteLine($"[Receiver/TCP] listening {port}");
var listener = new TcpListener(IPAddress.Any, port);
listener.Start();

try
{
    while (true)
    {
        var client = await listener.AcceptTcpClientAsync();
        _ = Task.Run(() => HandleClientAsync(client, inbox, saveXml, xsdPath));
    }
}
finally
{
    listener.Stop();
}

static async Task HandleClientAsync(TcpClient client, string inbox, bool saveXml, string xsdPath)
{
    using var c = client;
    using var stream = c.GetStream();
    using var reader = new StreamReader(stream, Encoding.UTF8);

    string? line;
    while ((line = await reader.ReadLineAsync()) is not null)
    {
        if (!TryDeserialize(line, out var env) || env is null)
        {
            Console.WriteLine("[Receiver/TCP] invalid json (ignored)");
            continue;
        }
        if (!IsValid(env, out var reason))
        {
            Console.WriteLine($"[Receiver/TCP] rejected: {reason}");
            continue;
        }

        // 1) Salvare JSONL (inbox.jsonl cumulativ)
        var jsonlPath = Path.Combine(inbox, "inbox.jsonl");
        await File.AppendAllTextAsync(jsonlPath,
            System.Text.Json.JsonSerializer.Serialize(env) + Environment.NewLine);

        // 2) (opțional) salvare XML + validare XSD
        if (saveXml)
        {
            var xml = XmlCodec.ToXml(env);
            // nume fișier prietenos
            var name = $"{env.Timestamp:yyyyMMdd_HHmmss}_{env.Id:N}.xml";
            var xmlPath = Path.Combine(inbox, name);
            await File.WriteAllTextAsync(xmlPath, xml, Encoding.UTF8);

            if (!string.IsNullOrWhiteSpace(xsdPath) && File.Exists(xsdPath))
            {
                if (XmlCodec.TryParseAndValidate(xml, xsdPath, out var parsed, out var err))
                    Console.WriteLine($"[Receiver/TCP] XML OK: {parsed!.Type}/{parsed.Subject}");
                else
                    Console.WriteLine($"[Receiver/TCP] XML INVALID: {err}");
            }
            else if (!string.IsNullOrWhiteSpace(xsdPath))
            {
                Console.WriteLine($"[Receiver/TCP] XSD not found: {xsdPath}");
            }
        }

        Console.WriteLine($"[Receiver/TCP] {env.Type}/{env.Subject} -> {env.Payload}");
    }
}

static void Show()
{
    Console.WriteLine("Usage: dotnet run -- --port 6001 [--inbox data\\inbox] [--save-xml] [--xsd C:\\path\\envelope.xsd]");
}
