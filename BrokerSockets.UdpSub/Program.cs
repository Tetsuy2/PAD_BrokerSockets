using System.Net;
using System.Net.Sockets;
using System.Text;

var group = args.Length > 0 ? args[0] : "239.0.0.1";
var port  = args.Length > 1 && int.TryParse(args[1], out var p) ? p : 5002;

using var udp = new UdpClient(port, AddressFamily.InterNetwork);
udp.JoinMulticastGroup(IPAddress.Parse(group));
Console.WriteLine($"[UDP/Sub] joined {group}:{port}");

while (true)
{
    var res = await udp.ReceiveAsync();
    var text = Encoding.UTF8.GetString(res.Buffer);
    Console.WriteLine($"[UDP/Sub] {res.RemoteEndPoint} -> {text}");
}
