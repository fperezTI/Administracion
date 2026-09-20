using AssetManagement.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Assets;

public sealed class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.ToTable("CustomFieldDefinitions", "assets");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name).IsRequired().HasMaxLength(100);
        builder.Property(f => f.Code).IsRequired().HasMaxLength(50);
        builder.Property(f => f.DataType).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.IsRequired).IsRequired();
        builder.Property(f => f.SortOrder).IsRequired();
        builder.Property(f => f.Options).HasMaxLength(1000);

        builder.HasIndex(f => new { f.AssetCategoryId, f.Code }).IsUnique();
    }
}
