using Npgsql;

namespace server.api;

public class Companies
{
    private NpgsqlDataSource Db;

    public Companies(WebApplication app, NpgsqlDataSource db, string url)
    {
        Db = db;
        url += "/companies";

        app.MapGet(url, (Delegate)GetCompanies);
    }

    private async Task<IResult> GetCompanies(HttpRequest req, HttpResponse res)
    {
        await using var cmd = Db.CreateCommand("SELECT name FROM companys");

        try
        {
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                List<String> companiesList = new();
                while (reader.Read())
                {
                    companiesList.Add(reader.GetString(0));
                }

                if (companiesList.Count > 0)
                {
                    return Results.Ok(new {companies = companiesList});
                }
                else
                {
                    return Results.NotFound(new { message = "No companies found." });
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return Results.Problem("Something went wrong.", statusCode: 500);
        }
    }
    
}