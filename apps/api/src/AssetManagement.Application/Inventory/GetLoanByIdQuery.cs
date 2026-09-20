using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record GetLoanByIdQuery(Guid LoanId) : IRequest<LoanDetail>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Loans.Read;
}

public sealed record LoanDetail(
    Guid Id,
    Guid AssetId,
    string AssetFolio,
    Guid BorrowerUserId,
    string BorrowerDisplayName,
    DateOnly ExpectedReturnDate,
    LoanStatus Status,
    DateTimeOffset LoanedAtUtc,
    DateTimeOffset? ReturnedAtUtc);

public sealed class GetLoanByIdQueryHandler(IApplicationDbContext db) : IRequestHandler<GetLoanByIdQuery, LoanDetail>
{
    public async Task<LoanDetail> Handle(GetLoanByIdQuery request, CancellationToken cancellationToken)
    {
        var loan = await db.Loans.AsNoTracking().FirstOrDefaultAsync(l => l.Id == request.LoanId, cancellationToken)
            ?? throw new NotFoundException(nameof(Loan), request.LoanId);

        var asset = await db.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == loan.AssetId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Assets.Asset), loan.AssetId);

        var borrower = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == loan.BorrowerUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Identity.User), loan.BorrowerUserId);

        return new LoanDetail(
            loan.Id, loan.AssetId, asset.InternalFolio, loan.BorrowerUserId, borrower.DisplayName,
            loan.ExpectedReturnDate, loan.Status, loan.LoanedAtUtc, loan.ReturnedAtUtc);
    }
}
