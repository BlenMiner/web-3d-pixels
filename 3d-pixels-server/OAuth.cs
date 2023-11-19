using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelsServer
{
    public static class OAuth
    {
        public static string GetOAuthUrl(string clientId, string redirectUri)
        {
            var query =
                $"scope={Uri.EscapeDataString("https://www.googleapis.com/auth/userinfo.email")}&" +
                $"response_type=code&" +
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                $"client_id={Uri.EscapeDataString(clientId)}";

            return $"https://accounts.google.com/o/oauth2/v2/auth?{query}";
        }
    }
}
