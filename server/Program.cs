using Npgsql;
using server;
using server.api;

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

String url = "/api";

new Test(app, db, url);
new Login(app, db, url);
new Users(app, db, url);
new Issues(app, db, url);


await app.RunAsync();
