using AssetManagement.Domain.Documents;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Documents;

public class DocumentTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_succeeds_with_valid_data()
    {
        var document = Document.Create(
            Guid.NewGuid(), "Asset", Guid.NewGuid(), "manual.pdf", "application/pdf", 1024, "companyId/Asset/id/manual.pdf",
            Guid.NewGuid(), Now);

        document.FileName.Should().Be("manual.pdf");
        document.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public void Create_rejects_empty_entity_type()
    {
        var act = () => Document.Create(
            Guid.NewGuid(), "  ", Guid.NewGuid(), "manual.pdf", "application/pdf", 1024, "path", Guid.NewGuid(), Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_zero_size()
    {
        var act = () => Document.Create(
            Guid.NewGuid(), "Asset", Guid.NewGuid(), "manual.pdf", "application/pdf", 0, "path", Guid.NewGuid(), Now);

        act.Should().Throw<DomainException>();
    }
}
