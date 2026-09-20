using AssetManagement.Domain.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Requests;

public sealed class InternalRequestConfiguration : IEntityTypeConfiguration<InternalRequest>
{
    public void Configure(EntityTypeBuilder<InternalRequest> builder)
    {
        builder.ToTable("InternalRequests", "requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Type).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Justification).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.CreatedAtUtc).IsRequired();

        builder.HasIndex(r => new { r.CompanyId, r.Status });
        builder.HasIndex(r => r.RequestedByUserId);
        builder.HasIndex(r => r.AssetId);

        builder.HasOne<Domain.Assets.Asset>().WithMany().HasForeignKey(r => r.AssetId).OnDelete(DeleteBehavior.Restrict);
    }
}
