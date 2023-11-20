using Newtonsoft.Json.Linq;

namespace PixelsServer
{
    public struct OAuthSecrets
    {
        public string ClientID;
        public string ClientSecret;
    }

    public static class OAuth
    {
        public static OAuthSecrets GetSecrets(string[] args, string rootPath)
        {
            var secrets = new OAuthSecrets();

            if (args.Length < 2)
            {
                // Load from file

                string path = Path.Combine(rootPath, "oauth.json");

                if (!File.Exists(path))
                {
                    Console.WriteLine($"Could not find oauth.json at {path}");
                    return secrets;
                }

                var json = JObject.Parse(File.ReadAllText(path));

                var webRoot = json["web"];

                if (webRoot == null)
                {
                    Console.WriteLine($"Could not find web root in oauth.json");
                    return secrets;
                }

                var clientId = webRoot["client_id"];
                var clientSecret = webRoot["client_secret"];

                if (clientId == null || clientSecret == null)
                {
                    Console.WriteLine($"Could not find client_id or client_secret in oauth.json");
                    return secrets;
                }

                secrets.ClientID = clientId.ToString();
                secrets.ClientSecret = clientSecret.ToString();

                return secrets;
            }

            secrets.ClientID = args[0];
            secrets.ClientSecret = args[1];

            return secrets;
        }

        public static string GetOAuthUrl(string clientId, string redirectUri)
        {
            var query =
                $"scope=https://www.googleapis.com/auth/userinfo.email openid&" +
                $"response_type=code&" +
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                $"client_id={Uri.EscapeDataString(clientId)}";

            return $"https://accounts.google.com/o/oauth2/v2/auth?{query}";
        }
    }
}
