namespace ZdoRpgAi.Database;

public class SaveGameDatabase : Database {
    public SaveGameDatabase(string path) : base(path, Migrations.SaveGame.All.Migrations) { }

    protected override string DbType => "savegame";
}
