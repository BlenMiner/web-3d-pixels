using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelsServer
{
    public static class OAuth
    {
        public static string GetOAuthUrl(string clientId, string redirectUri, string scope, string state)
        {
            return $"https://accounts.google.com/o/oauth2/v2/auth?" +
                // $"scope=https%3A//www.googleapis.com/auth/drive.metadata.readonly&" +
                $"response_type=code&" +
                $"redirect_uri={redirectUri}&" +
                $"client_id=client_id";
        }

        static string GenerateGoogleAuthUrl()
        {
            // Replace with your client ID and redirect URI
            string clientId = "YOUR_CLIENT_ID";
            string redirectUri = "http://localhost:8080/oauth2callback";
            string scopes = "openid%20email";
            return $"https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope={scopes}&access_type=offline";
        }
    }
}
