using Comments.Domain.Html;
using Comments.Infrastructure.Caching;
using Comments.Infrastructure.Captcha;
using Comments.Infrastructure.Comments;
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

        services.AddDbContext<CommentsDbContext>(options => options.UseNpgsql(postgres));
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redis;
            options.InstanceName = "app:";
        });

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IHtmlSanitizer, WhitelistHtmlSanitizer>();

        services.AddSingleton<CommentsCache>();
        services.AddScoped<CommentService>();
        services.AddScoped<ICommentService, CachedCommentService>();

        services.AddSingleton<CaptchaImageRenderer>();
        services.AddSingleton<ICaptchaService, CaptchaService>();

        return services;
    }
}