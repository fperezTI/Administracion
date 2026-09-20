using AssetManagement.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Inventory;

public sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers", "inventory");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.HasIndex(t => t.AssetId);
        builder.HasIndex(t => new { t.FromCompanyId, t.Status });
        builder.HasIndex(t => new { t.ToCompanyId, t.Status });

        builder.HasOne<Domain.Assets.Asset>().WithMany().HasForeignKey(t => t.AssetId).OnDelete(DeleteBehavior.Restrict);
    }
}
