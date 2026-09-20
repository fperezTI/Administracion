using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Maintenance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record UpdateWarrantyCommand(
    Guid WarrantyId, WarrantyType Type, string Provider, DateOnly StartDate, DateOnly EndDate, string? Terms)
    : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Warranties.Update;
}

public sealed class UpdateWarrantyCommandValidator : AbstractValidator<UpdateWarrantyCommand>
{
    public UpdateWarrantyCommandValidator()
    {
        RuleFor(x => x.WarrantyId).NotEmpty();
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Terms).MaximumLength(2000);
        RuleFor(x => x).Must(x => x.StartDate <= x.EndDate)
            .WithMessage("La fecha de inicio de la garantía debe ser anterior o igual a la fecha de fin.");
    }
}

public sealed class UpdateWarrantyCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<UpdateWarrantyCommand>
{
    public async Task Handle(UpdateWarrantyCommand request, CancellationToken cancellationToken)
    {
        var warranty = await db.Warranties.FirstOrDefaultAsync(w => w.Id == request.WarrantyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Warranty), request.WarrantyId);

        if (!currentCompany.AccessibleCompanyIds.Contains(warranty.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de esta garantía.");
        }

        warranty.UpdateDetails(
            request.Type, request.Provider, request.StartDate, request.EndDate, request.Terms, clock.UtcNow,
            currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);
    }
}
