using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations;

public sealed class FolioSequenceRecordConfiguration : IEntityTypeConfiguration<FolioSequenceRecord>
{
    public void Configure(EntityTypeBuilder<FolioSequenceRecord> builder)
    {
        builder.ToTable("FolioSequences", "organization");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DocumentType).IsRequired().HasMaxLength(50);
        builder.Property(s => s.NextValue).IsRequired();

        builder.HasIndex(s => new { s.CompanyId, s.DocumentType }).IsUnique();
    }
}
