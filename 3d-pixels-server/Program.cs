using Microsoft.Data.Sqlite;
using System.Net;

namespace PixelsServer
{

    class Program
    {
        static string GetRootPath(string currentDirectory = ".")
        {
            var directoryInfo = new DirectoryInfo(currentDirectory);

            if (directoryInfo.Name == "web-3d-pixels")
                return directoryInfo.FullName;

            var parent = directoryInfo.Parent;

            if (parent == null)
                return string.Empty;

            return GetRootPath(parent.FullName);
        }

        static void Main(string[] args)
        {
            const int PORT = 8080;

            var rootPath = GetRootPath();
            var wwwPath = Path.Combine(rootPath, "www");
            var dataPath = Path.Combine(rootPath, "data");
            var resourcesPath = Path.Combine(rootPath, "resources");
            var oauthSecrets = OAuth.GetSecrets(args, rootPath);

            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);

            Console.WriteLine($"Creating database connection");
            var sqliteConnection = new SqliteConnection($"Data Source={Path.Combine(dataPath, "pixels.sqlite")}");

            try
            {
                sqliteConnection.Open();
            }
            catch (SqliteException ex)
            {
                Console.WriteLine($"# Error opening database connection:");
                Console.WriteLine(ex.Message);
                return;
            }

            Console.WriteLine($"Initialize database tables");

            var db = new Database(sqliteConnection);

            if (!db.InitDatabase(resourcesPath))
                return;

            // Create a new WebSocket server
            var server = new PixelsServer(db, oauthSecrets, IPAddress.Any, PORT);

            server.AddStaticContent(wwwPath);

            var homepage = server.Cache.Find("/index.html");
            if (homepage.Item1)
                server.Cache.Add("/", homepage.Item2);

            // Start the server
            server.Start();

            Console.WriteLine($"Started server on port {PORT}");

            while (true) Thread.Sleep(2000);
        }
    }
}