using AssetManagement.Domain.Approvals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Approvals;

public sealed class ApprovalFlowDefinitionConfiguration : IEntityTypeConfiguration<ApprovalFlowDefinition>
{
    public void Configure(EntityTypeBuilder<ApprovalFlowDefinition> builder)
    {
        builder.ToTable("ApprovalFlowDefinitions", "approvals");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Key).IsRequired().HasMaxLength(100);
        builder.Property(f => f.Mode).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.CreatedAtUtc).IsRequired();

        // Primitive collection (EF Core 8+) — an ordered list of role ids, meaningful only when
        // Mode == Sequential; stored as JSON by SQL Server's provider by default.
        builder.PrimitiveCollection(f => f.ApproverRoleIds);

        builder.HasIndex(f => new { f.Key, f.CompanyId, f.IsActive });
    }
}
