using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Domain.Theming;
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
    Guid? ActiveCompanyId,
    /// <summary>Null means "usar tema de la empresa" — see <see cref="EffectiveTheme"/> for what
    /// actually applies.</summary>
    string? ThemePreference,
    /// <summary>Always a concrete, valid theme code — resolved from <see cref="ThemePreference"/>,
    /// falling back to the active company's default (or, when none is explicitly selected via
    /// <c>X-Active-Company-Id</c> — the common case today, since no frontend caller sends it yet per
    /// docs/multi-company.md — the user's first accessible company), falling back to
    /// <see cref="ThemeCode.Fallback"/>. Never null.</summary>
    string EffectiveTheme);

public sealed record MeCompany(Guid CompanyId, string TradeName, string TimeZone, string DefaultThemeCode);

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

        // Already resolved once for this request by CurrentUserProvisioningMiddleware — no need to
        // re-run the same permission join here.
        var permissionCodes = currentUser.PermissionCodes;

        var companies = await (
            from userCompany in db.UserCompanies
            join company in db.Companies on userCompany.CompanyId equals company.Id
            where userCompany.UserId == userId
            select new MeCompany(company.Id, company.TradeName, company.TimeZone, company.DefaultThemeCode))
            .ToListAsync(cancellationToken);

        var themePreference = ThemeCode.IsValid(user.ThemePreferenceCode) ? user.ThemePreferenceCode : null;
        var activeCompany = companies.FirstOrDefault(c => c.CompanyId == currentCompany.CompanyId)
            ?? companies.FirstOrDefault();
        var effectiveTheme = themePreference
            ?? (activeCompany is not null ? ThemeCode.OrFallback(activeCompany.DefaultThemeCode) : ThemeCode.Fallback);

        return new MeResponse(
            user.Id, user.DisplayName, user.Email, permissionCodes, companies, currentCompany.CompanyId,
            themePreference, effectiveTheme);
    }
}
