using System.Net;
using BrokerSockets.Core;
using BrokerSockets.Sender;

static IPEndPoint ParseEndpoint(string s)
{
    var parts = s.Split(':', 2);
    var host = parts[0];
    var port = int.Parse(parts[1]);
    var ip = IPAddress.TryParse(host, out var ip1)
        ? ip1
        : System.Net.Dns.GetHostEntry(host).AddressList
            .First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
    return new IPEndPoint(ip, port);
}
static string Arg(string[] args, string key, string def = "")
{
    var i = Array.IndexOf(args, key);
    return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : def;
}

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run -- --broker 127.0.0.1:5001 --type T --subject S --payload \"JSON\"");
    return;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var brokerEp = ParseEndpoint(Arg(args, "--broker", "127.0.0.1:5001"));
var type     = Arg(args, "--type", "Test");
var subject  = Arg(args, "--subject", "demo.test");
var payload  = Arg(args, "--payload", "{\"hello\":\"world\"}");

await TcpSender.SendAsync(brokerEp, MessageEnvelope.Create(type, subject, payload), cts.Token);
