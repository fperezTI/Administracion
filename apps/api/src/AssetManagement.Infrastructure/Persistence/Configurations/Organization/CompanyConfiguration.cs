using AssetManagement.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Organization;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies", "organization");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.TradeName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.TaxId).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.TaxId).IsUnique();
        builder.Property(c => c.BaseCurrency).IsRequired().HasMaxLength(3);
        builder.Property(c => c.TimeZone).IsRequired().HasMaxLength(100);
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.CreatedAtUtc).IsRequired();
    }
}
