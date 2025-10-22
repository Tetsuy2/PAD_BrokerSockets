using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using Common;
using Newtonsoft.Json;

namespace Broker
{
    class MessageWorker
    {
        private readonly UdpClient _udp;
        private readonly IPEndPoint _mcastEndp;

        public MessageWorker()
        {
            _udp = new UdpClient(AddressFamily.InterNetwork);

            // allow reuse & bind to an ephemeral port for sending
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));

            // local testing: see your own multicast traffic
            _udp.MulticastLoopback = Settings.UDP_LOOPBACK;
            _udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, Settings.MULTICAST_TTL);

            // choose an outgoing IPv4 interface (non-loopback if possible)
            var outIf = PickLocalIPv4() ?? IPAddress.Loopback;
            _udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, outIf.GetAddressBytes());

            _mcastEndp = new IPEndPoint(IPAddress.Parse(Settings.MULTICAST_GROUP), Settings.MULTICAST_PORT);
        }

        public void Run()
        {
            foreach (var p in PayloadStorage.GetConsuming())
            {
                // TCP unicast to topic subscribers
                var json = JsonConvert.SerializeObject(p);
                var data = Encoding.UTF8.GetBytes(json);

                foreach (var c in ConnectionsStorage.ByTopic(p.Topic))
                {
                    try { c.Socket.Send(data); }
                    catch { /* ignore broken client; cleaned elsewhere */ }
                }

                // UDP multicast (broadcast to the group)
                try { _udp.Send(data, data.Length, _mcastEndp); }
                catch { /* non-fatal for multicast */ }
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
            return null; // fallback to loopback in ctor
        }
    }
}
