using System;
using System.Data.SQLite;
using System.IO;

public class IdStore : IDisposable
{
    private readonly string _dbPath;
    private readonly SQLiteConnection _connection;

    public IdStore()
    {
        _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Index", "idstore.db");

        if (!File.Exists(_dbPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));
            SQLiteConnection.CreateFile(_dbPath);
        }

        _connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;");
        _connection.Open();

        EnsureTable();
    }

    private void EnsureTable()
    {
        string createTableQuery = @"
            CREATE TABLE IF NOT EXISTS IdStore (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT UNIQUE NOT NULL
            );";

        using (var command = new SQLiteCommand(createTableQuery, _connection))
        {
            command.ExecuteNonQuery();
        }
    }

    public int Add(string name)
    {
        int existingId = GetIdByName(name);
        if (existingId != -1)
            return existingId;

        string insertQuery = "INSERT INTO IdStore (Name) VALUES (@Name);";
        using (var command = new SQLiteCommand(insertQuery, _connection))
        {
            command.Parameters.AddWithValue("@Name", name);
            command.ExecuteNonQuery();
        }

        return (int)_connection.LastInsertRowId;
    }

    public int GetIdByName(string name)
    {
        string selectQuery = "SELECT Id FROM IdStore WHERE Name = @Name;";
        using (var command = new SQLiteCommand(selectQuery, _connection))
        {
            command.Parameters.AddWithValue("@Name", name);
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                    return reader.GetInt32(0);
            }
        }
        return -1;
    }

    public string GetNameById(int id)
    {
        string selectQuery = "SELECT Name FROM IdStore WHERE Id = @Id;";
        using (var command = new SQLiteCommand(selectQuery, _connection))
        {
            command.Parameters.AddWithValue("@Id", id);
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                    return reader.GetString(0);
            }
        }
        return null;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
