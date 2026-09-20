using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.SparePartsAndConsumables;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.SparePartsAndConsumables;

public sealed record DisposeSparePartCommand(Guid SparePartId) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.SpareParts.Update;
}

public sealed class DisposeSparePartCommandValidator : AbstractValidator<DisposeSparePartCommand>
{
    public DisposeSparePartCommandValidator() => RuleFor(x => x.SparePartId).NotEmpty();
}

public sealed class DisposeSparePartCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<DisposeSparePartCommand>
{
    public async Task Handle(DisposeSparePartCommand request, CancellationToken cancellationToken)
    {
        var sparePart = await db.SpareParts.FirstOrDefaultAsync(p => p.Id == request.SparePartId, cancellationToken)
            ?? throw new NotFoundException(nameof(SparePart), request.SparePartId);

        if (!currentCompany.AccessibleCompanyIds.Contains(sparePart.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de esta refacción.");
        }

        sparePart.MarkDisposed(clock.UtcNow, currentUser.UserId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
