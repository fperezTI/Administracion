using AssetManagement.Domain.SparePartsAndConsumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.SparePartsAndConsumables;

public sealed class ConsumableConfiguration : IEntityTypeConfiguration<Consumable>
{
    public void Configure(EntityTypeBuilder<Consumable> builder)
    {
        builder.ToTable("Consumables", "spareparts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Sku).HasMaxLength(100);
        builder.Property(c => c.UnitOfMeasure).IsRequired().HasMaxLength(50);
        builder.Property(c => c.MinimumStock).HasColumnType("decimal(18,4)");
        builder.Property(c => c.CurrentStock).IsRequired().HasColumnType("decimal(18,4)");
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasIndex(c => new { c.CompanyId, c.Name });
    }
}
