using AssetManagement.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Configuration;

public sealed class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.ToTable("SystemSettings", "configuration");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SenderMailbox).HasMaxLength(320);
        builder.Property(s => s.GraphTenantId).HasMaxLength(64);
        builder.Property(s => s.GraphClientId).HasMaxLength(64);
        builder.Property(s => s.GraphClientSecretCiphertext).HasMaxLength(2000);
        builder.Property(s => s.UpdatedAtUtc).IsRequired();

        builder.HasOne<Domain.Identity.User>()
            .WithMany()
            .HasForeignKey(s => s.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
