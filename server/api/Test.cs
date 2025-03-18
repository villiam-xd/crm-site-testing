using Npgsql;

namespace server.api;

public class Test
{
    private NpgsqlDataSource Db;
    public Test(WebApplication app, NpgsqlDataSource db, string url)
    {
        Db = db;
        url += "/";
        
        app.MapGet(url, () => "Server is running!");
    }
}