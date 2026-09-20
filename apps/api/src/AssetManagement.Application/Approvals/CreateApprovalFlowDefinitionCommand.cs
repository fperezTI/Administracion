using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Approvals;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Approvals;

public sealed record CreateApprovalFlowDefinitionCommand(
    string Key,
    Guid? CompanyId,
    IReadOnlyList<Guid> ApproverRoleIds,
    int RequiredApprovals,
    ApprovalMode Mode,
    bool RequiresComment)
    : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Approvals.Configure;
}

public sealed class CreateApprovalFlowDefinitionCommandValidator : AbstractValidator<CreateApprovalFlowDefinitionCommand>
{
    public CreateApprovalFlowDefinitionCommandValidator()
    {
        RuleFor(x => x.Key).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ApproverRoleIds).NotEmpty();
        RuleFor(x => x.RequiredApprovals).GreaterThan(0);
    }
}

public sealed class CreateApprovalFlowDefinitionCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CreateApprovalFlowDefinitionCommand, Guid>
{
    public async Task<Guid> Handle(CreateApprovalFlowDefinitionCommand request, CancellationToken cancellationToken)
    {
        if (request.CompanyId is { } companyId && !currentCompany.AccessibleCompanyIds.Contains(companyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var validRoleCount = await db.Roles.CountAsync(r => request.ApproverRoleIds.Contains(r.Id), cancellationToken);
        if (validRoleCount != request.ApproverRoleIds.Distinct().Count())
        {
            throw new ConflictException("Uno o más roles aprobadores no existen.");
        }

        var alreadyExists = await db.ApprovalFlowDefinitions
            .AnyAsync(f => f.Key == request.Key && f.IsActive && f.CompanyId == request.CompanyId, cancellationToken);
        if (alreadyExists)
        {
            throw new ConflictException("Ya existe un flujo activo con esa clave para ese alcance. Desactívalo antes de crear uno nuevo.");
        }

        var now = clock.UtcNow;
        var flow = ApprovalFlowDefinition.Create(
            request.Key, request.CompanyId, request.ApproverRoleIds, request.RequiredApprovals, request.Mode,
            request.RequiresComment, now, currentUser.UserId);

        db.ApprovalFlowDefinitions.Add(flow);
        await db.SaveChangesAsync(cancellationToken);

        return flow.Id;
    }
}
