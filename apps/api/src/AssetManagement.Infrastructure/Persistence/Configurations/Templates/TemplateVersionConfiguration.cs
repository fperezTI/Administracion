using AssetManagement.Domain.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Templates;

public sealed class TemplateVersionConfiguration : IEntityTypeConfiguration<TemplateVersion>
{
    public void Configure(EntityTypeBuilder<TemplateVersion> builder)
    {
        builder.ToTable("TemplateVersions", "templates");
        builder.HasKey(v => new { v.TemplateId, v.VersionNumber });

        builder.Property(v => v.Content).IsRequired().HasMaxLength(10000);
        builder.Property(v => v.CreatedAtUtc).IsRequired();
    }
}
