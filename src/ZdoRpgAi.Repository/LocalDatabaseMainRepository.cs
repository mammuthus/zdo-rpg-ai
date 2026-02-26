using ZdoRpgAi.Database;

namespace ZdoRpgAi.Repository;

public class LocalDatabaseMainRepository : IMainRepository {
    private readonly MainDatabase _db;

    public LocalDatabaseMainRepository(MainDatabase db) {
        _db = db;
    }
}
