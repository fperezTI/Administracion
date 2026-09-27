using AssetManagement.Domain.SharedKernel;
using AssetManagement.Domain.Theming;

namespace AssetManagement.Domain.Organization;

/// <summary>
/// A legal entity within the single Entra ID tenant (pedido §8). Up to 50 active companies; the cap
/// is enforced by the application layer (a cross-aggregate policy, not a single aggregate's invariant).
/// </summary>
public sealed class Company : AggregateRoot<Guid>
{
    public string LegalName { get; private set; } = null!;
    public string TradeName { get; private set; } = null!;
    public string TaxId { get; private set; } = null!;
    public string BaseCurrency { get; private set; } = null!;
    public string TimeZone { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    /// <summary>The theme every user of this company sees unless they set their own personal
    /// preference (pedido: sistema de temas visuales). Only a Super Administrador may change this
    /// (enforced via <see cref="Application.Common.Security.PermissionCatalog.Companies.Update"/>, the
    /// same permission that already gates editing a company) — always a concrete, valid theme, never
    /// null: <see cref="ThemeCode.Fallback"/> until explicitly set otherwise.</summary>
    public string DefaultThemeCode { get; private set; } = ThemeCode.Fallback;

    private Company()
    {
    }

    private Company(
        Guid id,
        string legalName,
        string tradeName,
        string taxId,
        string baseCurrency,
        string timeZone,
        DateTimeOffset nowUtc)
        : base(id)
    {
        LegalName = legalName;
        TradeName = tradeName;
        TaxId = taxId;
        BaseCurrency = baseCurrency;
        TimeZone = timeZone;
        CreatedAtUtc = nowUtc;
        IsActive = true;
    }

    public static Company Create(
        string legalName,
        string tradeName,
        string taxId,
        string baseCurrency,
        string timeZone,
        DateTimeOffset nowUtc)
    {
        Validate(legalName, tradeName, taxId, baseCurrency, timeZone);

        return new Company(
            Guid.NewGuid(),
            legalName.Trim(),
            tradeName.Trim(),
            taxId.Trim(),
            baseCurrency.Trim().ToUpperInvariant(),
            timeZone.Trim(),
            nowUtc);
    }

    public void UpdateProfile(string legalName, string tradeName, string taxId, string baseCurrency, string timeZone)
    {
        Validate(legalName, tradeName, taxId, baseCurrency, timeZone);

        LegalName = legalName.Trim();
        TradeName = tradeName.Trim();
        TaxId = taxId.Trim();
        BaseCurrency = baseCurrency.Trim().ToUpperInvariant();
        TimeZone = timeZone.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SetDefaultTheme(string themeCode)
    {
        if (!ThemeCode.IsValid(themeCode))
        {
            throw new DomainException($"'{themeCode}' no es un código de tema válido.");
        }

        DefaultThemeCode = themeCode;
    }

    private static void Validate(string legalName, string tradeName, string taxId, string baseCurrency, string timeZone)
    {
        if (string.IsNullOrWhiteSpace(legalName))
        {
            throw new DomainException("La razón social es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(tradeName))
        {
            throw new DomainException("El nombre comercial es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(taxId))
        {
            throw new DomainException("La identificación fiscal es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(baseCurrency) || baseCurrency.Trim().Length != 3)
        {
            throw new DomainException("La moneda base debe ser un código ISO 4217 de 3 letras.");
        }

        if (string.IsNullOrWhiteSpace(timeZone))
        {
            throw new DomainException("La zona horaria es obligatoria.");
        }
    }
}
