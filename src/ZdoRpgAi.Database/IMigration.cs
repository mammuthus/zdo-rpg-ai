using Microsoft.Data.Sqlite;

namespace ZdoRpgAi.Database;

public interface IMigration {
    void Before(SqliteConnection conn) { }
    string GetSql();
    void After(SqliteConnection conn) { }
}
