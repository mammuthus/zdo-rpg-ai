namespace ZdoRpgAi.Database;

public class MainDatabase : Database {
    public MainDatabase(string path) : base(path, Migrations.Main.All.Migrations) { }

    protected override string DbType => "main";
}
