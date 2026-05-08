using Client.Data;
using Microsoft.EntityFrameworkCore;
using Temporalio.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite("Data Source=shop.db"));

builder.Services.AddSingleton<TemporalClient>(sp =>
    TemporalClient.ConnectAsync(new()
    {
        TargetHost = builder.Configuration["Temporal:Host"] ?? "localhost:7233",
        Namespace = builder.Configuration["Temporal:Namespace"] ?? "default"
    }).GetAwaiter().GetResult());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseStaticFiles();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
