using AssetManagement.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications", "notifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).IsRequired().HasMaxLength(50);
        builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Body).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.CreatedAtUtc).IsRequired();

        builder.HasIndex(n => new { n.UserId, n.IsRead });
    }
}
