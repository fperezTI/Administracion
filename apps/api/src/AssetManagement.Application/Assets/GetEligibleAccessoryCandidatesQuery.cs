using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

/// <summary>Assets that could be linked as an accessory of the given primary right now — same company,
/// not the primary itself, not already an accessory of something, and without accessories of its own
/// (see <see cref="LinkAssetAccessoryCommand"/>, which enforces exactly these rules). Kept as its own
/// query rather than a filter on <see cref="GetAssetsQuery"/> because the eligibility rule set is
/// specific to this one relationship, not a general asset listing concern.</summary>
public sealed record GetEligibleAccessoryCandidatesQuery(Guid CompanyId, Guid PrimaryAssetId)
    : IRequest<IReadOnlyList<AccessoryCandidate>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Read;
}

public sealed record AccessoryCandidate(Guid Id, string InternalFolio, string Brand, string Model, string? SerialNumber);

public sealed class GetEligibleAccessoryCandidatesQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetEligibleAccessoryCandidatesQuery, IReadOnlyList<AccessoryCandidate>>
{
    public async Task<IReadOnlyList<AccessoryCandidate>> Handle(
        GetEligibleAccessoryCandidatesQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var candidates =
            from a in db.Assets.AsNoTracking()
            where a.CompanyId == request.CompanyId
                && a.Id != request.PrimaryAssetId
                && a.AccessoryOfAssetId == null
                && !db.Assets.Any(other => other.AccessoryOfAssetId == a.Id)
            orderby a.InternalFolio
            select new AccessoryCandidate(a.Id, a.InternalFolio, a.Brand, a.Model, a.SerialNumber);

        return await candidates.ToListAsync(cancellationToken);
    }
}
