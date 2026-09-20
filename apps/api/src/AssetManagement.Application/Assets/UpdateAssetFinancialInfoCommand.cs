using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

/// <summary>Financial fields are informative only in V1 — no depreciation or accounting calculations
/// (pedido §11, §37). Purely data capture.</summary>
public sealed record UpdateAssetFinancialInfoCommand(
    Guid AssetId,
    DateOnly? AcquisitionDate,
    decimal? AcquisitionCost,
    string? Currency,
    string? Supplier,
    string? Invoice,
    string? PurchaseOrder)
    : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Update;
}

public sealed class UpdateAssetFinancialInfoCommandValidator : AbstractValidator<UpdateAssetFinancialInfoCommand>
{
    public UpdateAssetFinancialInfoCommandValidator()
    {
        RuleFor(x => x.Currency).Length(3).When(x => x.Currency is not null);
        RuleFor(x => x.AcquisitionCost).GreaterThanOrEqualTo(0).When(x => x.AcquisitionCost is not null);
        RuleFor(x => x.Supplier).MaximumLength(200);
        RuleFor(x => x.Invoice).MaximumLength(100);
        RuleFor(x => x.PurchaseOrder).MaximumLength(100);
    }
}

public sealed class UpdateAssetFinancialInfoCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<UpdateAssetFinancialInfoCommand>
{
    public async Task Handle(UpdateAssetFinancialInfoCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        asset.UpdateFinancialInfo(
            request.AcquisitionDate, request.AcquisitionCost, request.Currency, request.Supplier,
            request.Invoice, request.PurchaseOrder, clock.UtcNow, currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);
    }
}
