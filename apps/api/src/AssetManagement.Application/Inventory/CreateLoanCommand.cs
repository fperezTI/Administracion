using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

/// <summary>Short-term, signature-free borrowing (see <see cref="Loan"/>) — single-step, unlike
/// <see cref="CreateAssignmentCommand"/>.</summary>
public sealed record CreateLoanCommand(Guid AssetId, Guid BorrowerUserId, DateOnly ExpectedReturnDate, string? Notes)
    : IRequest<CreateLoanResult>, IRequiresPermission, IAuditableCommand
{
    public string PermissionCode => PermissionCatalog.Loans.Create;
}

public sealed record CreateLoanResult(Guid LoanId, Guid MovementId, string MovementFolio);

public sealed class CreateLoanCommandValidator : AbstractValidator<CreateLoanCommand>
{
    public CreateLoanCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.BorrowerUserId).NotEmpty();
        RuleFor(x => x.ExpectedReturnDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

public sealed class CreateLoanCommandHandler(
    IApplicationDbContext db, ICurrentCompanyContext currentCompany, ICurrentUserContext currentUser,
    IFolioGenerator folioGenerator, IClock clock)
    : IRequestHandler<CreateLoanCommand, CreateLoanResult>
{
    public async Task<CreateLoanResult> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
    {
        var asset = await db.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Asset), request.AssetId);

        if (!currentCompany.AccessibleCompanyIds.Contains(asset.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa de este activo.");
        }

        var borrowerHasAccess = await db.UserCompanies
            .AnyAsync(uc => uc.UserId == request.BorrowerUserId && uc.CompanyId == asset.CompanyId, cancellationToken);
        if (!borrowerHasAccess)
        {
            throw new ConflictException("El destinatario no tiene acceso a la empresa de este activo.");
        }

        var now = clock.UtcNow;
        var folio = await folioGenerator.NextAsync(asset.CompanyId, FolioDocumentTypes.MovementLoan, cancellationToken);

        var movement = Movement.Create(
            asset.CompanyId, asset.Id, MovementType.Loan, folio, startsCompleted: true,
            fromOrgUnitId: asset.CurrentOrgUnitId, toOrgUnitId: null, fromUserId: null,
            toUserId: request.BorrowerUserId, notes: request.Notes, now, currentUser.UserId);

        var loan = Loan.Create(asset.CompanyId, asset.Id, request.BorrowerUserId, movement.Id, request.ExpectedReturnDate, now, currentUser.UserId);

        asset.ChangeStatus(AssetStatus.OnLoan, now, currentUser.UserId);

        db.Movements.Add(movement);
        db.Loans.Add(loan);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateLoanResult(loan.Id, movement.Id, movement.FolioNumber);
    }
}
