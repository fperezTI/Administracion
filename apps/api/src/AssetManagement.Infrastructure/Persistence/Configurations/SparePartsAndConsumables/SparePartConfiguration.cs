using AssetManagement.Domain.SparePartsAndConsumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.SparePartsAndConsumables;

public sealed class SparePartConfiguration : IEntityTypeConfiguration<SparePart>
{
    public void Configure(EntityTypeBuilder<SparePart> builder)
    {
        builder.ToTable("SpareParts", "spareparts");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.PartNumber).HasMaxLength(100);
        builder.Property(p => p.SerialNumber).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        builder.HasIndex(p => new { p.CompanyId, p.SerialNumber }).IsUnique();

        builder.HasMany(p => p.Installations).WithOne().HasForeignKey(i => i.SparePartId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Installations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
