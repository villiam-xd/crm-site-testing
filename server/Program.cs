using System.Text.Json;
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
app.MapPost("/api/users", (Delegate)CreateUser);

async Task<IResult> Login(HttpContext context, LoginRequest loginRequest)
{
    if (context.Session.GetString("User") != null)
    {
        return Results.BadRequest(new { message = "Someone is already logged in."});
    }
    
    await using var cmd = db.CreateCommand("SELECT * FROM users WHERE email = @email AND password = @password");
    cmd.Parameters.AddWithValue("@email", loginRequest.Email);
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
                return Results.Ok(new { username = user.Username, role = user.Role.ToString() });
            }
        }
    }
    return Results.NotFound(new { message = "User not found." });
}

async Task<IResult> GetLogin(HttpContext context)
{
    var key = await Task.Run(() => context.Session.GetString("User"));
    if (key == null)
    {
        return Results.NotFound(new { message = "No one is logged in." });
    }
    var user = JsonSerializer.Deserialize<User>(key);
    return Results.Ok(new { username = user?.Username, role = user?.Role.ToString() });
}

async Task<IResult> Logout(HttpContext context)
{
    if (context.Session.GetString("User") == null)
    {
        return Results.Conflict(new { message = "No login found."});
    }
    Console.WriteLine("ClearSession is called..Clearing session");
    await Task.Run(context.Session.Clear);
    return Results.Ok(new { message = "Session cleared" });
}

async Task<IResult> CreateUser(RegisterRequest registerRequest)
{
    try
    {

        await using var cmd =
            db.CreateCommand(
                "INSERT INTO users (username, password, role, email) VALUES (@username, @password, 'ADMIN', @email);");
        cmd.Parameters.AddWithValue("@username", registerRequest.Username);
        cmd.Parameters.AddWithValue("@email", registerRequest.Email);
        cmd.Parameters.AddWithValue("@password", registerRequest.Password);

        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            Console.WriteLine(reader.RecordsAffected);
            if (reader.RecordsAffected == 1)
            {
                return Results.Ok(new { message = "User registered." });
            }
        }
    }
    catch
    {
        return Results.Conflict(new { message = "User already exists." });
    }
    return Results.Problem("Something went wrong.", statusCode: 500);
}

await app.RunAsync();

