using AssetManagement.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Maintenance;

public sealed class MaintenanceChecklistDefinitionConfiguration : IEntityTypeConfiguration<MaintenanceChecklistDefinition>
{
    public void Configure(EntityTypeBuilder<MaintenanceChecklistDefinition> builder)
    {
        builder.ToTable("MaintenanceChecklistDefinitions", "maintenance");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Key).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.CreatedAtUtc).IsRequired();

        builder.HasIndex(c => c.Key).IsUnique();

        builder.HasMany(c => c.Versions).WithOne().HasForeignKey(v => v.ChecklistDefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
