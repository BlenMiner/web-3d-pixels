namespace PixelsServer
{
    internal class RessourcesCache
    {
        public readonly string RootPath;

        Dictionary<string, string> m_cache = new ();

        public RessourcesCache(string rootPath)
        {
            RootPath = rootPath;
        }

        internal string ReadAllText(string relativePath)
        {
            if (m_cache.TryGetValue(relativePath, out var content))
                return content;

            return File.ReadAllText(Path.Combine(RootPath, relativePath));
        }
    }
}
