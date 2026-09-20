using AssetManagement.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Assets;

public sealed class AssetTagConfiguration : IEntityTypeConfiguration<AssetTag>
{
    public void Configure(EntityTypeBuilder<AssetTag> builder)
    {
        builder.ToTable("AssetTags", "assets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.Technology).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.PrintCount).IsRequired();
        builder.Property(t => t.IssuedAtUtc).IsRequired();
        builder.Property(t => t.LastPrintedAtUtc).IsRequired();
    }
}
