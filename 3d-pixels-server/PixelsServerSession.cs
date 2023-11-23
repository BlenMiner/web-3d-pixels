using NetCoreServer;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PixelsServer
{
    class PixelsServerSession : WsSession
    {
        private readonly OAuthSecrets m_secrets;

        private readonly System.Net.Http.HttpClient m_client = new System.Net.Http.HttpClient();

        private readonly Database m_database;

        public PixelsServerSession(WsServer server, Database db, OAuthSecrets oauthSecrets) : base(server) 
        {
            m_secrets = oauthSecrets;
            m_database = db;
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

        protected override async void OnReceivedRequest(HttpRequest request)
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
                        HandleLogin(request, rootUrl, host);
                    else if (urlPath == "/oauth")
                        await HandleOAuthResponse(rootUrl, urlQuery);

                    break;
            }
        }

        private void HandleLogin(HttpRequest req, string rootUrl, string host)
        {
            for (int i = 0; i < req.Cookies; i++)
            {
                (string key, string value) = req.Cookie(i);

                if (key == "session")
                {
                    SendResponseAsync(Response.MakeGetResponse($"Session: {value}"));
                    return;
                }
            }

            var resp = Response.MakeGetResponse($"Total cookies: {req.Cookies}");
            resp.SetCookie("session", "123", 5959, secure: false);
            SendResponseAsync(resp);
            return;

            var redirectAt = $"{rootUrl}/oauth";
            var oauthUrl = OAuth.GetOAuthUrl(m_secrets.ClientID, redirectAt);
            SendResponseAsync(Response.MakeRedirectResponse(oauthUrl));
        }

        private async Task HandleOAuthResponse(string rootUrl, string urlQuery)
        {
            var end = urlQuery.IndexOf('&');
            var code = urlQuery[5..end];

            var values = new Dictionary<string, string>
                        {
                            { "code", code },
                            { "client_id", m_secrets.ClientID },
                            { "client_secret", m_secrets.ClientSecret },
                            { "redirect_uri", rootUrl + "/oauth" },
                            { "grant_type", "authorization_code" }
                        };

            var content = new FormUrlEncodedContent(values);

            var response = await m_client.PostAsync("https://oauth2.googleapis.com/token", content);

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            var accessToken = json["access_token"]?.ToString();

            if (accessToken == null)
            {
                SendResponseAsync(Response.MakeGetResponse("OAUTH: Invalid response from google", "text/html; charset=UTF-8"));
                return;
            }

            var userInfo = await m_client.GetAsync($"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}");
            var userInfoText = await userInfo.Content.ReadAsStringAsync();

            var userInfoData = JObject.Parse(userInfoText);

            var id = userInfoData["id"]?.ToString();
            var email = userInfoData["email"]?.ToString();
            var avatarUrl = userInfoData["picture"]?.ToString();

            if (id == null || email == null)
            {
                SendResponseAsync(Response.MakeGetResponse("OAUTH: Failed to get user data", "text/html; charset=UTF-8"));
                return;
            }

            var newUserInfo = new UserInfo
            {
                ID = id,
                Email = email,
                AvatarURL = avatarUrl,
                Role = Role.User,
                IsBanned = false,
                BannedTime = null,
                LastVoxelModificationTime = DateTime.Now,
                CreatedTime = DateTime.Now
            };

            if (!await m_database.CreateUserIfDoesntExist(newUserInfo))
            {
                SendResponseAsync(Response.MakeGetResponse("OAUTH: Failed to create user", "text/html; charset=UTF-8"));
                return;
            }

            TimeSpan sessionDuration = TimeSpan.FromSeconds(60);

            var sessionInfo = new SessionInfo
            {
                ID = Guid.NewGuid().ToString(),
                UserID = id,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.Add(sessionDuration)
            };

            m_database.CreateSession(sessionInfo);

            var finalResponse = Response.MakeGetResponse("OAUTH: created session", "text/html; charset=UTF-8");

            finalResponse.SetCookie("session", sessionInfo.ID, sessionDuration.Seconds);

            SendResponseAsync(finalResponse);
        }
    }
}