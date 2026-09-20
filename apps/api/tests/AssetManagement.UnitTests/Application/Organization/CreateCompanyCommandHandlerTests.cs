using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Organization.Companies;
using AssetManagement.Domain.Organization;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Organization;

public class CreateCompanyCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Rejects_a_51st_company()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        for (var i = 0; i < 50; i++)
        {
            db.Companies.Add(Company.Create($"Empresa {i} S.A.", $"Empresa {i}", $"TAX-{i}", "MXN", "America/Mexico_City", Now));
        }

        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new CreateCompanyCommandHandler(db, new FakeClock(Now));

        var act = () => handler.Handle(
            new CreateCompanyCommand("Empresa 51 S.A.", "Empresa 51", "TAX-51", "MXN", "America/Mexico_City"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Creates_the_company_when_under_the_limit()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var handler = new CreateCompanyCommandHandler(db, new FakeClock(Now));

        var id = await handler.Handle(
            new CreateCompanyCommand("Empresa Uno S.A.", "Empresa Uno", "TAX-1", "MXN", "America/Mexico_City"),
            CancellationToken.None);

        id.Should().NotBeEmpty();
    }
}
