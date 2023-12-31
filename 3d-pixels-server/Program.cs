﻿using Microsoft.Data.Sqlite;
using NetCoreServer;
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

        static async void GarbageScheduler(Database db, string ressources)
        {
            const long SECONDS_IN_A_DAY = 60 * 60 * 24;
            long waitedSeconds = 0;

            while (true)
            {
                Thread.Sleep(5000);
                waitedSeconds += 5;

                if (waitedSeconds > SECONDS_IN_A_DAY)
                {
                    Console.WriteLine("Cleaning up sessions");
                    await db.DoSessionCleanup(ressources);
                    waitedSeconds = 0;
                }
            }
        }

        static void Main(string[] args)
        {
            DoMain(args);
        }

        static RessourcesCache? s_ressourcesCache;

        static async void DoMain(string[] args)
        {
            const int PORT = 8080;

            var rootPath = GetRootPath();
            var wwwPath = Path.Combine(rootPath, "www");
            var dataPath = Path.Combine(rootPath, "data");
            var resourcesPath = Path.Combine(rootPath, "resources");
            var oauthSecrets = OAuth.GetSecrets(args, rootPath);

            s_ressourcesCache = new RessourcesCache(resourcesPath);

            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);

            var dbPath = Path.Combine(dataPath, "pixels.sqlite");

            Console.WriteLine($"Creating database connection");

            SqliteConnection sqliteConnection;

            try
            {
                sqliteConnection = new SqliteConnection($"Data Source='{dbPath}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"# Error creating connection object:");
                Console.WriteLine(ex.Message);
                return;
            }


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
            catch (Exception ex)
            {
                Console.WriteLine($"# Error opening database connection:");
                Console.WriteLine(ex.Message);
                return;
            }

            Console.WriteLine($"Initialize database tables");

            var db = new Database(sqliteConnection);

            if (!(await db.InitDatabase(resourcesPath)))
                return;

            // Create a new WebSocket server
            var server = new PixelsServer(s_ressourcesCache, db, oauthSecrets, IPAddress.Any, PORT);

            server.AddStaticContentUnityCompatible(wwwPath);

            /*var homepage = server.Cache.Find("/index.html");
            if (homepage.Item1)
                server.Cache.Add("/", homepage.Item2);*/

            // Start the server
            server.Start();

            Console.WriteLine($"Started server on port {PORT}");

            GarbageScheduler(db, resourcesPath);
        }
    }
}