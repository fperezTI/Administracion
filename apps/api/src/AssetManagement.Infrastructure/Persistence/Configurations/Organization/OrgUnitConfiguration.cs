using AssetManagement.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Organization;

public sealed class OrgUnitConfiguration : IEntityTypeConfiguration<OrgUnit>
{
    public void Configure(EntityTypeBuilder<OrgUnit> builder)
    {
        builder.ToTable("OrgUnits", "organization");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CompanyId).IsRequired();
        builder.Property(o => o.OrgUnitTypeId).IsRequired();
        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Code).IsRequired().HasMaxLength(50);
        builder.Property(o => o.IsActive).IsRequired();
        builder.Property(o => o.CreatedAtUtc).IsRequired();

        builder.HasIndex(o => new { o.CompanyId, o.Code }).IsUnique();
        builder.HasIndex(o => new { o.CompanyId, o.IsActive });

        builder.HasOne<OrgUnitType>().WithMany().HasForeignKey(o => o.OrgUnitTypeId).OnDelete(DeleteBehavior.Restrict);

        // Self-referencing hierarchy (ADR 0002). Restrict avoids SQL Server's "multiple cascade paths"
        // error and matches the domain rule that a unit's children must be reparented explicitly.
        builder.HasOne<OrgUnit>().WithMany().HasForeignKey(o => o.ParentOrgUnitId).OnDelete(DeleteBehavior.Restrict);
    }
}
