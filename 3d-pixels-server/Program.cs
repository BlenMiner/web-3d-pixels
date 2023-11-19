using NetCoreServer;
using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;

namespace PixelsServer
{
    class ChatSession : WsSession
    {
        public ChatSession(WsServer server) : base(server) { }

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
                        SendResponseAsync(Response.MakeRedirectResponse("https://www.google.com/"));
                    }

                    if (urlPath == "/oauth")
                    {
                        SendResponseAsync(Response.MakeGetResponse("OAUTH page " + rootUrl, "text/html; charset=UTF-8"));
                    }

                    break;
            }
        }
    }

    class ChatServer : WsServer
    {
        public ChatServer(IPAddress address, int port) : base(address, port) { }

        protected override TcpSession CreateSession() { return new ChatSession(this); }

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
            // WebSocket server port
            int port = 8080;

            if (args.Length > 0)
                port = int.Parse(args[0]);

            Console.WriteLine($"WebSocket server port: {port}");
            Console.WriteLine($"WebSocket server website: http://localhost:{port}/index.html");

            Console.WriteLine();

            // Create a new WebSocket server
            var server = new ChatServer(IPAddress.Any, port);

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