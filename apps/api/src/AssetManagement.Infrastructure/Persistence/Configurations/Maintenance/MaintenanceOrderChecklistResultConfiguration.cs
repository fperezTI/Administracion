using AssetManagement.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Maintenance;

public sealed class MaintenanceOrderChecklistResultConfiguration : IEntityTypeConfiguration<MaintenanceOrderChecklistResult>
{
    public void Configure(EntityTypeBuilder<MaintenanceOrderChecklistResult> builder)
    {
        builder.ToTable("MaintenanceOrderChecklistResults", "maintenance");
        builder.HasKey(r => new { r.MaintenanceOrderId, r.ItemIndex });

        builder.Property(r => r.ItemText).IsRequired().HasMaxLength(500);
        builder.Property(r => r.Notes).HasMaxLength(500);
    }
}
