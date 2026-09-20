using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.UnitTests.TestSupport;

internal sealed class FakeCurrentCompanyContext : ICurrentCompanyContext
{
    public Guid? CompanyId { get; set; }

    public bool HasActiveCompany => CompanyId is not null;

    public IReadOnlyCollection<Guid> AccessibleCompanyIds { get; set; } = [];
}
