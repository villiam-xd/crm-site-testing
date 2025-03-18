using Npgsql;
using server.Records;

namespace server.api;

public class Issues
{
    private NpgsqlDataSource Db;
    public Issues(WebApplication app, NpgsqlDataSource db, string url)
    {
        Db = db;
        url += "/issues";
        
        app.MapPost(url + "/create/{companyId}", (Delegate)CreateIssue);
    }
    async Task<IResult> CreateIssue(int companyId, CreateIssueRequest createIssueRequest)
    {
        await using var cmd = Db.CreateCommand("INSERT INTO issues (company_id, customer_email, title, subject, state) VALUES (@company_id, @customer_email, @title, @subject, 'NEW') RETURNING id;");
        cmd.Parameters.AddWithValue("@company_id", companyId);
        cmd.Parameters.AddWithValue("@customer_email", createIssueRequest.Email);
        cmd.Parameters.AddWithValue("@title", createIssueRequest.Title);
        cmd.Parameters.AddWithValue("@subject", createIssueRequest.Subject);

        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                await using var cmd2 = Db.CreateCommand("INSERT INTO messages (issue_id, message, sender) VALUES (@issue_id, @message, 'CUSTOMER');");
                cmd2.Parameters.AddWithValue("@issue_id", reader.GetInt32(reader.GetOrdinal("id")));
                cmd2.Parameters.AddWithValue("@message", createIssueRequest.Message);
                    
                try
                {
                    int rowsAffected = await cmd2.ExecuteNonQueryAsync();
                    if (rowsAffected == 1)
                    {
                        return Results.Ok(new { message = "Issue was created successfully." });
                    }
                    else
                    {
                        return Results.Conflict(new { message = "Query executed but something went wrong." });
                    }
                }
                catch
                {
                    await using var cmd3 = Db.CreateCommand("DELETE FROM issues WHERE id = @issue_id;");
                    cmd3.Parameters.AddWithValue("@issue_id", reader.GetInt32(reader.GetOrdinal("id")));
                    await cmd3.ExecuteNonQueryAsync();
                        
                    return Results.Conflict(new { message = "Issue was created, but something went wrong during the process, so the issue has been deleted." });
                }
            }
        }
        
        return Results.Problem("Something went wrong.", statusCode: 500);
    }

}