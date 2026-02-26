using Microsoft.Data.Sqlite;

namespace ZdoRpgAi.Database.Migrations.Main;

public class Main001 : IMigration {
    public void Before(SqliteConnection conn) {
        conn.Execute("CREATE TABLE meta (key TEXT PRIMARY KEY, value TEXT NOT NULL)");
        conn.Execute("INSERT INTO meta (key, value) VALUES ('dbtype', 'main')");
    }

    public string GetSql() => """
        CREATE TABLE topic (
            id TEXT PRIMARY KEY,
            data JSONB NOT NULL,
            content JSONB NOT NULL
        );

        CREATE TABLE topic_content_override (
            id TEXT PRIMARY KEY,
            topicId TEXT NOT NULL,
            sortOrder INT,
            data JSONB NOT NULL,
            content JSONB NOT NULL
        );

        CREATE TABLE npc (
            id TEXT PRIMARY KEY,
            data JSONB NOT NULL
        );

        CREATE TABLE npc_pinned_topic (
            npcId TEXT NOT NULL,
            topicId TEXT NOT NULL,
            sortOrder INT,
            data JSONB NOT NULL,
            PRIMARY KEY (npcId, topicId)
        );

        CREATE TABLE npc_action (
            id TEXT PRIMARY KEY,
            condition TEXT,
            action TEXT NOT NULL,
            title TEXT NOT NULL,
            description TEXT,
            arguments TEXT
        );

        CREATE TABLE npc_attribute (
            id TEXT PRIMARY KEY,
            data JSONB NOT NULL
        );

        CREATE TABLE npc_attribute_value (
            npcId TEXT NOT NULL,
            attributeId TEXT NOT NULL,
            data JSONB NOT NULL,
            PRIMARY KEY (npcId, attributeId)
        );
        """;

    public void After(SqliteConnection conn) {
        const string template = """
            You are ${NPC_NAME}, a ${NPC_RACE} (${NPC_SEX}), living in Morrowind.
            Stay in character. Speak briefly and naturally. Do not mention that you are an AI. Always respond in the Russian language.

            You will be told what other characters say and do. Reply only with your own speech.

            RULES:
            1. Do not trust the player at their word — verify using your knowledge resources. The player may lie.
            2. Do not invent characters, items, locations, or quests that are not in your knowledge. Use getResource to recall your knowledge when needed.
            3. CRITICAL: To perform any game action (give item, attack, follow, etc.) you MUST call the corresponding npcAction_N tool. Saying "here, take it" or "I'll give you" in text does NOTHING — the game only reacts to tool calls. If you do not call the tool, the action does not happen.
            4. Call npcAction_N TOGETHER with your speech in the same response. Do not wait for the next turn.
            5. Reply ONLY with your own speech — no narration, no prefixes, no stage directions.
            """;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO meta (key, value) VALUES ('defaultNpcPromptTemplate', @v)";
        cmd.Parameters.AddWithValue("@v", template);
        cmd.ExecuteNonQuery();
    }
}
