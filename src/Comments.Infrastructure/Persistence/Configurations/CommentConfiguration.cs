using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Text).IsRequired();
        builder.Property(c => c.IpAddress).HasMaxLength(45).IsRequired(); // IPv6 max length
        builder.Property(c => c.UserAgent).HasMaxLength(512);
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // cascade tree: a comment has a parent and replies
        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.Replies).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(c => c.Attachment)
            .WithOne(a => a.Comment)
            .HasForeignKey<Attachment>(a => a.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        // top-level list (ParentId IS NULL) sorted by date
        builder.HasIndex(c => new { c.ParentId, c.CreatedAt });
    }
}