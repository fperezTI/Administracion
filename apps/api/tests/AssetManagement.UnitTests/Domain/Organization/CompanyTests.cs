using AssetManagement.Domain.Organization;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Organization;

public class CompanyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_normalizes_the_currency_code_to_upper_invariant()
    {
        var company = Company.Create("Acme S.A. de C.V.", "Acme", "TAX-123", "mxn", "America/Mexico_City", Now);

        company.BaseCurrency.Should().Be("MXN");
    }

    [Theory]
    [InlineData("", "Acme", "TAX-1", "MXN", "America/Mexico_City")]
    [InlineData("Acme S.A.", "", "TAX-1", "MXN", "America/Mexico_City")]
    [InlineData("Acme S.A.", "Acme", "", "MXN", "America/Mexico_City")]
    [InlineData("Acme S.A.", "Acme", "TAX-1", "MX", "America/Mexico_City")]
    [InlineData("Acme S.A.", "Acme", "TAX-1", "MXN", "")]
    public void Create_rejects_invalid_input(
        string legalName, string tradeName, string taxId, string baseCurrency, string timeZone)
    {
        var act = () => Company.Create(legalName, tradeName, taxId, baseCurrency, timeZone, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deactivate_then_Activate_round_trips_IsActive()
    {
        var company = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);

        company.Deactivate();
        company.IsActive.Should().BeFalse();

        company.Activate();
        company.IsActive.Should().BeTrue();
    }

    [Fact]
    public void New_company_defaults_to_a_safe_fallback_theme()
    {
        var company = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);

        company.DefaultThemeCode.Should().Be("light");
    }

    [Fact]
    public void SetDefaultTheme_accepts_a_known_code()
    {
        var company = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);

        company.SetDefaultTheme("corporate-blue");

        company.DefaultThemeCode.Should().Be("corporate-blue");
    }

    [Fact]
    public void SetDefaultTheme_rejects_an_unknown_code()
    {
        var company = Company.Create("Acme S.A.", "Acme", "TAX-1", "MXN", "America/Mexico_City", Now);

        var act = () => company.SetDefaultTheme("neon-pink");

        act.Should().Throw<DomainException>();
    }
}
