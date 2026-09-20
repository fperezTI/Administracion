using AssetManagement.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserCompanyConfiguration : IEntityTypeConfiguration<UserCompany>
{
    public void Configure(EntityTypeBuilder<UserCompany> builder)
    {
        builder.ToTable("UserCompanies", "identity");
        builder.HasKey(uc => new { uc.UserId, uc.CompanyId });

        builder.Property(uc => uc.GrantedAtUtc).IsRequired();
    }
}
