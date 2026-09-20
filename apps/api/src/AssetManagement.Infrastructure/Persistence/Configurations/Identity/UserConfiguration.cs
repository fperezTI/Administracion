using AssetManagement.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "identity");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.EntraObjectId).IsRequired();
        builder.HasIndex(u => u.EntraObjectId).IsUnique();

        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.HasIndex(u => u.Email);

        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAtUtc).IsRequired();

        builder.HasMany(u => u.UserRoles).WithOne().HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.UserRoles).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(u => u.UserCompanies).WithOne().HasForeignKey(uc => uc.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.UserCompanies).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
