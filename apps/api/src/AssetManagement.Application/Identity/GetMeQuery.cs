using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity;

/// <summary>The authenticated caller's own profile, permissions and accessible companies. No
/// permission is required — every authenticated user may see their own access, never another user's
/// (AuthorizationBehavior is not involved; this query never accepts a target user id).</summary>
public sealed record GetMeQuery : IRequest<MeResponse>;

public sealed record MeResponse(
    Guid UserId,
    string DisplayName,
    string Email,
    IReadOnlyCollection<string> PermissionCodes,
    IReadOnlyCollection<MeCompany> Companies,
    Guid? ActiveCompanyId);

public sealed record MeCompany(Guid CompanyId, string TradeName);

public sealed class GetMeQueryHandler(IApplicationDbContext db, ICurrentUserContext currentUser, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetMeQuery, MeResponse>
{
    public async Task<MeResponse> Handle(GetMeQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Authentication is required to read the current user's profile.");
        }

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Identity.User), userId);

        var permissionCodes = await UserPermissionLookup.GetPermissionCodesAsync(db, userId, cancellationToken);

        var companies = await (
            from userCompany in db.UserCompanies
            join company in db.Companies on userCompany.CompanyId equals company.Id
            where userCompany.UserId == userId
            select new MeCompany(company.Id, company.TradeName))
            .ToListAsync(cancellationToken);

        return new MeResponse(
            user.Id, user.DisplayName, user.Email, permissionCodes, companies, currentCompany.CompanyId);
    }
}
