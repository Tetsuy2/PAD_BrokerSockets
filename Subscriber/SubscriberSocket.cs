using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;

namespace Subscriber
{
    class SubscriberSocket
    {
        private Socket? _tcp;
        private readonly byte[] _buf = new byte[4096];
        private UdpClient? _udp;

        public void Connect(string ip, int port)
        {
            try
            {
                _tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _tcp.Connect(IPAddress.Parse(ip), port);
                Console.WriteLine("TCP connected.");
                _tcp.BeginReceive(_buf, 0, _buf.Length, SocketFlags.None, ReceiveCb, null);
            }
            catch (Exception ex) { Console.WriteLine("TCP connect error: " + ex.Message); }
        }

        public void Subscribe(string topic)
        {
            try
            {
                if (_tcp == null || !_tcp.Connected) { Console.WriteLine("Not connected."); return; }
                var s = "subscribe#" + topic;
                _tcp.Send(Encoding.UTF8.GetBytes(s));
                Console.WriteLine($"[TCP] Subscribed to '{topic}'");
            }
            catch (Exception ex) { Console.WriteLine("Subscribe error: " + ex.Message); }
        }

        private void ReceiveCb(IAsyncResult ar)
        {
            try
            {
                if (_tcp == null) return;
                var n = _tcp.EndReceive(ar);
                if (n <= 0) { Console.WriteLine("Server closed."); return; }

                var s = Encoding.UTF8.GetString(_buf, 0, n);
                Console.WriteLine($"[TCP] {s}");
                _tcp.BeginReceive(_buf, 0, _buf.Length, SocketFlags.None, ReceiveCb, null);
            }
            catch (Exception ex) { Console.WriteLine("TCP receive error: " + ex.Message); }
        }

        public void StartMulticast(string group, int port)
        {
            try
            {
                var mcast = IPAddress.Parse(group);

                // Pick a good local IPv4 (non-loopback if possible), else fall back to loopback
                var localIp = PickLocalIPv4() ?? IPAddress.Loopback;

                _udp = new UdpClient(AddressFamily.InterNetwork);
                _udp.ExclusiveAddressUse = false;
                _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Listen on all interfaces, on the multicast port
                _udp.Client.Bind(new IPEndPoint(IPAddress.Any, port));

                // Join the multicast group on the chosen interface
                _udp.JoinMulticastGroup(mcast, localIp);
                Console.WriteLine($"[UDP] Joined {group}:{port} via {localIp}");

                _ = Task.Run(async () =>
                {
                    while (true)
                    {
                        var result = await _udp.ReceiveAsync();
                        var s = Encoding.UTF8.GetString(result.Buffer);
                        Console.WriteLine($"[UDP] {s}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("UDP error: " + ex.Message);
            }
        }

        private static IPAddress? PickLocalIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (!ni.SupportsMulticast) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var ip = ni.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;
                if (ip != null) return ip;
            }
            return null; // caller will fall back to loopback
        }

        public void Close()
        {
            try { _tcp?.Shutdown(SocketShutdown.Both); } catch { }
            try { _tcp?.Close(); } catch { }
            try { _udp?.Close(); } catch { }
        }
    }
}
