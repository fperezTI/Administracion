using AssetManagement.Domain.Approvals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Approvals;

public sealed class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("ApprovalSteps", "approvals");
        builder.HasKey(s => new { s.ApprovalInstanceId, s.ApproverUserId });

        builder.Property(s => s.Decision).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Comment).HasMaxLength(1000);
        builder.Property(s => s.DecidedAtUtc).IsRequired();
    }
}
