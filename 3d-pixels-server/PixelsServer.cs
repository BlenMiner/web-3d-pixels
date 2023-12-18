using NetCoreServer;
using System.Net.Sockets;
using System.Net;

namespace PixelsServer
{
    class PixelsServer : WsServer
    {
        readonly OAuthSecrets m_oauth;

        readonly Database m_database;

        readonly RessourcesCache m_cache;

        public PixelsServer(RessourcesCache cache, Database db, OAuthSecrets oauth, IPAddress address, int port) : base(address, port) 
        {
            m_oauth = oauth;
            m_database = db;
            m_cache = cache;
        }

        protected override TcpSession CreateSession() { return new PixelsServerSession(this, m_cache, m_database, m_oauth); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket server caught an error with code {error}");
        }
    }
}