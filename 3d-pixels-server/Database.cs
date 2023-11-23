using Microsoft.Data.Sqlite;

namespace PixelsServer
{
    public enum Role
    {
        User,
        Moderator,
        Administrator
    }

    public struct UserInfo
    {
        public string ID;
        public string Email;
        public string? AvatarURL;
        public Role Role;
        public bool IsBanned;
        public DateTime? BannedTime;
        public DateTime LastVoxelModificationTime;
        public DateTime CreatedTime;
    }

    public struct SessionInfo
    {
        public string ID;
        public string UserID;
        public DateTime StartTime;
        public DateTime? EndTime;
    }

    public class Database
    {
        private const string INITIALIZE_TABLES_FILE = "initializeTables.sql";
        private const string DELETE_EXPIRED_SESSIONS_FILE = "deleteExpiredSessions.sql";

        readonly SqliteConnection m_connection;

        private bool m_isInitialized = false;

        public Database(SqliteConnection connection)
        {
            m_connection = connection;
        }

        public bool InitDatabase(string resoucesPath)
        {
            if (m_isInitialized) return true;

            m_isInitialized = true;

            var commandTxt = File.ReadAllText(Path.Combine(resoucesPath, INITIALIZE_TABLES_FILE));

            var cmd = m_connection.CreateCommand();
            cmd.CommandText = commandTxt;

            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (SqliteException ex)
            {
                Console.WriteLine($"# Error initializing database tables:");
                Console.WriteLine(ex.Message);
                return false;
            }

            var deleteExpiredSessions = File.ReadAllText(Path.Combine(resoucesPath, DELETE_EXPIRED_SESSIONS_FILE));

            var cmd2 = m_connection.CreateCommand();
            cmd2.CommandText = deleteExpiredSessions;

            try
            {
                cmd2.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException ex)
            {
                Console.WriteLine($"# Error initializing database tables:");
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public bool CreateSession(SessionInfo sessionToCreate)
        {
            var cmd = m_connection.CreateCommand();

            cmd.CommandText =
                "INSERT INTO Sessions (SessionID, UserID, StartTime, EndTime) " +
                $"VALUES ('{sessionToCreate.ID}', '{sessionToCreate.UserID}', {sessionToCreate.StartTime}, {sessionToCreate.EndTime})";

            try
            {
                int inserted = cmd.ExecuteNonQuery();

                if (inserted != 1)
                {
                    Console.WriteLine($"# Error creating session.");
                    return false;
                }

                return true;
            }
            catch (SqliteException ex)
            {
                Console.WriteLine($"# Error creating session:");
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<bool> CreateUserIfDoesntExist(UserInfo userToCreate)
        {
            var cmd = m_connection.CreateCommand();

            cmd.CommandText =
                "INSERT INTO Users (UserID, Email, AvatarURL, Role, IsBanned, BannedTime, LastVoxelModificationTime, CreatedTime) " +
                $"VALUES ('{userToCreate.ID}', '{userToCreate.Email}', '{userToCreate.AvatarURL}', '{userToCreate.Role}', {(userToCreate.IsBanned ? 1 : 0)}, " +
                        $"'{(userToCreate.BannedTime == null ? "NULL" : userToCreate.BannedTime.Value.ToUniversalTime())}'," +
                        $"'{userToCreate.LastVoxelModificationTime.ToUniversalTime()}'," +
                        $"'{userToCreate.CreatedTime.ToUniversalTime()}')";

            try
            {
                int inserted = await cmd.ExecuteNonQueryAsync();

                if (inserted != 1)
                {
                    Console.WriteLine($"# Error creating user:");
                    Console.WriteLine(cmd.CommandText);
                    return false;
                }

                return true;
            }
            catch (SqliteException ex)
            {
                if (ex.SqliteErrorCode == 19)
                    return true;

                Console.WriteLine($"# Error creating user:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(cmd.CommandText);
                return false;
            }
        }
    }
}
