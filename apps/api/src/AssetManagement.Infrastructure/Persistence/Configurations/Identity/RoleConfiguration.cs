using AssetManagement.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Identity;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "identity");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.Property(r => r.IsActive).IsRequired();
        builder.Property(r => r.CreatedAtUtc).IsRequired();

        builder.HasMany(r => r.RolePermissions).WithOne().HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(r => r.RolePermissions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
