using AssetManagement.Domain.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Templates;

public sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("Templates", "templates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Key).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.HasIndex(t => t.Key).IsUnique();

        builder.HasMany(t => t.Versions).WithOne().HasForeignKey(v => v.TemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
