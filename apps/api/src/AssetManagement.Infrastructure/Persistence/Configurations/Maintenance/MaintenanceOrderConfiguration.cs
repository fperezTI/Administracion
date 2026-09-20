using AssetManagement.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Maintenance;

public sealed class MaintenanceOrderConfiguration : IEntityTypeConfiguration<MaintenanceOrder>
{
    public void Configure(EntityTypeBuilder<MaintenanceOrder> builder)
    {
        builder.ToTable("MaintenanceOrders", "maintenance");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Folio).IsRequired().HasMaxLength(50);
        builder.Property(o => o.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.Description).IsRequired().HasMaxLength(1000);
        builder.Property(o => o.ResultStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(o => o.ResultNotes).HasMaxLength(2000);
        builder.Property(o => o.CreatedAtUtc).IsRequired();

        builder.HasIndex(o => new { o.CompanyId, o.Folio }).IsUnique();
        builder.HasIndex(o => o.AssetId);

        builder.HasOne<Domain.Assets.Asset>().WithMany().HasForeignKey(o => o.AssetId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.ChecklistResults).WithOne().HasForeignKey(r => r.MaintenanceOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.ChecklistResults).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
