using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Assets;

public sealed record UpdateAssetContractualInfoCommand(
    Guid AssetId,
    DateOnly? WarrantyStartDate,
    DateOnly? WarrantyEndDate,
    string? SupportContract,
    string? SupportProvider)
    : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Assets.Update;
}

public sealed class UpdateAssetContractualInfoCommandValidator : AbstractValidator<UpdateAssetContractualInfoCommand>
{
    public UpdateAssetContractualInfoCommandValidator()
    {
        RuleFor(x => x.SupportContract).MaximumLength(100);
        RuleFor(x => x.SupportProvider).MaximumLength(200);
        RuleFor(x => x)
            .Must(x => x.WarrantyStartDate is null || x.WarrantyEndDate is null || x.WarrantyStartDate <= x.WarrantyEndDate)
            .WithMessage("La fecha de inicio de garantía debe ser anterior o igual a la fecha de fin.");
    }
}

public sealed class UpdateAssetContractualInfoCommandHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, IClock clock)
    : IRequestHandler<UpdateAssetContractualInfoCommand>
{
    public async Task Handle(UpdateAssetContractualInfoCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        asset.UpdateContractualInfo(
            request.WarrantyStartDate, request.WarrantyEndDate, request.SupportContract, request.SupportProvider,
            clock.UtcNow, currentUser.UserId);

        await db.SaveChangesAsync(cancellationToken);
    }
}
