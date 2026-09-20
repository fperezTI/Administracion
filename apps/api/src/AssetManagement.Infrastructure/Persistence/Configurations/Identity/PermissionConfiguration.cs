using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using AssetManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Identity;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", "identity");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Module).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Action).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(300);
        builder.HasIndex(p => new { p.Module, p.Action }).IsUnique();
        builder.Ignore(p => p.Code);

        builder.HasData(PermissionCatalog.SeedEntries.Select(entry => new
        {
            Id = DeterministicGuid.Create($"permission:{entry.Module}.{entry.Action}"),
            entry.Module,
            entry.Action,
            entry.Description,
        }));
    }
}
