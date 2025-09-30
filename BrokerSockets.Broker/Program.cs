using System.Net;
using BrokerSockets.Core;
using BrokerSockets.Broker;

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
static int ArgI(string[] args, string key, int def = 0)
    => int.TryParse(Arg(args, key), out var v) ? v : def;
static bool Has(string[] args, string key) => Array.IndexOf(args, key) >= 0;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var port = ArgI(args, "--port", 5001);
var receiverOpt = Arg(args, "--receiver", "");
var enableQueue = Has(args, "--enable-queue");

IPEndPoint? knownReceiver = null;
if (!string.IsNullOrWhiteSpace(receiverOpt))
    knownReceiver = ParseEndpoint(receiverOpt);

var router = new Router(knownReceiver);
ITransientStore? store = enableQueue ? new InMemoryStore() : null;

Console.WriteLine("Usage: dotnet run -- --port 5001 [--receiver 127.0.0.1:6001] [--enable-queue]");
await new TcpBroker(port, router, store).RunAsync(cts.Token);
