using AssetManagement.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Maintenance;

public sealed class MaintenanceChecklistVersionConfiguration : IEntityTypeConfiguration<MaintenanceChecklistVersion>
{
    public void Configure(EntityTypeBuilder<MaintenanceChecklistVersion> builder)
    {
        builder.ToTable("MaintenanceChecklistVersions", "maintenance");
        builder.HasKey(v => new { v.ChecklistDefinitionId, v.VersionNumber });

        // Primitive collection (EF Core 8+), same pattern as ApprovalFlowDefinition.ApproverRoleIds.
        builder.PrimitiveCollection(v => v.Items);
        builder.Property(v => v.CreatedAtUtc).IsRequired();
    }
}
