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

    public struct SessionResponse
    {
        public bool Valid;
        public string UserID;
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

        static void HandleException(SqliteException ex, string title)
        {
            Console.WriteLine($"# {title}:");
            Console.WriteLine(ex.Message);
        }

        public async Task<bool> InitDatabase(string resoucesPath)
        {
            if (m_isInitialized) return true;

            m_isInitialized = true;

            var commandTxt = File.ReadAllText(Path.Combine(resoucesPath, INITIALIZE_TABLES_FILE));

            var cmd = m_connection.CreateCommand();
            cmd.CommandText = commandTxt;

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (SqliteException ex)
            {
                HandleException(ex, "Error initializing database tables");
                return false;
            }

            return await DoSessionCleanup(resoucesPath);
        }

        public async Task<bool> DoSessionCleanup(string resoucesPath)
        {
            var deleteExpiredSessions = File.ReadAllText(Path.Combine(resoucesPath, DELETE_EXPIRED_SESSIONS_FILE));

            var cmd = m_connection.CreateCommand();
            cmd.CommandText = deleteExpiredSessions;
            cmd.Parameters.AddWithValue("$now", ToDate(DateTime.Now));

            try
            {
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (SqliteException ex)
            {
                HandleException(ex, "Error doing session cleanup");
                return false;
            }
        }

        private string ToDate(DateTime? date)
        {
            if (date == null)
                return "NULL";

            return date.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF");
        }

        public async Task<bool> DeleteAnyExistingSessions(string userId)
        {
            var cmd = m_connection.CreateCommand();

            cmd.CommandText = "DELETE FROM Sessions WHERE UserID = $userId;";

            cmd.Parameters.AddWithValue("$userId", userId);

            try
            {
                int deleted = await cmd.ExecuteNonQueryAsync();

                if (deleted > 0)
                    Console.WriteLine($"Deleted {deleted} previous sessions.");
                else return false;
            }
            catch (SqliteException ex)
            {
                HandleException(ex, "Error deleting previous session");
                return false;
            }

            return true;
        }

        public async Task<bool> DeleteSessionId(string sessionId)
        {
            var cmd = m_connection.CreateCommand();

            cmd.CommandText = "DELETE FROM Sessions WHERE SessionID = $sessionId;";
            cmd.Parameters.AddWithValue("$sessionId", sessionId);

            try
            {
                int deleted = await cmd.ExecuteNonQueryAsync();

                if (deleted > 0)
                    Console.WriteLine($"Deleted {deleted} sessions.");
                else return false;
            }
            catch (SqliteException ex)
            {
                HandleException(ex, "Error deleting previous session");
                return false;
            }

            return true;
        }

        public bool CreateSession(SessionInfo sessionToCreate)
        {
            var cmd = m_connection.CreateCommand();

            cmd.CommandText =
                "INSERT INTO Sessions (SessionID, UserID, StartTime, EndTime) " +
                $"VALUES ($sessionId, $userId, $startTime, $endTime)";

            cmd.Parameters.AddWithValue("$sessionId", sessionToCreate.ID);
            cmd.Parameters.AddWithValue("$userId", sessionToCreate.UserID);
            cmd.Parameters.AddWithValue("$startTime", ToDate(sessionToCreate.StartTime));
            cmd.Parameters.AddWithValue("$endTime", ToDate(sessionToCreate.EndTime));

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
                HandleException(ex, "Error creating session");
                return false;
            }
        }

        public async Task<SessionResponse> ValidateSession(string sessionID)
        {
            var cmd = m_connection.CreateCommand();

            cmd.CommandText = $"SELECT UserID FROM Sessions WHERE SessionID = $sessionId AND EndTime > $now";

            cmd.Parameters.AddWithValue("$sessionId", sessionID);
            cmd.Parameters.AddWithValue("$now", ToDate(DateTime.Now));

            try
            {
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var userId = reader.GetString(0);

                    return new SessionResponse
                    {
                        Valid = true,
                        UserID = userId
                    };
                }
            }
            catch (SqliteException ex)
            {
                HandleException(ex, "Error validating session");
            }

            return default;
        }

        public async Task<UserInfo?> GetUserInfoFromID(string userId)
        {
            var cmd = m_connection.CreateCommand();

            cmd.CommandText = $"SELECT * FROM Users WHERE UserID = $userId";

            cmd.Parameters.AddWithValue("$userId", userId);

            try
            {
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    UserInfo user;

                    user.ID = reader.GetString(0);
                    user.Email = reader.GetString(1);
                    user.AvatarURL = reader.GetString(2);
                    user.Role = (Role)reader.GetInt32(3);
                    user.IsBanned = reader.GetBoolean(4);
                    user.BannedTime = reader.GetString(5) == "NULL" ? null : reader.GetDateTime(5);
                    user.LastVoxelModificationTime = reader.GetDateTime(6);
                    user.CreatedTime = reader.GetDateTime(7);

                    return user;
                }
            }
            catch (SqliteException ex)
            {
                HandleException(ex, "Error getting user info");
            }

            return null;
        }

        public async Task<bool> CreateUserIfDoesntExist(UserInfo userToCreate)
        {
            var cmd = m_connection.CreateCommand();

            cmd.CommandText =
                "INSERT INTO Users (UserID, Email, AvatarURL, Role, IsBanned, BannedTime, LastVoxelModificationTime, CreatedTime) " +
                $"VALUES ($userId, $email, $avatarUrl, $role, $isBanned, $bannedTime, $lastVoxelModificationTime, $createdTime)";

            cmd.Parameters.AddWithValue("$userId", userToCreate.ID);
            cmd.Parameters.AddWithValue("$email", userToCreate.Email);
            cmd.Parameters.AddWithValue("$avatarUrl", userToCreate.AvatarURL);
            cmd.Parameters.AddWithValue("$role", userToCreate.Role);
            cmd.Parameters.AddWithValue("$isBanned", userToCreate.IsBanned);
            cmd.Parameters.AddWithValue("$bannedTime", ToDate(userToCreate.BannedTime));
            cmd.Parameters.AddWithValue("$lastVoxelModificationTime", ToDate(userToCreate.LastVoxelModificationTime));
            cmd.Parameters.AddWithValue("$createdTime", ToDate(userToCreate.CreatedTime));

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

                HandleException(ex, "Error creating user");
                return false;
            }
        }
    }
}
