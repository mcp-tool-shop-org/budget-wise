using Microsoft.Data.Sqlite;

namespace BudgetWise.Infrastructure.Database;

/// <summary>
/// Factory for creating and managing SQLite database connections.
/// </summary>
public sealed class SqliteConnectionFactory : IDisposable
{
    public string DatabasePath { get; }

    private readonly string _connectionString;
    private SqliteConnection? _connection;
    private bool _disposed;

    public SqliteConnectionFactory(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("Database path cannot be empty.", nameof(databasePath));

        DatabasePath = databasePath;

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public static SqliteConnectionFactory CreateInMemory()
    {
        return new SqliteConnectionFactory(":memory:");
    }

    public async Task<SqliteConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection is null)
        {
            _connection = new SqliteConnection(_connectionString);
            await _connection.OpenAsync(ct);
        }
        else if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync(ct);
        }

        return _connection;
    }

    public SqliteConnection GetConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection is null)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
        else if (_connection.State != System.Data.ConnectionState.Open)
        {
            _connection.Open();
        }

        return _connection;
    }

    public async Task InitializeDatabaseAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);

        foreach (var tableScript in DatabaseSchema.AllTables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = tableScript;
            await command.ExecuteNonQueryAsync(ct);
        }

        await EnsureTransactionsSoftDeleteSupportAsync(connection, ct);
    }

    private static async Task EnsureTransactionsSoftDeleteSupportAsync(SqliteConnection connection, CancellationToken ct)
    {
        // If the database already existed before IsDeleted was added to the schema,
        // CREATE TABLE IF NOT EXISTS will not apply the new column. Add it via ALTER.
        var hasIsDeleted = false;

        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA table_info(Transactions);";
            await using var reader = await pragma.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var name = reader.GetString(1);
                if (string.Equals(name, "IsDeleted", StringComparison.OrdinalIgnoreCase))
                {
                    hasIsDeleted = true;
                    break;
                }
            }
        }

        if (!hasIsDeleted)
        {
            await using var alter = connection.CreateCommand();
            alter.CommandText = "ALTER TABLE Transactions ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0;";
            await alter.ExecuteNonQueryAsync(ct);
        }

        await using (var index = connection.CreateCommand())
        {
            index.CommandText = "CREATE INDEX IF NOT EXISTS IX_Transactions_IsDeleted ON Transactions(IsDeleted);";
            await index.ExecuteNonQueryAsync(ct);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _connection?.Dispose();
        _connection = null;
        _disposed = true;
    }
}
