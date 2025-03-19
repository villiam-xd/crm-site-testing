using System.Text.Json;
using Npgsql;
using server.Classes;
using server.Enums;
using server.Records;

namespace server.api;

public class Issues
{
    private NpgsqlDataSource Db;
    public Issues(WebApplication app, NpgsqlDataSource db, string url)
    {
        Db = db;
        url += "/issues";
        
        app.MapGet(url, (Delegate)GetIssueByCompany);
        app.MapGet(url + "/{issueId}", (Delegate)GetIssue);
        app.MapPut(url + "/{issueId}/state", (Delegate)UpdateIssueState);
        app.MapGet(url + "/{issueId}/messages", (Delegate)GetMessages);
        app.MapPost(url + "/create/{companyId}", (Delegate)CreateIssue);
        app.MapPost(url + "/{issueId}/messages", (Delegate)CreateMessage);
    }

    private async Task<IResult> GetIssueByCompany(HttpContext context)
    {
        if (context.Session.GetString("User") == null)
        {
            return Results.Unauthorized();
        }
        
        var user = JsonSerializer.Deserialize<User>(context.Session.GetString("User"));

        await using var cmd = Db.CreateCommand("SELECT * FROM companys_issues WHERE company_name = @company");
        cmd.Parameters.AddWithValue("@company", user.Company);

        try
        {
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
               List<Issue> issuesList = new List<Issue>();
               while (reader.Read())
               {
                   issuesList.Add(new Issue(
                       reader.GetInt32(reader.GetOrdinal("id")),
                       reader.GetString(reader.GetOrdinal("company_name")),
                       reader.GetString(reader.GetOrdinal("customer_email")),
                       reader.GetString(reader.GetOrdinal("subject")),
                       Enum.Parse<IssueState>(reader.GetString(reader.GetOrdinal("state"))),
                       reader.GetString(reader.GetOrdinal("title")),
                       reader.GetDateTime(reader.GetOrdinal("created")),
                       reader.GetDateTime(reader.GetOrdinal("latest"))
                       ));
               }

               if (issuesList.Count > 0)
               {
                   return Results.Ok(new { issues = issuesList });
               }
               else
               {
                   return Results.NotFound(new { message = "No issues found." });
               }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return Results.Problem("Something went wrong.", statusCode: 500);
        }
    }
    
    private async Task<IResult> GetIssue(int issueId, HttpContext context)
    {
        await using var cmd = Db.CreateCommand("SELECT * FROM companys_issues WHERE id = @issue_id");
        cmd.Parameters.AddWithValue("@issue_id", issueId);

        try
        {
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                Issue issue = null;
                while (await reader.ReadAsync())
                {
                    issue = new Issue(
                        reader.GetInt32(reader.GetOrdinal("id")),
                        reader.GetString(reader.GetOrdinal("company_name")),
                        reader.GetString(reader.GetOrdinal("customer_email")),
                        reader.GetString(reader.GetOrdinal("subject")),
                        Enum.Parse<IssueState>(reader.GetString(reader.GetOrdinal("state"))),
                        reader.GetString(reader.GetOrdinal("title")),
                        reader.GetDateTime(reader.GetOrdinal("created")),
                        reader.GetDateTime(reader.GetOrdinal("latest"))
                    );
                }

                if (issue != null)
                {
                    return Results.Ok(issue);
                }
                else
                {
                    return Results.NotFound(new { message = "No issue found." });
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return Results.Problem("Something went wrong.", statusCode: 500);
        }
    }
    
    private async Task<IResult> UpdateIssueState(int issueId, HttpContext context, UpdateIssueStateRequest updateIssueStateRequest)
    {
        if (context.Session.GetString("User") == null)
        {
            return Results.Unauthorized();
        }
        
        await using var cmd = Db.CreateCommand("UPDATE issues SET state = @state::issue_state WHERE id = @issue_id");
        cmd.Parameters.AddWithValue("@state", Enum.Parse<IssueState>(updateIssueStateRequest.NewState).ToString());
        cmd.Parameters.AddWithValue("@issue_id", issueId);

        try
        {
            var reader = await cmd.ExecuteNonQueryAsync();
            if (reader == 1)
            {
                return Results.Ok(new { message = "Issue state was updated." });
            }
            else
            {
                return Results.Conflict(new { message = "Query executed but something went wrong." });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return Results.Problem("Something went wrong.", statusCode: 500);
        }    
    }

    private async Task<IResult> GetMessages(int issueId, HttpContext context)
    {
        await using var cmd = Db.CreateCommand("SELECT * FROM issue_messages WHERE issue_id = @issue_id");
        cmd.Parameters.AddWithValue("@issue_id", issueId);

        try
        {
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                List<Message> messageList = new List<Message>();
                while (reader.Read())
                {
                    messageList.Add(new Message(
                        reader.GetString(reader.GetOrdinal("message")),
                        reader.GetString(reader.GetOrdinal("sender")),
                        reader.GetString(reader.GetOrdinal("username")),
                        reader.GetDateTime(reader.GetOrdinal("time"))
                        ));
                }

                if (messageList.Count > 0)
                {
                    return Results.Ok(new { messages = messageList});
                }
                else
                {
                    return Results.NotFound(new { message = "No messages found." });
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return Results.Problem("Something went wrong.", statusCode: 500);
        }
    }
    
    private async Task<IResult> CreateIssue(int companyId, CreateIssueRequest createIssueRequest)
    {
        await using var cmd = Db.CreateCommand("INSERT INTO issues (company_id, customer_email, title, subject, state, created) VALUES (@company_id, @customer_email, @title, @subject, 'NEW', current_timestamp) RETURNING id;");
        cmd.Parameters.AddWithValue("@company_id", companyId);
        cmd.Parameters.AddWithValue("@customer_email", createIssueRequest.Email);
        cmd.Parameters.AddWithValue("@title", createIssueRequest.Title);
        cmd.Parameters.AddWithValue("@subject", createIssueRequest.Subject);

        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                await using var cmd2 = Db.CreateCommand("INSERT INTO messages (issue_id, message, sender) VALUES (@issue_id, @message, 'CUSTOMER')");
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

    private async Task<IResult> CreateMessage(int issueId, CreateMessageRequest createMessageRequest)
    {
        await using var cmd = Db.CreateCommand("INSERT INTO messages (issue_id, message, sender, username, time) VALUES (@issue_id, @message, @sender::sender, @username, current_timestamp)");
        cmd.Parameters.AddWithValue("@issue_id", issueId);
        cmd.Parameters.AddWithValue("@message", createMessageRequest.Message);
        cmd.Parameters.AddWithValue("@sender", Enum.Parse<Sender>(createMessageRequest.Sender).ToString());
        cmd.Parameters.AddWithValue("@username", createMessageRequest.Username);
        
        try
        {
            var reader = await cmd.ExecuteNonQueryAsync();
            if (reader == 1)
            {
                return Results.Ok(new { message = "Message was created successfully." });
            }
            else
            {
                return Results.Conflict(new { message = "Query executed but something went wrong." });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);   
            return Results.Problem("Something went wrong.", statusCode: 500);
        }
    }

}