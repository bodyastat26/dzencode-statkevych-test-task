using Comments.Domain.Entities;
using Comments.Domain.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.UserName).HasMaxLength(CommentRules.UserNameMaxLength).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(CommentRules.EmailMaxLength).IsRequired();
        builder.Property(u => u.HomePage).HasMaxLength(CommentRules.HomePageMaxLength);
        builder.Property(u => u.CreatedAt).IsRequired();

        // one "user" = unique pair of e-mail + name
        builder.HasIndex(u => new { u.Email, u.UserName }).IsUnique();
        // for sorting top-level comments by name
        builder.HasIndex(u => u.UserName);
    }
}