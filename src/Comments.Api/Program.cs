using Comments.Api.Realtime;
using Comments.Infrastructure;
using Comments.Infrastructure.Events;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// WebSocket (SignalR) + its event handlers
builder.Services.AddSignalR();
builder.Services.AddScoped<IEventHandler<CommentCreatedEvent>, SignalRBroadcastHandler>();
builder.Services.AddScoped<IEventHandler<AttachmentProcessedEvent>, SignalRBroadcastHandler>();

// trust X-Forwarded-For from nginx (the API is reachable only through it inside Docker),
// so comments store the real client IP instead of the proxy's
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// must run first, before anything reads the client IP
app.UseForwardedHeaders();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CommentsDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// serve uploaded files at /uploads/...
var storage = app.Services.GetRequiredService<IFileStorage>();
var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".txt"] = "text/plain; charset=utf-8";
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storage.RootPath),
    RequestPath = "/uploads",
    ContentTypeProvider = contentTypes,
    OnPrepareResponse = ctx => ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});

app.MapControllers();
app.MapHub<CommentsHub>("/hubs/comments");
app.Run();