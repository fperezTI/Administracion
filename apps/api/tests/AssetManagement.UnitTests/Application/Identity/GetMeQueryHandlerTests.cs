using AssetManagement.Application.Identity;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.Identity;

public class GetMeQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task User_without_a_preference_inherits_the_active_companys_theme()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var company = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);
        company.SetDefaultTheme("corporate-blue");
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        user.GrantCompanyAccess(company.Id, Now);
        db.Companies.Add(company);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id, PermissionCodes = [] };
        var currentCompany = new FakeCurrentCompanyContext { CompanyId = company.Id };
        var handler = new GetMeQueryHandler(db, currentUser, currentCompany);

        var result = await handler.Handle(new GetMeQuery(), CancellationToken.None);

        result.ThemePreference.Should().BeNull();
        result.EffectiveTheme.Should().Be("corporate-blue");
    }

    [Fact]
    public async Task User_with_a_personal_preference_uses_it_regardless_of_the_companys_default()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var company = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);
        company.SetDefaultTheme("corporate-blue");
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        user.SetThemePreference("dark");
        user.GrantCompanyAccess(company.Id, Now);
        db.Companies.Add(company);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id, PermissionCodes = [] };
        var currentCompany = new FakeCurrentCompanyContext { CompanyId = company.Id };
        var handler = new GetMeQueryHandler(db, currentUser, currentCompany);

        var result = await handler.Handle(new GetMeQuery(), CancellationToken.None);

        result.ThemePreference.Should().Be("dark");
        result.EffectiveTheme.Should().Be("dark");
    }

    [Fact]
    public async Task Inherited_theme_changes_when_the_active_company_changes()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var companyA = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);
        companyA.SetDefaultTheme("corporate-blue");
        var companyB = Company.Create("Beta S.A.", "Beta", "TAX-2", "MXN", "America/Mexico_City", Now);
        companyB.SetDefaultTheme("executive-gray");
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        user.GrantCompanyAccess(companyA.Id, Now);
        user.GrantCompanyAccess(companyB.Id, Now);
        db.Companies.AddRange(companyA, companyB);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id, PermissionCodes = [] };

        var handlerOnA = new GetMeQueryHandler(db, currentUser, new FakeCurrentCompanyContext { CompanyId = companyA.Id });
        var resultOnA = await handlerOnA.Handle(new GetMeQuery(), CancellationToken.None);
        resultOnA.EffectiveTheme.Should().Be("corporate-blue");

        var handlerOnB = new GetMeQueryHandler(db, currentUser, new FakeCurrentCompanyContext { CompanyId = companyB.Id });
        var resultOnB = await handlerOnB.Handle(new GetMeQuery(), CancellationToken.None);
        resultOnB.EffectiveTheme.Should().Be("executive-gray");
    }

    [Fact]
    public async Task Personal_preference_stays_the_same_across_companies()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var companyA = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);
        companyA.SetDefaultTheme("corporate-blue");
        var companyB = Company.Create("Beta S.A.", "Beta", "TAX-2", "MXN", "America/Mexico_City", Now);
        companyB.SetDefaultTheme("executive-gray");
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        user.SetThemePreference("high-contrast");
        user.GrantCompanyAccess(companyA.Id, Now);
        user.GrantCompanyAccess(companyB.Id, Now);
        db.Companies.AddRange(companyA, companyB);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id, PermissionCodes = [] };

        var handlerOnA = new GetMeQueryHandler(db, currentUser, new FakeCurrentCompanyContext { CompanyId = companyA.Id });
        (await handlerOnA.Handle(new GetMeQuery(), CancellationToken.None)).EffectiveTheme.Should().Be("high-contrast");

        var handlerOnB = new GetMeQueryHandler(db, currentUser, new FakeCurrentCompanyContext { CompanyId = companyB.Id });
        (await handlerOnB.Handle(new GetMeQuery(), CancellationToken.None)).EffectiveTheme.Should().Be("high-contrast");
    }

    [Fact]
    public async Task Falls_back_to_a_safe_theme_when_the_company_default_is_corrupted()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var company = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);
        // Simulates pre-existing/corrupted data bypassing the domain's own validation (e.g. a direct
        // SQL update) — GetMeQuery must never surface an invalid code to the frontend.
        typeof(Company).GetProperty(nameof(Company.DefaultThemeCode))!.SetValue(company, "not-a-real-theme");
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        user.GrantCompanyAccess(company.Id, Now);
        db.Companies.Add(company);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id, PermissionCodes = [] };
        var currentCompany = new FakeCurrentCompanyContext { CompanyId = company.Id };
        var handler = new GetMeQueryHandler(db, currentUser, currentCompany);

        var result = await handler.Handle(new GetMeQuery(), CancellationToken.None);

        result.EffectiveTheme.Should().Be("light");
    }

    [Fact]
    public async Task Inherits_the_first_companys_theme_when_no_active_company_is_explicitly_selected()
    {
        // No frontend caller sends X-Active-Company-Id yet (docs/multi-company.md) — until one does,
        // a user with at least one company should still see a meaningful inherited theme instead of
        // always falling back to "light".
        using var db = InMemoryAppDbContextFactory.Create();
        var company = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);
        company.SetDefaultTheme("executive-gray");
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        user.GrantCompanyAccess(company.Id, Now);
        db.Companies.Add(company);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id, PermissionCodes = [] };
        var currentCompany = new FakeCurrentCompanyContext { CompanyId = null };
        var handler = new GetMeQueryHandler(db, currentUser, currentCompany);

        var result = await handler.Handle(new GetMeQuery(), CancellationToken.None);

        result.EffectiveTheme.Should().Be("executive-gray");
    }

    [Fact]
    public async Task Falls_back_to_a_safe_theme_when_there_is_no_active_company()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var user = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        var currentUser = new FakeCurrentUserContext { UserId = user.Id, PermissionCodes = [] };
        var currentCompany = new FakeCurrentCompanyContext { CompanyId = null };
        var handler = new GetMeQueryHandler(db, currentUser, currentCompany);

        var result = await handler.Handle(new GetMeQuery(), CancellationToken.None);

        result.EffectiveTheme.Should().Be("light");
    }
}
