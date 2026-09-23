using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record GetLoansQuery(
    Guid CompanyId,
    int PageNumber = 1,
    int PageSize = 50,
    LoanStatus? Status = null,
    /// <summary>loanedAtUtc descendente (default) | assetFolio | borrowerDisplayName | expectedReturnDate | status</summary>
    string? SortBy = null,
    bool SortDescending = false)
    : IRequest<PagedResult<LoanSummary>>, IRequiresPermission
{
    public string PermissionCode => PermissionCatalog.Loans.Read;
}

public sealed record LoanSummary(
    Guid Id,
    Guid AssetId,
    string AssetFolio,
    Guid BorrowerUserId,
    string BorrowerDisplayName,
    DateOnly ExpectedReturnDate,
    LoanStatus Status,
    DateTimeOffset LoanedAtUtc,
    DateTimeOffset? ReturnedAtUtc);

public sealed class GetLoansQueryHandler(IApplicationDbContext db, ICurrentCompanyContext currentCompany)
    : IRequestHandler<GetLoansQuery, PagedResult<LoanSummary>>
{
    public Task<PagedResult<LoanSummary>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
    {
        if (!currentCompany.AccessibleCompanyIds.Contains(request.CompanyId))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        var query = db.Loans.AsNoTracking().Where(l => l.CompanyId == request.CompanyId);

        if (request.Status is { } status)
        {
            query = query.Where(l => l.Status == status);
        }

        var joined =
            from l in query
            join asset in db.Assets.AsNoTracking() on l.AssetId equals asset.Id
            join user in db.Users.AsNoTracking() on l.BorrowerUserId equals user.Id
            select new { Loan = l, AssetFolio = asset.InternalFolio, BorrowerName = user.DisplayName };

        var descending = request.SortDescending;
        var ordered = request.SortBy switch
        {
            "assetFolio" => descending ? joined.OrderByDescending(x => x.AssetFolio) : joined.OrderBy(x => x.AssetFolio),
            "borrowerDisplayName" => descending
                ? joined.OrderByDescending(x => x.BorrowerName)
                : joined.OrderBy(x => x.BorrowerName),
            "expectedReturnDate" => descending
                ? joined.OrderByDescending(x => x.Loan.ExpectedReturnDate)
                : joined.OrderBy(x => x.Loan.ExpectedReturnDate),
            "status" => descending ? joined.OrderByDescending(x => x.Loan.Status) : joined.OrderBy(x => x.Loan.Status),
            "loanedAtUtc" => descending
                ? joined.OrderByDescending(x => x.Loan.LoanedAtUtc)
                : joined.OrderBy(x => x.Loan.LoanedAtUtc),
            // Default histórico (sin SortBy): más reciente primero, sin importar SortDescending.
            _ => joined.OrderByDescending(x => x.Loan.LoanedAtUtc),
        };

        var projected = ordered.Select(x => new LoanSummary(
            x.Loan.Id, x.Loan.AssetId, x.AssetFolio, x.Loan.BorrowerUserId, x.BorrowerName, x.Loan.ExpectedReturnDate,
            x.Loan.Status, x.Loan.LoanedAtUtc, x.Loan.ReturnedAtUtc));

        return PagedResult<LoanSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
