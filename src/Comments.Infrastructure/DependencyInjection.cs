using Comments.Domain.Html;
using Comments.Infrastructure.Caching;
using Comments.Infrastructure.Captcha;
using Comments.Infrastructure.Comments;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Messaging;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgres = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        var redis = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");
        var rabbitMq = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("Connection string 'RabbitMq' is not configured.");
        var uploadsPath = configuration["Storage:UploadsPath"] ?? "uploads";

        services.AddDbContext<CommentsDbContext>(options => options.UseNpgsql(postgres));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redis;
            options.InstanceName = "app:";
        });

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHtmlSanitizer, WhitelistHtmlSanitizer>();

        // files
        services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(uploadsPath));
        services.AddSingleton<ImageProcessor>();

        // queue
        services.AddSingleton(_ => new RabbitMqConnection(rabbitMq));
        services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        services.AddHostedService<ImageResizeConsumer>();

        // comments + cache
        services.AddSingleton<CommentsCache>();
        services.AddScoped<CommentService>();
        services.AddScoped<ICommentService, CachedCommentService>();

        // captcha
        services.AddSingleton<CaptchaImageRenderer>();
        services.AddSingleton<ICaptchaService, CaptchaService>();

        return services;
    }
}