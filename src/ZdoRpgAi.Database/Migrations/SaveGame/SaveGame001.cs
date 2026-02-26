using Microsoft.Data.Sqlite;

namespace ZdoRpgAi.Database.Migrations.SaveGame;

public class SaveGame001 : IMigration {
    public void Before(SqliteConnection conn) {
        conn.Execute("CREATE TABLE IF NOT EXISTS meta (key TEXT PRIMARY KEY, value TEXT NOT NULL)");
        conn.Execute("INSERT OR IGNORE INTO meta (key, value) VALUES ('dbtype', 'savegame')");
    }

    public string GetSql() => """
        CREATE TABLE IF NOT EXISTS player (
            id TEXT PRIMARY KEY,
            meta JSONB NOT NULL
        );

        CREATE TABLE IF NOT EXISTS npc_new (
            id TEXT PRIMARY KEY,
            meta JSONB NOT NULL
        );

        CREATE TABLE IF NOT EXISTS npc_attribute_value_new (
            npcId TEXT NOT NULL,
            attributeId TEXT NOT NULL,
            meta JSONB NOT NULL,
            PRIMARY KEY (npcId, attributeId)
        );
        """;
}
