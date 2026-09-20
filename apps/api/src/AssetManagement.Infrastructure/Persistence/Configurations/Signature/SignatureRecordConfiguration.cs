using AssetManagement.Domain.Signature;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Signature;

public sealed class SignatureRecordConfiguration : IEntityTypeConfiguration<SignatureRecord>
{
    public void Configure(EntityTypeBuilder<SignatureRecord> builder)
    {
        builder.ToTable("SignatureRecords", "signature");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.CompanyId).IsRequired();
        builder.Property(s => s.ContextType).IsRequired().HasMaxLength(100);
        builder.Property(s => s.ContextId).IsRequired();
        builder.Property(s => s.SignerUserId).IsRequired();
        builder.Property(s => s.SignerDisplayName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.IpAddress).HasMaxLength(64);
        builder.Property(s => s.UserAgent).HasMaxLength(500);
        builder.Property(s => s.ContentHash).IsRequired().HasMaxLength(128);
        builder.Property(s => s.Mechanism).IsRequired().HasMaxLength(50);
        builder.Property(s => s.CreatedAtUtc).IsRequired();

        builder.HasIndex(s => new { s.ContextType, s.ContextId });
    }
}
