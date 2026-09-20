using AssetManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AssetManagement.Infrastructure.Security;

public sealed class HttpContextCurrentCompanyContext(IHttpContextAccessor httpContextAccessor) : ICurrentCompanyContext
{
    public bool HasActiveCompany => CompanyId is not null;

    public Guid? CompanyId => httpContextAccessor.HttpContext?.Items[RequestContextKeys.ActiveCompanyId] as Guid?;

    public IReadOnlyCollection<Guid> AccessibleCompanyIds =>
        httpContextAccessor.HttpContext?.Items[RequestContextKeys.AccessibleCompanyIds] as IReadOnlyCollection<Guid>
        ?? [];
}
