namespace Comments.Domain.Entities;

public class User
{
    public int Id { get; private set; }
    public string UserName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? HomePage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // for EF Core

    public User(string userName, string email, string? homePage, DateTime createdAtUtc)
    {
        UserName = userName;
        Email = email.Trim().ToLowerInvariant();
        HomePage = string.IsNullOrWhiteSpace(homePage) ? null : homePage.Trim();
        CreatedAt = createdAtUtc;
    }

    public void UpdateHomePage(string? homePage)
    {
        if (!string.IsNullOrWhiteSpace(homePage))
            HomePage = homePage.Trim();
    }
}