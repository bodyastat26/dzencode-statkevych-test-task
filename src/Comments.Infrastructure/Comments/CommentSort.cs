namespace Comments.Infrastructure.Comments;

public enum CommentSortField { CreatedAt, UserName, Email }

public sealed record CommentSort(CommentSortField Field, bool Descending)
{
    // default: newest first (LIFO)
    public static readonly CommentSort Default = new(CommentSortField.CreatedAt, true);

    /// Whitelist parsing: user input never goes into SQL directly.
    public static CommentSort Parse(string? sortBy, string? sortDir)
    {
        var field = sortBy?.ToLowerInvariant() switch
        {
            "username" => CommentSortField.UserName,
            "email" => CommentSortField.Email,
            _ => CommentSortField.CreatedAt
        };
        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        return new CommentSort(field, descending);
    }
}