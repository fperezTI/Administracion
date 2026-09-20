using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record ReturnLoanCommand(Guid LoanId, string? Notes) : IRequest, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Returns.Create;
}

public sealed class ReturnLoanCommandValidator : AbstractValidator<ReturnLoanCommand>
{
    public ReturnLoanCommandValidator()
    {
        RuleFor(x => x.LoanId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class ReturnLoanCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFolioGenerator folioGenerator, IClock clock)
    : IRequestHandler<ReturnLoanCommand>
{
    public async Task Handle(ReturnLoanCommand request, CancellationToken cancellationToken)
    {
        var loan = await db.Loans.FirstOrDefaultAsync(l => l.Id == request.LoanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        if (!currentCompany.AccessibleCompanyIds.Contains(loan.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este préstamo.");
        }

        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == loan.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), loan.AssetId);

        var now = clock.UtcNow;
        var folio = await folioGenerator.NextAsync(loan.CompanyId, FolioDocumentTypes.MovementLoanReturn, cancellationToken);

        var returnMovement = Movement.Create(
            loan.CompanyId, asset.Id, MovementType.LoanReturn, folio, startsCompleted: true,
            fromOrgUnitId: asset.CurrentOrgUnitId, toOrgUnitId: null, fromUserId: loan.BorrowerUserId,
            toUserId: null, notes: request.Notes, now, currentUser.UserId);

        loan.Return(returnMovement.Id, now, currentUser.UserId);
        asset.ChangeStatus(AssetStatus.InWarehouse, now, currentUser.UserId);

        db.Movements.Add(returnMovement);
        await db.SaveChangesAsync(cancellationToken);
    }
}
