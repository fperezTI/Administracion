using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Organization.Companies;
using AssetManagement.Domain.Organization;
using AssetManagement.Domain.SharedKernel;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Organization;

public class SetCompanyDefaultThemeCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Sets_the_companys_default_theme()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var company = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);
        db.Companies.Add(company);
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new SetCompanyDefaultThemeCommandHandler(db);

        await handler.Handle(new SetCompanyDefaultThemeCommand(company.Id, "corporate-blue"), CancellationToken.None);

        (await db.Companies.SingleAsync()).DefaultThemeCode.Should().Be("corporate-blue");
    }

    [Fact]
    public async Task Rejects_an_unknown_theme_code()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var company = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);
        db.Companies.Add(company);
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new SetCompanyDefaultThemeCommandHandler(db);

        var act = () => handler.Handle(new SetCompanyDefaultThemeCommand(company.Id, "neon-pink"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Throws_not_found_for_an_unknown_company()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var handler = new SetCompanyDefaultThemeCommandHandler(db);

        var act = () => handler.Handle(new SetCompanyDefaultThemeCommand(Guid.NewGuid(), "dark"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
