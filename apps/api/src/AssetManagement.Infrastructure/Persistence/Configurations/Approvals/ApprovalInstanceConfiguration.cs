using AssetManagement.Domain.Approvals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Approvals;

public sealed class ApprovalInstanceConfiguration : IEntityTypeConfiguration<ApprovalInstance>
{
    public void Configure(EntityTypeBuilder<ApprovalInstance> builder)
    {
        builder.ToTable("ApprovalInstances", "approvals");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.CompanyId).IsRequired();
        builder.Property(i => i.ContextType).IsRequired().HasMaxLength(100);
        builder.Property(i => i.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Mode).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Comment).HasMaxLength(1000);
        builder.Property(i => i.CreatedAtUtc).IsRequired();

        builder.PrimitiveCollection(i => i.ApproverRoleIds);

        builder.HasIndex(i => new { i.CompanyId, i.Status });
        builder.HasIndex(i => new { i.ContextType, i.ContextId });

        builder.HasMany(i => i.Steps).WithOne().HasForeignKey(s => s.ApprovalInstanceId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
