using Comments.Domain.Html;
using Comments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Comments.Infrastructure.Comments;

namespace Comments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<CommentsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IHtmlSanitizer, WhitelistHtmlSanitizer>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICommentService, CommentService>();
        return services;
    }
}