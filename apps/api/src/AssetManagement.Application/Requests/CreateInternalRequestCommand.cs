using AssetManagement.Application.Approvals;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Requests;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Requests;

/// <summary>
/// Submits an internal request (pedido: "solicitudes internas con aprobación configurable"). Goes straight
/// to <see cref="InternalRequestStatus.PendingApproval"/> — no draft phase, see the F7 plan decision 1.
/// Each <see cref="InternalRequestType"/> asks a distinct approval flow (decision 2) so an admin can assign
/// different approvers per type, same as F4's <c>asset.decommission</c>/<c>asset.disposal</c>.
/// </summary>
public sealed record CreateInternalRequestCommand(
    InternalRequestType Type, Guid AssetId, string Justification, DateOnly? ExpectedReturnDate)
    : IRequest<Guid>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Requests.Create;
}

public sealed class CreateInternalRequestCommandValidator : AbstractValidator<CreateInternalRequestCommand>
{
    public CreateInternalRequestCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Justification).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.ExpectedReturnDate).NotEmpty().When(x => x.Type == InternalRequestType.Loan)
            .WithMessage("Una solicitud de préstamo requiere la fecha esperada de devolución.");
        RuleFor(x => x.ExpectedReturnDate).Empty().When(x => x.Type != InternalRequestType.Loan)
            .WithMessage("La fecha esperada de devolución solo aplica a solicitudes de préstamo.");
    }
}

public sealed class CreateInternalRequestCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IApprovalCoordinator approvalCoordinator, IClock clock)
    : IRequestHandler<CreateInternalRequestCommand, Guid>
{
    public async Task<Guid> Handle(CreateInternalRequestCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(asset.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        var eligible = request.Type == InternalRequestType.Maintenance
            ? asset.Status is AssetStatus.InWarehouse or AssetStatus.Assigned
            : asset.Status == AssetStatus.InWarehouse;
        if (!eligible)
        {
            throw new ConflictException(
                request.Type == InternalRequestType.Maintenance
                    ? "Solo un activo en almacén o asignado puede reportarse a mantenimiento."
                    : "Solo un activo en almacén puede solicitarse.");
        }

        var now = clock.UtcNow;
        var internalRequest = InternalRequest.Create(
            asset.CompanyId, request.Type, asset.Id, currentUser.UserId!.Value, request.Justification,
            request.ExpectedReturnDate, now, currentUser.UserId);
        db.InternalRequests.Add(internalRequest);

        var flowKey = request.Type switch
        {
            InternalRequestType.AssetAssignment => "internal-request.asset-assignment",
            InternalRequestType.Loan => "internal-request.loan",
            InternalRequestType.Maintenance => "internal-request.maintenance",
            _ => throw new ConflictException("Tipo de solicitud no reconocido."),
        };

        await approvalCoordinator.RequestApprovalAsync(
            asset.CompanyId, flowKey, "InternalRequest", internalRequest.Id, currentUser.UserId!.Value,
            request.Justification, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return internalRequest.Id;
    }
}
