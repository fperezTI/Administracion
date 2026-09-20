using System.Text;
using AssetManagement.Application.ImportExport;
using AssetManagement.Domain.Assets;
using AssetManagement.UnitTests.TestSupport;
using ClosedXML.Excel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.ImportExport;

public class ExportAssetsQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    private static async Task<(AssetManagement.Infrastructure.Persistence.AppDbContext Db, FakeCurrentCompanyContext CompanyContext, Guid CompanyId)> SeedAssetsAsync(int count)
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        var db = InMemoryAppDbContextFactory.Create(companyContext);

        var category = AssetCategory.Create("Laptops", "LAPTOP", IdentificationTechnology.Qr, Now);
        db.AssetCategories.Add(category);

        for (var i = 0; i < count; i++)
        {
            var asset = Asset.Create(
                companyId, category.Id, $"ASSET-{i:D6}", "Dell", "Latitude 5420", $"SN-{i:D3}", null,
                PhysicalCondition.Excellent, Now, null);
            asset.IssueTag(Guid.NewGuid().ToString("N"), IdentificationTechnology.Qr, Now);
            db.Assets.Add(asset);
        }

        await db.SaveChangesAsync();
        return (db, companyContext, companyId);
    }

    [Fact]
    public async Task Xlsx_export_contains_one_row_per_asset_plus_a_header()
    {
        var (db, companyContext, companyId) = await SeedAssetsAsync(3);
        var handler = new ExportAssetsQueryHandler(db, companyContext, new FakeClock(Now));

        var result = await handler.Handle(new ExportAssetsQuery(companyId, ExportFileFormat.Xlsx), CancellationToken.None);

        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        using var workbook = new XLWorkbook(new MemoryStream(result.Content));
        var worksheet = workbook.Worksheet(1);
        worksheet.Cell(1, 1).GetString().Should().Be("Folio");
        worksheet.RowsUsed().Count().Should().Be(4); // 1 header + 3 assets
    }

    [Fact]
    public async Task Pdf_export_produces_a_non_empty_valid_pdf()
    {
        var (db, companyContext, companyId) = await SeedAssetsAsync(2);
        var handler = new ExportAssetsQueryHandler(db, companyContext, new FakeClock(Now));

        var result = await handler.Handle(new ExportAssetsQuery(companyId, ExportFileFormat.Pdf), CancellationToken.None);

        result.ContentType.Should().Be("application/pdf");
        result.Content.Should().NotBeEmpty();
        Encoding.ASCII.GetString(result.Content, 0, 4).Should().Be("%PDF");
    }
}
