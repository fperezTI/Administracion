using AssetManagement.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Inventory;

public sealed class MovementConfiguration : IEntityTypeConfiguration<Movement>
{
    public void Configure(EntityTypeBuilder<Movement> builder)
    {
        builder.ToTable("Movements", "inventory");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.CompanyId).IsRequired();
        builder.Property(m => m.AssetId).IsRequired();
        builder.Property(m => m.Type).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(m => m.FolioNumber).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Notes).HasMaxLength(500);
        builder.Property(m => m.CreatedAtUtc).IsRequired();

        // Matches docs/architecture/domain-model.md "Índices previstos": (CompanyId, AssetId,
        // EffectiveDate) for history lookups, (CompanyId, FolioNumber) unique for the foliated document.
        builder.HasIndex(m => new { m.CompanyId, m.AssetId, m.EffectiveAtUtc });
        builder.HasIndex(m => new { m.CompanyId, m.FolioNumber }).IsUnique();

        builder.HasOne<Domain.Assets.Asset>().WithMany().HasForeignKey(m => m.AssetId).OnDelete(DeleteBehavior.Restrict);
    }
}
