using AssetManagement.Domain.SparePartsAndConsumables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.SparePartsAndConsumables;

public sealed class SparePartInstallationConfiguration : IEntityTypeConfiguration<SparePartInstallation>
{
    public void Configure(EntityTypeBuilder<SparePartInstallation> builder)
    {
        builder.ToTable("SparePartInstallations", "spareparts");
        builder.HasKey(i => i.Id);
        // Client always assigns Id (SparePartInstallation.Create) — without this, EF Core's graph-fixup
        // can mark a brand-new child added to an already-tracked SparePart's loaded collection as
        // Modified instead of Added, because the key already has a non-default value when discovered.
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.InstalledAtUtc).IsRequired();

        builder.HasIndex(i => i.AssetId);

        builder.HasOne<Domain.Assets.Asset>().WithMany().HasForeignKey(i => i.AssetId).OnDelete(DeleteBehavior.Restrict);
    }
}
