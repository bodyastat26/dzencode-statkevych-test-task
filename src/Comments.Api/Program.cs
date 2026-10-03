using Comments.Infrastructure;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
                       ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// apply migrations automatically on startup (convenient for Docker)
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CommentsDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();
app.Run();