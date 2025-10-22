using System.Net;
using System.Net.Sockets;

namespace Publisher
{
    class PublisherSocket
    {
        private Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public bool IsConnected { get; private set; }

        public void Connect(string ip, int port)
        {
            try
            {
                _socket.Connect(IPAddress.Parse(ip), port);
                IsConnected = _socket.Connected;
                Console.WriteLine(IsConnected ? "Connected to broker." : "Connect failed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connect error: " + ex.Message);
                IsConnected = false;
            }
        }

        public void Send(byte[] data)
        {
            try { _socket.Send(data); }
            catch (Exception ex) { Console.WriteLine("Send error: " + ex.Message); }
        }
    }
}
