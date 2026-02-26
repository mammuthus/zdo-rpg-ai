using ZdoRpgAi.Database;

namespace ZdoRpgAi.Repository;

public class LocalDatabaseSaveGameRepository : ISaveGameRepository {
    private readonly SaveGameDatabase _db;

    public LocalDatabaseSaveGameRepository(SaveGameDatabase db) {
        _db = db;
    }
}
