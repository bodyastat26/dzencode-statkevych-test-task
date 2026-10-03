using Comments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comments.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.StoredFileName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
    }
}