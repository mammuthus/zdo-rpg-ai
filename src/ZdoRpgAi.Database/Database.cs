using Microsoft.Data.Sqlite;
using ZdoRpgAi.Core;

namespace ZdoRpgAi.Database;

public abstract class Database : IDisposable {
    static readonly ILog Log = Logger.Get<Database>();

    private readonly string _path;
    private readonly MigrationRunner _migrationRunner;
    private SqliteConnection? _connection;

    protected Database(string path, IMigration[] migrations) {
        _path = path;
        _migrationRunner = new MigrationRunner(migrations, DbType);
    }

    protected abstract string DbType { get; }

    public SqliteConnection Connection => _connection ?? throw new InvalidOperationException("Database not opened");

    public void Open() {
        _connection = new SqliteConnection($"Data Source={_path}");
        _connection.Open();

        if (IsNewDatabase(Connection)) {
            _migrationRunner.Run(Connection);
        }
        else {
            ValidateDbType();
            _migrationRunner.Run(Connection);
        }
    }

    public void Dispose() {
        _connection?.Dispose();
        _connection = null;
    }

    private void ValidateDbType() {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = "SELECT value FROM meta WHERE key = 'dbtype'";
        var result = cmd.ExecuteScalar();

        if (result is not string dbType)
            throw new InvalidOperationException("Database is missing 'dbtype' in meta table");

        if (dbType != DbType)
            throw new InvalidOperationException(
                $"Database type mismatch: expected '{DbType}', got '{dbType}'");
    }

    private static bool IsNewDatabase(SqliteConnection conn) {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='meta'";
        return (long)cmd.ExecuteScalar()! == 0;
    }
}
