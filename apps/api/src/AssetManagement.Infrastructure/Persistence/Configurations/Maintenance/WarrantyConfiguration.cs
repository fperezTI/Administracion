using AssetManagement.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Maintenance;

public sealed class WarrantyConfiguration : IEntityTypeConfiguration<Warranty>
{
    public void Configure(EntityTypeBuilder<Warranty> builder)
    {
        builder.ToTable("Warranties", "maintenance");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(w => w.Provider).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Terms).HasMaxLength(2000);
        builder.Property(w => w.CreatedAtUtc).IsRequired();

        builder.HasIndex(w => w.AssetId);

        builder.HasOne<Domain.Assets.Asset>().WithMany().HasForeignKey(w => w.AssetId).OnDelete(DeleteBehavior.Restrict);
    }
}
