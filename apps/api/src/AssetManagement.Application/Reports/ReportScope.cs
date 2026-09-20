using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;

namespace AssetManagement.Application.Reports;

/// <summary>
/// Every report query takes an optional <c>CompanyId</c>: provided means "this one company" (gated by
/// <see cref="PermissionCatalog.Reports.Read"/>, membership validated like everywhere else in the app);
/// omitted means "consolidated across every company I can access" (gated by the stronger
/// <see cref="PermissionCatalog.Reports.ReadConsolidated"/>). Both permissions were seeded since F1 for
/// exactly this distinction — see ADR 0012.
/// </summary>
internal static class ReportScope
{
    public static string PermissionCode(Guid? companyId) =>
        companyId is null ? PermissionCatalog.Reports.ReadConsolidated : PermissionCatalog.Reports.Read;

    public static IReadOnlyCollection<Guid> ResolveCompanyIds(Guid? companyId, ICurrentCompanyContext currentCompany)
    {
        if (companyId is null)
        {
            return currentCompany.AccessibleCompanyIds;
        }

        if (!currentCompany.AccessibleCompanyIds.Contains(companyId.Value))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        return [companyId.Value];
    }
}
