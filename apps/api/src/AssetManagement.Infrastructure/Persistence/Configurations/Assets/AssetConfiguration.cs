using AssetManagement.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Assets;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets", "assets");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CompanyId).IsRequired();
        builder.Property(a => a.AssetCategoryId).IsRequired();
        builder.Property(a => a.InternalFolio).IsRequired().HasMaxLength(50);
        builder.Property(a => a.PatrimonialFolio).HasMaxLength(100);
        builder.Property(a => a.Brand).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Model).IsRequired().HasMaxLength(100);
        builder.Property(a => a.SerialNumber).HasMaxLength(100);
        builder.Property(a => a.Description).HasMaxLength(500);

        builder.Property(a => a.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.PhysicalCondition).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.Property(a => a.AcquisitionCost).HasColumnType("decimal(18,2)");
        builder.Property(a => a.Currency).HasMaxLength(3);
        builder.Property(a => a.Supplier).HasMaxLength(200);
        builder.Property(a => a.Invoice).HasMaxLength(100);
        builder.Property(a => a.PurchaseOrder).HasMaxLength(100);

        builder.Property(a => a.SupportContract).HasMaxLength(100);
        builder.Property(a => a.SupportProvider).HasMaxLength(200);

        builder.Property(a => a.CreatedAtUtc).IsRequired();

        // Optimistic concurrency (pedido §11 "versión de concurrencia") — an EF-only concern, not
        // exposed on the domain entity itself.
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(a => new { a.CompanyId, a.InternalFolio }).IsUnique();
        builder.HasIndex(a => new { a.CompanyId, a.SerialNumber }).IsUnique().HasFilter("[SerialNumber] IS NOT NULL");
        builder.HasIndex(a => new { a.CompanyId, a.Status });
        builder.HasIndex(a => a.AccessoryOfAssetId);

        builder.HasOne<AssetCategory>().WithMany().HasForeignKey(a => a.AssetCategoryId).OnDelete(DeleteBehavior.Restrict);

        // SQL Server refuses SetNull here ("may cause cycles or multiple cascade paths") for a
        // self-referencing FK on a table that already has other cascading children (AssetTag,
        // AssetCustomFieldValue) — Restrict is also the right behavior anyway: assets are never hard-deleted
        // in this app (decommissioned instead, see AssetStateMachine), so this only ever matters for
        // application-level unlinking (UnlinkAssetAccessoryCommand), never a real delete.
        builder.HasOne<Asset>().WithMany().HasForeignKey(a => a.AccessoryOfAssetId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Tag).WithOne().HasForeignKey<AssetTag>(t => t.AssetId).OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.CustomFieldValues).WithOne().HasForeignKey(v => v.AssetId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.CustomFieldValues).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
