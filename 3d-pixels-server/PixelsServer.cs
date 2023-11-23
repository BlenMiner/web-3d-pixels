using NetCoreServer;
using System.Net.Sockets;
using System.Net;

namespace PixelsServer
{
    class PixelsServer : WsServer
    {
        readonly OAuthSecrets m_oauth;

        public PixelsServer(OAuthSecrets oauth, IPAddress address, int port) : base(address, port) 
        {
            m_oauth = oauth;
        }

        protected override TcpSession CreateSession() { return new PixelsServerSession(this, m_oauth); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket server caught an error with code {error}");
        }
    }
}