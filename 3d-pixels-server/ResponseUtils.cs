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
            response.SetBody("Redirecting you...");
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
