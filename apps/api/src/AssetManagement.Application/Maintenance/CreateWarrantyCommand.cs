using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Maintenance;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Maintenance;

public sealed record CreateWarrantyCommand(
    Guid AssetId, WarrantyType Type, string Provider, DateOnly StartDate, DateOnly EndDate, string? Terms)
    : IRequest<Guid>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Warranties.Create;
}

public sealed class CreateWarrantyCommandValidator : AbstractValidator<CreateWarrantyCommand>
{
    public CreateWarrantyCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Terms).MaximumLength(2000);
        RuleFor(x => x).Must(x => x.StartDate <= x.EndDate)
            .WithMessage("La fecha de inicio de la garantía debe ser anterior o igual a la fecha de fin.");
    }
}

public sealed class CreateWarrantyCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<CreateWarrantyCommand, Guid>
{
    public async Task<Guid> Handle(CreateWarrantyCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(asset.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        var warranty = Warranty.Create(
            asset.CompanyId, asset.Id, request.Type, request.Provider, request.StartDate, request.EndDate,
            request.Terms, clock.UtcNow, currentUser.UserId);

        db.Warranties.Add(warranty);
        await db.SaveChangesAsync(cancellationToken);

        return warranty.Id;
    }
}
