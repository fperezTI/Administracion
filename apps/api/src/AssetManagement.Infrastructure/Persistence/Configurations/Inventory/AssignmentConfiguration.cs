using AssetManagement.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Inventory;

public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments", "inventory");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.CompanyId).IsRequired();
        builder.Property(a => a.AssetId).IsRequired();
        builder.Property(a => a.AssignedToUserId).IsRequired();
        builder.Property(a => a.MovementId).IsRequired();
        builder.Property(a => a.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.CreatedAtUtc).IsRequired();

        builder.HasIndex(a => new { a.CompanyId, a.AssetId });
        builder.HasIndex(a => new { a.CompanyId, a.AssignedToUserId });

        builder.HasOne<Domain.Assets.Asset>().WithMany().HasForeignKey(a => a.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Movement>().WithMany().HasForeignKey(a => a.MovementId).OnDelete(DeleteBehavior.Restrict);
    }
}
