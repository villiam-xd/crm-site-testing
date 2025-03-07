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
app.MapPost("/api/users/admin", (Delegate)CreateAdmin);
app.MapGet("/api/users/bycompany/{company}", (Delegate)GetEmployesByCompany);

async Task<IResult> Login(HttpContext context, LoginRequest loginRequest)
{
    if (context.Session.GetString("User") != null)
    {
        return Results.BadRequest(new { message = "Someone is already logged in."});
    }
    
    await using var cmd = db.CreateCommand("SELECT * FROM user_with_company WHERE email = @email AND password = @password");
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

async Task<IResult> GetLogin(HttpContext context)
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

async Task<IResult> CreateAdmin(RegisterRequest registerRequest)
{
    await using var cmd = db.CreateCommand("INSERT INTO companys (name) VALUES (@company) RETURNING id, name;");
    cmd.Parameters.AddWithValue("@company", registerRequest.Company);
    
    try
    {
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                await using var cmd2 = db.CreateCommand("INSERT INTO users (username, password, role, email, company) VALUES (@username, @password, 'ADMIN', @email, @company_id);");
                cmd2.Parameters.AddWithValue("@username", registerRequest.Username);
                cmd2.Parameters.AddWithValue("@email", registerRequest.Email);
                cmd2.Parameters.AddWithValue("@password", registerRequest.Password);
                cmd2.Parameters.AddWithValue("@company_id", reader.GetInt32(reader.GetOrdinal("id")));
                
                try
                {
                    await using (var reader2 = await cmd2.ExecuteReaderAsync())
                    {
                        if (reader2.RecordsAffected == 1)
                        {
                            return Results.Ok(new { message = "User registered." });
                        }
                    }
                }
                catch
                {
                    await using var cmd3 = db.CreateCommand("DELETE FROM companys WHERE name = @company;");
                    cmd3.Parameters.AddWithValue("@company", registerRequest.Company);
                    await cmd3.ExecuteNonQueryAsync();
                    
                    return Results.Conflict(new { message = "User already exists." });
                }
            }
        }
    }
    catch
    {
        return Results.Conflict(new { message = "Company already exists." });
    }
    
    return Results.Problem("Something went wrong.", statusCode: 500);
}

async Task<IResult> GetEmployesByCompany(string company)
{
    List<Employe> employes = new List<Employe>();
    await using var cmd = db.CreateCommand("SELECT * FROM user_with_company WHERE company = @company");
    cmd.Parameters.AddWithValue("@company", company);

    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                employes.Add(new Employe(
                    reader.GetInt32(reader.GetOrdinal("user_id")),
                    reader.GetString(reader.GetOrdinal("username")),
                    Enum.Parse<Role>(reader.GetString(reader.GetOrdinal("role")))
                ));
            } 
            return Results.Ok(employes);    
        }
    }
    return Results.NoContent();
}

await app.RunAsync();

