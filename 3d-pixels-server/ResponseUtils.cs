using NetCoreServer;

namespace PixelsServer
{
    public static class ResponseUtils
    {
        public static HttpResponse MakeRedirectResponse(this HttpResponse response, string url)
        {
            response.Clear();
            response.SetBegin(301);

            response.SetHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            response.SetHeader("Pragma", "no-cache");
            response.SetHeader("Expires", "0");

            response.SetHeader("Location", url);

            response.SetContentType("text/html; charset=UTF-8");
            response.SetBody($"Redirecting you to {url} ...");
            return response;
        }

        public static HttpResponse MakeHTMLWithoutCaching(this HttpResponse response, string content)
        {
            response.Clear();
            response.SetBegin(200);

            response.SetHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            response.SetHeader("Pragma", "no-cache");
            response.SetHeader("Expires", "0");

            response.SetContentType("text/html; charset=UTF-8");
            response.SetBody(content);

            return response;
        }


        public static HttpResponse MakeRedirectWithCookie(this HttpResponse response, string url, string cookieName, string cookieValue, int durationInSeconds, bool secure)
        {
            response.Clear();
            response.SetBegin(301);

            response.SetHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            response.SetHeader("Pragma", "no-cache");
            response.SetHeader("Expires", "0");

            response.SetHeader("Location", url);
            response.SetCookie(cookieName, cookieValue, durationInSeconds, strict: false, secure: secure);
            response.SetContentType("text/html; charset=UTF-8");
            response.SetBody($"Redirecting you to {url} ...");

            return response;
        }

        public static string? GetHeader(this HttpRequest request, string keyValue)
        {
            for (int i = 0; i < request.Headers; ++i)
            {
                (string key, string val) = request.Header(i);

                if (key == keyValue)
                    return val;
            }

            return null;
        }
    }
}
