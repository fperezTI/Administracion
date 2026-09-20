using AssetManagement.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles", "identity");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.Property(ur => ur.AssignedAtUtc).IsRequired();

        builder.HasOne(ur => ur.Role).WithMany().HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
