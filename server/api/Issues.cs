using System.Text.Json;
using Npgsql;
using server.Classes;
using server.Enums;
using server.Records;
using server.Services;

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
        app.MapPost(url + "/create/{companyName}", (Delegate)CreateIssue);
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
    
    private async Task<IResult> CreateIssue(string companyName, CreateIssueRequest createIssueRequest, IEmailService email)
    {
        await using var cmd = Db.CreateCommand("SELECT * FROM companys WHERE name = @company_name");
        cmd.Parameters.AddWithValue("@company_name", companyName);

        try
        {
            var companyId = await cmd.ExecuteScalarAsync();
            if (companyId is null)
            {
                return Results.NotFound(new { message = "No company found." });
            }
        
            
            await using var cmd2 = Db.CreateCommand("INSERT INTO issues (company_id, customer_email, title, subject, state, created) VALUES (@company_id, @customer_email, @title, @subject, 'NEW', current_timestamp) RETURNING id;");
            cmd2.Parameters.AddWithValue("@company_id", companyId);
            cmd2.Parameters.AddWithValue("@customer_email", createIssueRequest.Email);
            cmd2.Parameters.AddWithValue("@title", createIssueRequest.Title);
            cmd2.Parameters.AddWithValue("@subject", createIssueRequest.Subject);

            var issuesId = await cmd2.ExecuteScalarAsync();
            if (issuesId is not null)
            {
                await using var cmd3 = Db.CreateCommand("INSERT INTO messages (issue_id, message, sender, username, time) VALUES (@issue_id, @message, 'CUSTOMER', @username, current_timestamp)");
                cmd3.Parameters.AddWithValue("@issue_id", issuesId);
                cmd3.Parameters.AddWithValue("@message", createIssueRequest.Message);
                cmd3.Parameters.AddWithValue("@username", createIssueRequest.Email);
                    
                try
                {
                    int rowsAffected = await cmd3.ExecuteNonQueryAsync();
                    if (rowsAffected == 1)
                    {
                        await email.SendEmailAsync(createIssueRequest.Email, 
                            $"{companyName} - ISSUE: {createIssueRequest.Title}", 
                            IssueCreatedMessage(companyName, 
                                createIssueRequest.Message, 
                                createIssueRequest.Title,
                                issuesId.ToString()));
                        return Results.Ok(new { message = "Issue was created successfully." });
                    }
                    else
                    {
                        return Results.Conflict(new { message = "Query executed but something went wrong." });
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    await using var cmd4 = Db.CreateCommand("DELETE FROM issues WHERE id = @issue_id;");
                    cmd4.Parameters.AddWithValue("@issue_id", issuesId);
                    await cmd4.ExecuteNonQueryAsync();
                        
                    return Results.Conflict(new { message = "Issue was created, but something went wrong during the process, so the issue has been deleted." });
                }
            }
            else
            {
                return Results.Problem("Something went wrong.", statusCode: 500);   
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return Results.Problem("Something went wrong.", statusCode: 500);
        }
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

    private string IssueCreatedMessage(string companyName, string message, string title, string chatId)
    {
        return $"<h1>{companyName}</h1>" +
               $"<br> <p>Tack för att du kontaktade oss!</p>" +
               "<p>Vi har tagit emot dit meddelande: </p>" +
               $"<br> <p><i>{message}</i></p> <br>" +
               $"<p>Vi har skapat ett chatt-rum där du kan prata direkt med en av våra kundtjänstmedarbetare angående ditt ärende <strong>{title}</strong>.</p>" +
               $"<p>För att ansluta till chatten, <a href='http://localhost:5173/chat/{{chatId}}'> klicka på denna länken.</a></p>" +
               $"<br> <br> <p>Vänliga hälsningar,</p>" +
               $"<p><strong>{companyName}</strong> kundtjänst.<br>";
    }
}