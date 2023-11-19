using NetCoreServer;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace PixelsServer
{
    class PixelsServerSession : WsSession
    {
        private readonly string m_clientId;

        public PixelsServerSession(WsServer server, string oauthClientID) : base(server) 
        {
            m_clientId = oauthClientID;
        }

        public override void OnWsConnected(HttpRequest request)
        {
            Console.WriteLine($"Chat WebSocket session with Id {Id} connected!");

            // Send invite message
            string message = "Please send a message or '!' to disconnect the client!";

            SendBinaryAsync(message);
        }

        public override void OnWsDisconnected()
        {
            Console.WriteLine($"Chat WebSocket session with Id {Id} disconnected!");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            Console.WriteLine("Incoming: " + message);

            // Multicast message to all connected sessions
            ((WsServer)Server).MulticastBinary(buffer, offset, size);

            // If the buffer starts with '!' the disconnect the current session
            if (message == "!")
                Close(1000);
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket session caught an error with code {error}");
        }

        protected override void OnReceivedRequest(HttpRequest request)
        {
            var host = request.GetHeader("Host");
            if (host == null) return;

            var isLocalHost = host.StartsWith("localhost");
            var rootUrl = $"{(isLocalHost ? "http" : "https")}://{host}";

            var unescapedPathAndQuery = Uri.UnescapeDataString(request.Url).Split('?');

            string urlPath = unescapedPathAndQuery[0];
            string urlQuery = unescapedPathAndQuery.Length > 1 ? unescapedPathAndQuery[1] : string.Empty;

            switch (request.Method)
            {
                case "GET":

                    if (urlPath == "/login")
                    {
                        var redirectAt = $"{rootUrl}/oauth";
                        var oauthUrl = OAuth.GetOAuthUrl(m_clientId, redirectAt);
                        SendResponseAsync(Response.MakeRedirectResponse(oauthUrl));
                    }

                    if (urlPath == "/oauth")
                    {
                        // http://localhost:8080/oauth?code=4%2F0AfJohXkIxhgVtfvJOxEuC5xArAiMFMlM_g-GOw1mpmYh9vZGXLAVvhaCo6Y7E5IAs24bvg&scope=email+https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fuserinfo.email+openid&authuser=0&prompt=consent
                        SendResponseAsync(Response.MakeGetResponse("OAUTH page " + rootUrl, "text/html; charset=UTF-8"));
                    }

                    break;
            }
        }
    }

    class PixelsServer : WsServer
    {
        readonly string m_clientId;

        public PixelsServer(string oauth_clientId, IPAddress address, int port) : base(address, port) 
        {
            m_clientId = oauth_clientId;
        }

        protected override TcpSession CreateSession() { return new PixelsServerSession(this, m_clientId); }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Chat WebSocket server caught an error with code {error}");
        }
    }

    class Program
    {
        static string GetWWWPath(string currentDirectory = ".")
        {
            var directoryInfo = new DirectoryInfo(currentDirectory);

            if (directoryInfo.Name == "web-3d-pixels")
                return Path.Combine(directoryInfo.FullName, "www");

            var parent = directoryInfo.Parent;

            if (parent == null)
                return string.Empty;

            return GetWWWPath(parent.FullName);
        }

        static void Main(string[] args)
        {
            const int PORT = 8080;

            // WebSocket server port
            string oauthClientId = args.Length > 0 ? args[0] : "1046003701952-57on8uhpj7ba89afgo30ott3no9vgobj.apps.googleusercontent.com";

            Console.WriteLine($"WebSocket server port: {PORT}");
            Console.WriteLine($"WebSocket server website: http://localhost:{PORT}/index.html");

            Console.WriteLine();

            // Create a new WebSocket server
            var server = new PixelsServer(oauthClientId, IPAddress.Any, PORT);

            server.AddStaticContent(GetWWWPath());

            var homepage = server.Cache.Find("/index.html");
            if (homepage.Item1)
                server.Cache.Add("/", homepage.Item2);

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            while (true) Thread.Sleep(2000);
        }
    }
}