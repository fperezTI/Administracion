using AssetManagement.Domain.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Assets;

public sealed class AssetCustomFieldValueConfiguration : IEntityTypeConfiguration<AssetCustomFieldValue>
{
    public void Configure(EntityTypeBuilder<AssetCustomFieldValue> builder)
    {
        builder.ToTable("AssetCustomFieldValues", "assets");
        builder.HasKey(v => new { v.AssetId, v.CustomFieldDefinitionId });

        builder.Property(v => v.Value).IsRequired().HasMaxLength(2000);

        builder.HasOne<CustomFieldDefinition>().WithMany()
            .HasForeignKey(v => v.CustomFieldDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}
