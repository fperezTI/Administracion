using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Users;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Users.Read;
}

public sealed record UserDetail(
    Guid Id,
    string DisplayName,
    string Email,
    bool IsActive,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset? AnonymizedAtUtc,
    IReadOnlyCollection<Guid> RoleIds,
    IReadOnlyCollection<Guid> CompanyIds);

public sealed class GetUserByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetUserByIdQuery, UserDetail>
{
    public async Task<UserDetail> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var roleIds = await db.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == request.UserId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var companyIds = await db.UserCompanies.AsNoTracking()
            .Where(uc => uc.UserId == request.UserId)
            .Select(uc => uc.CompanyId)
            .ToListAsync(cancellationToken);

        return new UserDetail(
            user.Id, user.DisplayName, user.Email, user.IsActive, user.LastLoginAtUtc, user.AnonymizedAtUtc, roleIds, companyIds);
    }
}
