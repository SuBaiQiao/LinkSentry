using System;
using System.IO;
using Microsoft.Data.Sqlite;
using LinkSentry.Models;

namespace LinkSentry.Services;

/// <summary>
/// Handles SQLite database connection management and schema initialization.
/// </summary>
public class SqliteDbFactory
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public SqliteDbFactory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "LinkSentry");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        _dbPath = Path.Combine(dir, "traffic_history.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    /// <summary>
    /// Creates and opens a new SQLite connection with WAL mode enabled.
    /// </summary>
    public SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enable Write-Ahead Logging for better concurrency
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();

        return connection;
    }

    /// <summary>
    /// Initializes the database schema if it doesn't exist.
    /// </summary>
    public void Initialize()
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS TrafficHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp INTEGER NOT NULL,
                InterfaceName TEXT NOT NULL,
                SpeedSent REAL NOT NULL,
                SpeedReceived REAL NOT NULL,
                TotalSent INTEGER,
                TotalReceived INTEGER
            );
            
            CREATE INDEX IF NOT EXISTS IX_TrafficHistory_Interface_Time 
            ON TrafficHistory (InterfaceName, Timestamp);
        ";
        command.ExecuteNonQuery();
    }

    public string GetDatabasePath() => _dbPath;
}
