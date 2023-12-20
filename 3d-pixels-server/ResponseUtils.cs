using NetCoreServer;
using System.Runtime.CompilerServices;

namespace PixelsServer
{
    public static class ResponseUtils
    {
        public static void AddStaticContentUnityCompatible(this HttpServer server, string path, string prefix = "/", string filter = "*.*", TimeSpan? timeout = null)
        {
            TimeSpan valueOrDefault = timeout.GetValueOrDefault();
            if (!timeout.HasValue)
            {
                valueOrDefault = TimeSpan.FromHours(1.0);
                timeout = valueOrDefault;
            }

            server.Cache.InsertPath(path, prefix, filter, timeout.Value, Handler);
            static bool Handler(FileCache cache, string key, byte[] value, TimeSpan timespan)
            {
                HttpResponse httpResponse = new HttpResponse();
                httpResponse.SetBegin(200);

                var extension = Path.GetExtension(key);

                httpResponse.SetContentType(extension);

                switch (extension)
                {
                    case ".gz":
                        httpResponse.SetHeader("Content-Encoding", "gzip");

                        if (key.EndsWith(".wasm.gz"))
                             httpResponse.SetHeader("Content-Type", "application/wasm");
                        else httpResponse.SetContentType(extension);
                        break;
                    default:
                        httpResponse.SetContentType(extension);
                        break;
                }

                DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(8, 1);
                defaultInterpolatedStringHandler.AppendLiteral("max-age=");
                defaultInterpolatedStringHandler.AppendFormatted(timespan.Seconds);
                httpResponse.SetHeader("Cache-Control", defaultInterpolatedStringHandler.ToStringAndClear());
                httpResponse.SetBody(value);
                return cache.Add(key, httpResponse.Cache.Data, timespan);
            }
        }

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
