using AssetManagement.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Identity;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions", "identity");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne<Permission>().WithMany().HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Restrict);
    }
}
