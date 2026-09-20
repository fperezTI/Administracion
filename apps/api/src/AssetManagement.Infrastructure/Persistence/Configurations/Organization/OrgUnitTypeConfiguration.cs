using AssetManagement.Application.Common.Organization;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Organization;

public sealed class OrgUnitTypeConfiguration : IEntityTypeConfiguration<OrgUnitType>
{
    public void Configure(EntityTypeBuilder<OrgUnitType> builder)
    {
        builder.ToTable("OrgUnitTypes", "organization");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.IsActive).IsRequired();

        builder.HasData(OrgUnitTypeCatalog.SeedEntries.Select(entry => new
        {
            Id = DeterministicGuid.Create($"orgunittype:{entry.Code}"),
            entry.Code,
            entry.Name,
            IsActive = true,
        }));
    }
}
