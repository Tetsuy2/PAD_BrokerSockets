using System.Net;
using System.Net.Sockets;
using System.Text;

// Usage: groupIp port "message" [--unicast ip:port]
var group = args.Length > 0 ? args[0] : "239.0.0.1";
var port  = args.Length > 1 && int.TryParse(args[1], out var p) ? p : 5002;
var msg   = args.Length > 2 ? args[2] : "hello-udp";

var unicastArg = args.FirstOrDefault(a => a.StartsWith("--unicast", StringComparison.OrdinalIgnoreCase));
var unicast = unicastArg?.Split('=',2).ElementAtOrDefault(1);

using var udp = new UdpClient();

if (!string.IsNullOrEmpty(unicast))
{
    var parts = unicast.Split(':',2);
    var ip = IPAddress.Parse(parts[0]);
    var po = int.Parse(parts[1]);
    await udp.SendAsync(Encoding.UTF8.GetBytes(msg), new IPEndPoint(ip, po));
    Console.WriteLine($"[UDP/Pub] unicast -> {ip}:{po} : {msg}");
}
else
{
    await udp.SendAsync(Encoding.UTF8.GetBytes(msg), new IPEndPoint(IPAddress.Parse(group), port));
    Console.WriteLine($"[UDP/Pub] multicast -> {group}:{port} : {msg}");
}
