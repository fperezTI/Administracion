using AssetManagement.Domain.ImportExport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssetManagement.Infrastructure.Persistence.Configurations.ImportExport;

public sealed class ImportBatchConfiguration : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("ImportBatches", "importexport");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.FileName).IsRequired().HasMaxLength(260);
        builder.Property(b => b.BlobPath).IsRequired().HasMaxLength(1000);
        builder.Property(b => b.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(b => b.CommitMode).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.ReportJson).HasColumnType("nvarchar(max)");
        builder.Property(b => b.ErrorMessage).HasMaxLength(2000);
        builder.Property(b => b.CreatedAtUtc).IsRequired();

        builder.HasIndex(b => new { b.CompanyId, b.CreatedAtUtc });
    }
}
