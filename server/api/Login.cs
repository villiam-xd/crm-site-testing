using System.Text.Json;
using Npgsql;
using server.Classes;
using server.Enums;
using server.Records;

namespace server.api;

public class Login
{
    private NpgsqlDataSource Db;
    public Login(WebApplication app, NpgsqlDataSource db, string url)
    {
        Db = db;
        url += "/login";
        
        app.MapPost(url, (Delegate)SetLogin);
        app.MapGet(url, (Delegate)GetLogin);
        app.MapGet(url + "/role", (Delegate)GetRole);
        app.MapDelete(url, (Delegate)Logout);
        
    }
    
    private async Task<IResult> SetLogin(HttpContext context, LoginRequest loginRequest)
    {
        if (context.Session.GetString("User") != null)
        {
            return Results.BadRequest(new { message = "Someone is already logged in."});
        }
        
        await using var cmd = Db.CreateCommand("SELECT * FROM users_with_company WHERE email = @email AND password = @password");
        cmd.Parameters.AddWithValue("@email", loginRequest.Email);
        cmd.Parameters.AddWithValue("@password", loginRequest.Password);

        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    User user = new User(
                        reader.GetInt32(reader.GetOrdinal("user_id")),
                        reader.GetString(reader.GetOrdinal("username")),
                        Enum.Parse<Role>(reader.GetString(reader.GetOrdinal("role"))),
                        reader.GetInt32(reader.GetOrdinal("company_id")),
                        reader.GetString(reader.GetOrdinal("company_name"))
                        );
                    await Task.Run(() => context.Session.SetString("User", JsonSerializer.Serialize(user)));
                    return Results.Ok(new
                    {
                        username = user.Username, 
                        role = user.Role.ToString(),
                        company = user.Company
                    });
                }
            }
        }
        return Results.NotFound(new { message = "User not found." });
    }

    private async Task<IResult> GetLogin(HttpContext context)
    {
        var key = await Task.Run(() => context.Session.GetString("User"));
        if (key == null)
        {
            return Results.NotFound(new { message = "No one is logged in." });
        }
        var user = JsonSerializer.Deserialize<User>(key);
        return Results.Ok(new { username = user?.Username, 
            role = user?.Role.ToString(), 
            company = user?.Company
        });
    }
    
    private async Task<IResult> GetRole(HttpContext context)
    {
        var key = await Task.Run(() => context.Session.GetString("User"));
        if (key == null)
        {
            return Results.NotFound(new { message = "No one is logged in." });
        }
        var user = JsonSerializer.Deserialize<User>(key);
        return Results.Ok(new { role = user?.Role.ToString() });
    }

    private async Task<IResult> Logout(HttpContext context)
    {
        if (context.Session.GetString("User") == null)
        {
            return Results.Conflict(new { message = "No login found."});
        }
        Console.WriteLine("ClearSession is called..Clearing session");
        await Task.Run(context.Session.Clear);
        return Results.Ok(new { message = "Session cleared" });
    }
}