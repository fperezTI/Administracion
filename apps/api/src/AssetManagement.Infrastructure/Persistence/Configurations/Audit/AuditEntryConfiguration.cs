using AssetManagement.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Audit;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditEntries", "audit");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserDisplayName).HasMaxLength(200);
        builder.Property(a => a.CommandName).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Module).HasMaxLength(50);
        builder.Property(a => a.Action).HasMaxLength(50);
        builder.Property(a => a.DetailsJson).HasColumnType("nvarchar(max)");
        builder.Property(a => a.ErrorMessage).HasMaxLength(2000);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasMaxLength(500);
        builder.Property(a => a.CorrelationId).HasMaxLength(100);
        builder.Property(a => a.OccurredAtUtc).IsRequired();

        builder.HasIndex(a => new { a.CompanyId, a.OccurredAtUtc });
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.CommandName);
    }
}
