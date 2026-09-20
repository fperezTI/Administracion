using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Common.Models;

/// <summary>Every collection endpoint returns this — pagination is mandatory (pedido §11/§31), never optional.</summary>
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int TotalCount, int PageNumber, int PageSize)
{
    public static async Task<PagedResult<T>> CreateAsync(
        IQueryable<T> query, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageNumber = pageNumber < 1 ? 1 : pageNumber;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }
}
