using System.Net;
using System.Net.Sockets;
using Common;

namespace Broker
{
    class BrokerSocket
    {
        private readonly Socket _listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly IMessageStore _store;

        public BrokerSocket(IMessageStore store) { _store = store; }

        public void Start(string ip, int port)
        {
            _listener.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            _listener.Listen(128);
            _listener.BeginAccept(AcceptCb, null);
        }

        private void AcceptCb(IAsyncResult ar)
        {
            try
            {
                var sock = _listener.EndAccept(ar);
                var conn = new ConnectionInfo { Socket = sock, Address = sock.RemoteEndPoint?.ToString() ?? "unknown" };
                ConnectionsStorage.AddOrUpdate(conn);
                sock.BeginReceive(conn.Data, 0, conn.Data.Length, SocketFlags.None, ReceiveCb, conn);
            }
            catch { /* ignore */ }
            finally { _listener.BeginAccept(AcceptCb, null); }
        }

        private void ReceiveCb(IAsyncResult ar)
        {
            var conn = (ConnectionInfo)ar.AsyncState!;
            try
            {
                var n = conn.Socket.EndReceive(ar, out var se);
                if (n <= 0 || se != SocketError.Success) throw new SocketException();

                var block = new byte[n];
                Buffer.BlockCopy(conn.Data, 0, block, 0, n);
                PayloadHandler.Handle(block, conn, _store);
                conn.Socket.BeginReceive(conn.Data, 0, conn.Data.Length, SocketFlags.None, ReceiveCb, conn);
            }
            catch
            {
                try { conn.Socket.Shutdown(SocketShutdown.Both); } catch { }
                try { conn.Socket.Close(); } catch { }
                ConnectionsStorage.Remove(conn.Address);
            }
        }
    }
}
