using Npgsql;
using server.Classes;

namespace server.api;

public class Forms
{
    private NpgsqlDataSource Db;
    public Forms(WebApplication app, NpgsqlDataSource db, string url)
    {
        Db = db;
        url += "/forms";
        
        app.MapGet(url + "/{companyName}", (Delegate)GetCompanyForm);
    }
    
    
    async Task<IResult> GetCompanyForm(string companyName)
    {
        await using var cmd = Db.CreateCommand("SELECT * FROM companys WHERE name = @company_name");
        cmd.Parameters.AddWithValue("@company_name", companyName);
    
        try
        {
            var reader = await cmd.ExecuteScalarAsync();
            if (reader is not null)
            {
                await using var cmd2 = Db.CreateCommand("SELECT name FROM subjects WHERE company_id = @company_id");
                cmd2.Parameters.AddWithValue("@company_id", (Int32) reader);

                await using (var reader2 = await cmd2.ExecuteReaderAsync())
                {
                    List<String> formSubjects = new List<string>();
                    while (await reader2.ReadAsync())
                    {
                        formSubjects.Add(reader2.GetString(0));
                    }

                    if (formSubjects.Count == 0)
                    {
                        return Results.NotFound(new { message = "No subjects was found." });
                    }
                    
                    return Results.Ok(new {company_info = new CompanyForm(companyName, formSubjects)});
                }
            }else
            {
                return Results.NotFound(new { message = "No company was found." });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return Results.Problem("Something went wrong.", statusCode: 500);
        }
    }
}