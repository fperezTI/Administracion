using AssetManagement.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Inventory;

public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.ToTable("Loans", "inventory");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.CompanyId).IsRequired();
        builder.Property(l => l.AssetId).IsRequired();
        builder.Property(l => l.BorrowerUserId).IsRequired();
        builder.Property(l => l.MovementId).IsRequired();
        builder.Property(l => l.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.CreatedAtUtc).IsRequired();

        builder.HasIndex(l => new { l.CompanyId, l.AssetId });
        builder.HasIndex(l => new { l.CompanyId, l.BorrowerUserId });

        builder.HasOne<Domain.Assets.Asset>().WithMany().HasForeignKey(l => l.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Movement>().WithMany().HasForeignKey(l => l.MovementId).OnDelete(DeleteBehavior.Restrict);
    }
}
