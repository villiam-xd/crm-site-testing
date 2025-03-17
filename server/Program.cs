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

app.MapGet("/", () => "Server is running!");
app.MapPost("/api/login", (Delegate)Login);
app.MapGet("/api/login", (Delegate)GetLogin);
app.MapDelete("/api/login", (Delegate)Logout);
app.MapPost("/api/users/admin", (Delegate)CreateAdmin);
app.MapPost("/api/users/create", (Delegate)CreateEmployee);
app.MapGet("/api/users/bycompany/{companyName}", (Delegate)GetEmployeesByCompany);
app.MapPut("/api/users/{userId}", (Delegate)UpdateUser);
app.MapDelete("/api/users/{userId}", (Delegate)DeleteUser);
app.MapPost("/api/issue/create/{companyId}", (Delegate)CreateIssue);
app.MapGet("/api/form/{companyName}", (Delegate)GetCompanyForm);

async Task<IResult> Login(HttpContext context, LoginRequest loginRequest)
{
    if (context.Session.GetString("User") != null)
    {
        return Results.BadRequest(new { message = "Someone is already logged in."});
    }
    
    await using var cmd = db.CreateCommand("SELECT * FROM users_with_company WHERE email = @email AND password = @password");
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

async Task<IResult> GetEmployeesByCompany(string companyName)
{
    List<Employee> employeesList = new List<Employee>();
    await using var cmd = db.CreateCommand("SELECT * FROM users_with_company WHERE company_name = @company_name");
    cmd.Parameters.AddWithValue("@company_name", companyName);

    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        if (reader.HasRows)
        {
            while (await reader.ReadAsync())
            {
                employeesList.Add(new Employee(
                    reader.GetInt32(reader.GetOrdinal("user_id")),
                    reader.GetString(reader.GetOrdinal("username")),
                    reader.GetString(reader.GetOrdinal("firstname")),
                    reader.GetString(reader.GetOrdinal("lastname")),
                    reader.GetString(reader.GetOrdinal("email")),
                    Enum.Parse<Role>(reader.GetString(reader.GetOrdinal("role")))
                ));
            } 
            return Results.Ok(new { employees = employeesList });    
        }
    }
    return Results.NoContent();
}

async Task<IResult> CreateEmployee(HttpContext context, CreateEmployeeRequest createEmployeeRequest)
{
    
    if (context.Session.GetString("User") == null)
    {
        return Results.Unauthorized();
    }
    
    var user = JsonSerializer.Deserialize<User>(context.Session.GetString("User"));
    if (user.Role != Role.ADMIN)
    {
        Results.Conflict(new { message = "You dont have access to this" });
    }
    
    await using var cmd = db.CreateCommand("SELECT * FROM companys WHERE name = @company");
    cmd.Parameters.AddWithValue("@company", user.Company);
    
    try
    {
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                await using var cmd2 = db.CreateCommand("INSERT INTO users (firstname, lastname, username, password, role, email, company) VALUES (@firstname, @lastname, @username, @password, @role::role, @email, @company_id);");
                cmd2.Parameters.AddWithValue("@firstname", createEmployeeRequest.Firstname);
                cmd2.Parameters.AddWithValue("@lastname", createEmployeeRequest.Lastname);
                cmd2.Parameters.AddWithValue("@username", createEmployeeRequest.Firstname + "_" + createEmployeeRequest.Lastname);
                cmd2.Parameters.AddWithValue("@password", createEmployeeRequest.Password);
                cmd2.Parameters.AddWithValue("@role", Enum.Parse<Role>(createEmployeeRequest.Role).ToString());
                cmd2.Parameters.AddWithValue("@email", createEmployeeRequest.Email);
                cmd2.Parameters.AddWithValue("@company_id", reader.GetInt32(reader.GetOrdinal("id")));
                
                try
                {
                    int rowsAffected = await cmd2.ExecuteNonQueryAsync();
                    if (rowsAffected == 1)
                    {
                        return Results.Ok(new { message = "User registered." });
                    }
                    else
                    {
                        return Results.Conflict(new { message = "Query executed but something went wrong." });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return Results.Conflict(new { message = "User already exists" });
                }
            }
        }
    }
    catch
    {
        return Results.NotFound(new { message = "Company not found." });
    }
    
    return Results.Problem("Something went wrong.", statusCode: 500);
}

async Task<IResult> UpdateUser(int userId, HttpContext context, UpdateUserRequest updateUserRequest)
{
    if (context.Session.GetString("User") == null)
    {
        return Results.Unauthorized();
    }
    
    var user = JsonSerializer.Deserialize<User>(context.Session.GetString("User"));
    if (user.Role != Role.ADMIN)
    {
        if (user.Id != userId)
        {        
            Results.Conflict(new { message = "You dont have access to this" });
        }
    }

    await using var cmd = db.CreateCommand("UPDATE users SET firstname = @firstname, lastname = @lastname, email = @email, role = @role::role WHERE id = @user_id");
    cmd.Parameters.AddWithValue("@firstname", updateUserRequest.Firstname);
    cmd.Parameters.AddWithValue("@lastname", updateUserRequest.Lastname);
    cmd.Parameters.AddWithValue("@email", updateUserRequest.Email);
    cmd.Parameters.AddWithValue("@role", Enum.Parse<Role>(updateUserRequest.Role).ToString());
    cmd.Parameters.AddWithValue("@user_id", userId);

    try
    {
        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        if (rowsAffected == 1)
        {
            return Results.Ok(new { message = "User updated successfully." });
        }
        else
        {
            return Results.Conflict(new { message = "Query executed but something went wrong." });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Conflict(new { message = "User update failed." });
    }
}

async Task<IResult> DeleteUser(int userId, HttpContext context)
{
    if (context.Session.GetString("User") == null)
    {
        return Results.Unauthorized();
    }
    
    var user = JsonSerializer.Deserialize<User>(context.Session.GetString("User"));
    if (user.Role != Role.ADMIN)
    {
        Results.Conflict(new { message = "You dont have access to this" });
    }
    
    await using var cmd = db.CreateCommand("DELETE FROM users WHERE id = @user_id");
    cmd.Parameters.AddWithValue("@user_id", userId);
    
    try
    {
        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        if (rowsAffected == 1)
        {
            return Results.Ok(new { message = "User was deleted successfully." });
        }
        else if (rowsAffected == 0)
        {
            return Results.NotFound(new { message = "No user was found." });
        }
        else
        {
            return Results.Conflict(new { message = "Query executed but something went wrong." });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.Conflict(new { message = "Query was not executed." });
    }
}

async Task<IResult> CreateIssue(int companyId, CreateIssueRequest createIssueRequest)
{
    await using var cmd = db.CreateCommand("INSERT INTO issues (company_id, customer_email, title, subject, state) VALUES (@company_id, @customer_email, @title, @subject, 'NEW') RETURNING id;");
    cmd.Parameters.AddWithValue("@company_id", companyId);
    cmd.Parameters.AddWithValue("@customer_email", createIssueRequest.Email);
    cmd.Parameters.AddWithValue("@title", createIssueRequest.Title);
    cmd.Parameters.AddWithValue("@subject", createIssueRequest.Subject);

    await using (var reader = await cmd.ExecuteReaderAsync())
    {
        if (await reader.ReadAsync())
        {
            await using var cmd2 = db.CreateCommand("INSERT INTO messages (issue_id, message, sender) VALUES (@issue_id, @message, 'CUSTOMER');");
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
                await using var cmd3 = db.CreateCommand("DELETE FROM issues WHERE id = @issue_id;");
                cmd3.Parameters.AddWithValue("@issue_id", reader.GetInt32(reader.GetOrdinal("id")));
                await cmd3.ExecuteNonQueryAsync();
                    
                return Results.Conflict(new { message = "Issue was created, but something went wrong during the process, so the issue has been deleted." });
            }
        }
    }
    
    return Results.Problem("Something went wrong.", statusCode: 500);
}

async Task<IResult> GetCompanyForm(string companyName)
{
    await using var cmd = db.CreateCommand("SELECT * FROM companys WHERE name = @company_name");
    cmd.Parameters.AddWithValue("@company_name", companyName);
    
    try
    {
        var reader = await cmd.ExecuteScalarAsync();
        if (reader is not null)
        {
            await using var cmd2 = db.CreateCommand("SELECT name FROM subjects WHERE company_id = @company_id");
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
                    
                return Results.Ok(new {company_info = new CompanyForm((Int32) reader, companyName, formSubjects)});
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

await app.RunAsync();
