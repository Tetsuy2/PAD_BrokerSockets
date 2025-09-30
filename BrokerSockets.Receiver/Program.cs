using BrokerSockets.Receiver;

static string Arg(string[] args, string key, string def = "")
{
    var i = Array.IndexOf(args, key);
    return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : def;
}
static int ArgI(string[] args, string key, int def = 0)
    => int.TryParse(Arg(args, key), out var v) ? v : def;

var port = ArgI(args, "--port", 6001);
var inbox = Arg(args, "--inbox", ""); // optional path to append JSONL

Console.WriteLine("Usage: dotnet run -- --port 6001 [--inbox data\\inbox]");
await new TcpReceiver(port, inbox).RunAsync(CancellationToken.None);
