using AssetManagement.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Documents;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents", "documents");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.EntityType).IsRequired().HasMaxLength(50);
        builder.Property(d => d.FileName).IsRequired().HasMaxLength(260);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(d => d.BlobPath).IsRequired().HasMaxLength(1000);
        builder.Property(d => d.CreatedAtUtc).IsRequired();

        builder.HasIndex(d => new { d.EntityType, d.EntityId });
    }
}
