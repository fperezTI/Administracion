using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Organization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Identity.Users;

/// <summary>
/// Pre-provisions a person already in the tenant directory (found via <see cref="SearchDirectoryUsersQuery"/>)
/// before their first login, with an initial role and company access already in place. Uses the exact same
/// domain factory the login JIT path uses (<see cref="User.Provision"/>) — when this person does log in for
/// the first time, <c>ProvisionOrUpdateUserCommand</c> finds the existing row by <c>EntraObjectId</c> and
/// only refreshes the profile from the real token claims, it never creates a duplicate.
/// </summary>
public sealed record CreateUserFromDirectoryCommand(
    Guid EntraObjectId, string DisplayName, string Email, Guid RoleId, IReadOnlyList<Guid> CompanyIds)
    : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Users.Create;
}

public sealed class CreateUserFromDirectoryCommandValidator : AbstractValidator<CreateUserFromDirectoryCommand>
{
    public CreateUserFromDirectoryCommandValidator()
    {
        RuleFor(x => x.EntraObjectId).NotEmpty();
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320);
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public sealed class CreateUserFromDirectoryCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CreateUserFromDirectoryCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserFromDirectoryCommand request, CancellationToken cancellationToken)
    {
        var alreadyExists = await db.Users.AnyAsync(u => u.EntraObjectId == request.EntraObjectId, cancellationToken);
        if (alreadyExists)
        {
            throw new ConflictException("Esta persona ya tiene un perfil en el sistema.");
        }

        var roleExists = await db.Roles.AnyAsync(r => r.Id == request.RoleId && r.IsActive, cancellationToken);
        if (!roleExists)
        {
            throw new NotFoundException(nameof(Role), request.RoleId);
        }

        foreach (var companyId in request.CompanyIds.Distinct())
        {
            var companyExists = await db.Companies.AnyAsync(c => c.Id == companyId, cancellationToken);
            if (!companyExists)
            {
                throw new NotFoundException(nameof(Company), companyId);
            }
        }

        var now = clock.UtcNow;
        var user = User.Provision(request.EntraObjectId, request.DisplayName, request.Email, now);
        user.AssignRole(request.RoleId, currentUser.UserId, now);

        foreach (var companyId in request.CompanyIds.Distinct())
        {
            user.GrantCompanyAccess(companyId, now);
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
