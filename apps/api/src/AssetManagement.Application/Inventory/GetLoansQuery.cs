using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Models;
using AssetManagement.Application.Common.Security;
using AssetManagement.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Inventory;

public sealed record GetLoansQuery(Guid CompanyId, int PageNumber = 1, int PageSize = 50, LoanStatus? Status = null)
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

        var projected =
            from l in query
            join asset in db.Assets.AsNoTracking() on l.AssetId equals asset.Id
            join user in db.Users.AsNoTracking() on l.BorrowerUserId equals user.Id
            orderby l.LoanedAtUtc descending
            select new LoanSummary(
                l.Id, l.AssetId, asset.InternalFolio, l.BorrowerUserId, user.DisplayName, l.ExpectedReturnDate,
                l.Status, l.LoanedAtUtc, l.ReturnedAtUtc);

        return PagedResult<LoanSummary>.CreateAsync(projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}
