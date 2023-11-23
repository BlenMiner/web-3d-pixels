-- Creating the User table
CREATE TABLE IF NOT EXISTS Users (
    UserID TEXT PRIMARY KEY UNIQUE,
    Email TEXT NOT NULL UNIQUE,
    AvatarURL TEXT,
    Role TEXT NOT NULL,
    IsBanned INTEGER NOT NULL DEFAULT 0,
    BannedTime DATETIME DEFAULT NULL,
    LastVoxelModificationTime DATETIME DEFAULT CURRENT_TIMESTAMP,
    CreatedTime DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Creating the Session table
CREATE TABLE IF NOT EXISTS Sessions (
    SessionID TEXT PRIMARY KEY,
    UserID TEXT,
    StartTime DATETIME DEFAULT CURRENT_TIMESTAMP,
    EndTime DATETIME DEFAULT NULL,
    FOREIGN KEY(UserID) REFERENCES Users(UserID)
);