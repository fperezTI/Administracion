using System.Text;
using AssetManagement.Application.ImportExport;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.ImportExport;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.ImportExport;

public class ImportBatchProcessorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    private static string BuildCsv(params string[] dataRows)
    {
        var header = string.Join(',', AssetImportColumns.FixedColumns);
        return string.Join("\r\n", new[] { header }.Concat(dataRows)) + "\r\n";
    }

    private static async Task<(AssetManagement.Infrastructure.Persistence.AppDbContext Db, ImportBatchProcessor Processor, FakeFileStorage Storage, FakeCurrentCompanyContext CompanyContext, ImportBatch Batch)>
        SetupAsync(AssetCategory category, string csv)
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        var db = InMemoryAppDbContextFactory.Create(companyContext);
        db.AssetCategories.Add(category);

        var storage = new FakeFileStorage();
        var blobPath = $"imports/{companyId}/test.csv";
        await storage.UploadAsync(blobPath, "text/csv", new MemoryStream(Encoding.UTF8.GetBytes(csv)), CancellationToken.None);

        var batch = ImportBatch.Create(companyId, "activos.csv", blobPath, Now, Guid.NewGuid());
        db.ImportBatches.Add(batch);
        await db.SaveChangesAsync();

        var processor = new ImportBatchProcessor(db, storage, new FakeFolioGenerator(), new FakeClock(Now));
        return (db, processor, storage, companyContext, batch);
    }

    private static AssetCategory CreateCategory() => AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);

    [Fact]
    public async Task ValidateAsync_reports_valid_and_invalid_rows_without_creating_any_asset()
    {
        var category = CreateCategory();
        var csv = BuildCsv(
            $"{category.Code},Dell,Latitude 5420,SN-001,,Excellent,,",
            $",Dell,Latitude 5420,SN-002,,Excellent,,"); // missing category code -> invalid
        var (db, processor, _, _, batch) = await SetupAsync(category, csv);

        await processor.ValidateAsync(batch.Id, CancellationToken.None);

        var reloaded = await db.ImportBatches.IgnoreQueryFilters().SingleAsync(b => b.Id == batch.Id);
        reloaded.Status.Should().Be(ImportBatchStatus.Validated);
        reloaded.TotalRows.Should().Be(2);
        reloaded.ValidRows.Should().Be(1);
        reloaded.InvalidRows.Should().Be(1);
        (await db.Assets.IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CommitAsync_ValidRowsOnly_creates_only_the_valid_rows()
    {
        var category = CreateCategory();
        var csv = BuildCsv(
            $"{category.Code},Dell,Latitude 5420,SN-001,,Excellent,,",
            $",Dell,Latitude 5420,SN-002,,Excellent,,");
        var (db, processor, _, _, batch) = await SetupAsync(category, csv);
        await processor.ValidateAsync(batch.Id, CancellationToken.None);

        var validated = await db.ImportBatches.IgnoreQueryFilters().SingleAsync(b => b.Id == batch.Id);
        validated.RequestCommit(ImportCommitMode.ValidRowsOnly, Now, Guid.NewGuid());
        await db.SaveChangesAsync();

        await processor.CommitAsync(batch.Id, CancellationToken.None);

        var reloaded = await db.ImportBatches.IgnoreQueryFilters().SingleAsync(b => b.Id == batch.Id);
        reloaded.Status.Should().Be(ImportBatchStatus.CompletedWithErrors);
        reloaded.SucceededRows.Should().Be(1);
        reloaded.FailedRows.Should().Be(1);
        (await db.Assets.IgnoreQueryFilters().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CommitAsync_AllOrNothing_creates_nothing_when_a_row_becomes_invalid_since_the_preview()
    {
        var category = CreateCategory();
        var csv = BuildCsv($"{category.Code},Dell,Latitude 5420,SN-001,,Excellent,,");
        var (db, processor, _, _, batch) = await SetupAsync(category, csv);
        await processor.ValidateAsync(batch.Id, CancellationToken.None);

        var validated = await db.ImportBatches.IgnoreQueryFilters().SingleAsync(b => b.Id == batch.Id);
        validated.RequestCommit(ImportCommitMode.AllOrNothing, Now, Guid.NewGuid());
        await db.SaveChangesAsync();

        // Simulate drift between preview and confirmation: the category gets deactivated in between.
        var trackedCategory = await db.AssetCategories.SingleAsync(c => c.Id == category.Id);
        trackedCategory.Deactivate();
        await db.SaveChangesAsync();

        await processor.CommitAsync(batch.Id, CancellationToken.None);

        var reloaded = await db.ImportBatches.IgnoreQueryFilters().SingleAsync(b => b.Id == batch.Id);
        reloaded.Status.Should().Be(ImportBatchStatus.Failed);
        (await db.Assets.IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }
}
