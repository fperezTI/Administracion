using AssetManagement.Application.Common.Assets;
using AssetManagement.Domain.Assets;
using AssetManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Assets;

public sealed class AssetCategoryConfiguration : IEntityTypeConfiguration<AssetCategory>
{
    public void Configure(EntityTypeBuilder<AssetCategory> builder)
    {
        builder.ToTable("AssetCategories", "assets");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.DefaultIdentificationTechnology).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasMany(c => c.CustomFields).WithOne().HasForeignKey(f => f.AssetCategoryId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.CustomFields).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(AssetCategoryCatalog.SeedEntries.Select(entry => new
        {
            Id = DeterministicGuid.Create($"assetcategory:{entry.Code}"),
            entry.Name,
            entry.Code,
            IsActive = true,
            DefaultIdentificationTechnology = entry.DefaultTechnology,
            CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        }));
    }
}
