using Microsoft.Data.Sqlite;

namespace ZdoRpgAi.Database;

public static class SqliteExtensions {
    public static void Execute(this SqliteConnection conn, string sql) {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
