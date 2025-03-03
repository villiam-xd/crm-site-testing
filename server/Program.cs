using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using server;
using server.Classes;
using server.Enums;
using server.Records;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

Database database = new Database();
NpgsqlDataSource db = database.Connection();

var app = builder.Build();

app.UseSession();



app.MapGet("/", () => "Hello World!");

app.MapPost("/api/login", (Delegate)Login);
app.MapGet("/api/login", (Delegate)GetLogin);
app.MapDelete("/api/login", (Delegate)Logout);

async Task<IResult> Login(HttpContext context, LoginRequest loginRequest)
{
    if (context.Session.GetString("User") != null)
    {
        return Results.BadRequest("Someone is already logged in.");
    }
    Console.WriteLine("SetSession is called..Setting session");
    Console.WriteLine(loginRequest);
    
    await using var cmd = db.CreateCommand("SELECT * FROM users WHERE username = @username AND password = @password");
    cmd.Parameters.AddWithValue("@username", loginRequest.Username);
    cmd.Parameters.AddWithValue("@password", loginRequest.Password);

    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                User user = new User(
                    reader.GetInt32(reader.GetOrdinal("id")),
                    reader.GetString(reader.GetOrdinal("username")),
                    Enum.Parse<Role>(reader.GetString(reader.GetOrdinal("role")))
                    );
                await Task.Run(() => context.Session.SetString("User", JsonSerializer.Serialize(user)));
                return Results.Ok(user);
            }
        }
    }
    return Results.NotFound("User not found.");
}

async Task<IResult> GetLogin(HttpContext context)
{
    var key = await Task.Run(() => context.Session.GetString("User"));
    if (key == null)
    {
        return Results.NotFound("No one is logged in.");
    }
    var user = JsonSerializer.Deserialize<User>(key);
    return Results.Ok(new {username = user?.Username, role = user?.Role});
}

async Task<IResult> Logout(HttpContext context)
{
    if (context.Session.GetString("User") == null)
    {
        return Results.Conflict("No login found.");
    }
    Console.WriteLine("ClearSession is called..Clearing session");
    await Task.Run(context.Session.Clear);
    return Results.Ok("Session cleared");
}

await app.RunAsync();

