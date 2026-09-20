namespace AssetManagement.Application.Common.Interfaces;

/// <summary>
/// The active company for the current request, resolved server-side from the caller's verified
/// company memberships — never trusted directly from a request parameter or header.
/// See docs/multi-company.md.
/// </summary>
public interface ICurrentCompanyContext
{
    public bool HasActiveCompany { get; }

    public Guid? CompanyId { get; }

    public IReadOnlyCollection<Guid> AccessibleCompanyIds { get; }
}
