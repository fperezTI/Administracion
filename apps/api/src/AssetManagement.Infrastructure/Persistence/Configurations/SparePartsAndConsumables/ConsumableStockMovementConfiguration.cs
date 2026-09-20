using AssetManagement.Domain.SparePartsAndConsumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.SparePartsAndConsumables;

public sealed class ConsumableStockMovementConfiguration : IEntityTypeConfiguration<ConsumableStockMovement>
{
    public void Configure(EntityTypeBuilder<ConsumableStockMovement> builder)
    {
        builder.ToTable("ConsumableStockMovements", "spareparts");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Folio).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Direction).IsRequired().HasConversion<string>().HasMaxLength(10);
        builder.Property(m => m.Reason).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Quantity).IsRequired().HasColumnType("decimal(18,4)");
        builder.Property(m => m.Notes).HasMaxLength(1000);
        builder.Property(m => m.CreatedAtUtc).IsRequired();

        builder.HasIndex(m => new { m.CompanyId, m.Folio }).IsUnique();
        builder.HasIndex(m => new { m.ConsumableId, m.WarehouseOrgUnitId, m.OccurredAtUtc });

        builder.HasOne<Consumable>().WithMany().HasForeignKey(m => m.ConsumableId).OnDelete(DeleteBehavior.Restrict);
    }
}
