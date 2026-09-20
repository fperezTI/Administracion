using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Search;

/// <summary>
/// Global multi-entity search (pedido: "búsqueda global multi-entidad con permisos") — deliberately
/// implements no single <see cref="IRequiresPermission"/> (like <c>GetMeQuery</c>): any authenticated user
/// can search, but each of the 8 result types is only included when the caller holds that type's own
/// existing <c>{Module}.Read</c> permission, checked imperatively via <see cref="IPermissionChecker"/>
/// rather than a blocking request-level permission — see ADR 0013.
/// </summary>
public sealed record GlobalSearchQuery(string Term, Guid? CompanyId) : IRequest<IReadOnlyList<SearchResultItem>>;

/// <summary><c>LinkEntityType</c>/<c>LinkEntityId</c> differ from <c>EntityType</c>/<c>EntityId</c> only
/// for <c>Movement</c> (no detail page of its own — links to its <c>Asset</c> instead), keeping the
/// frontend's route mapping uniform (one lookup by <c>LinkEntityType</c>, never a per-type special
/// case).</summary>
public sealed record SearchResultItem(
    string EntityType, Guid EntityId, string Title, string? Subtitle, string LinkEntityType, Guid LinkEntityId);

public sealed class GlobalSearchQueryHandler(
    IApplicationDbContext db, ICurrentUserContext currentUser, ICurrentCompanyContext currentCompany, IPermissionChecker permissionChecker)
    : IRequestHandler<GlobalSearchQuery, IReadOnlyList<SearchResultItem>>
{
    private const int MaxResultsPerType = 8;

    public async Task<IReadOnlyList<SearchResultItem>> Handle(GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
        {
            throw new ForbiddenAccessException("Se requiere iniciar sesión.");
        }

        var term = request.Term.Trim();
        if (term.Length < 2)
        {
            return [];
        }

        var companyIds = ResolveCompanyIds(request.CompanyId);

        var results = new List<SearchResultItem>();

        if (await permissionChecker.HasPermissionAsync(userId, PermissionCatalog.Assets.Read, cancellationToken))
        {
            results.AddRange(await SearchAssetsAsync(companyIds, term, cancellationToken));
        }

        if (await permissionChecker.HasPermissionAsync(userId, PermissionCatalog.Movements.Read, cancellationToken))
        {
            results.AddRange(await SearchMovementsAsync(companyIds, term, cancellationToken));
        }

        if (await permissionChecker.HasPermissionAsync(userId, PermissionCatalog.Maintenance.Read, cancellationToken))
        {
            results.AddRange(await SearchMaintenanceOrdersAsync(companyIds, term, cancellationToken));
        }

        if (await permissionChecker.HasPermissionAsync(userId, PermissionCatalog.Warranties.Read, cancellationToken))
        {
            results.AddRange(await SearchWarrantiesAsync(companyIds, term, cancellationToken));
        }

        if (await permissionChecker.HasPermissionAsync(userId, PermissionCatalog.SpareParts.Read, cancellationToken))
        {
            results.AddRange(await SearchSparePartsAsync(companyIds, term, cancellationToken));
        }

        if (await permissionChecker.HasPermissionAsync(userId, PermissionCatalog.Consumables.Read, cancellationToken))
        {
            results.AddRange(await SearchConsumablesAsync(companyIds, term, cancellationToken));
        }

        if (await permissionChecker.HasPermissionAsync(userId, PermissionCatalog.Requests.Read, cancellationToken))
        {
            results.AddRange(await SearchInternalRequestsAsync(companyIds, term, cancellationToken));
        }

        if (await permissionChecker.HasPermissionAsync(userId, PermissionCatalog.Users.Read, cancellationToken))
        {
            results.AddRange(await SearchUsersAsync(term, cancellationToken));
        }

        return results;
    }

    private IReadOnlyCollection<Guid> ResolveCompanyIds(Guid? companyId)
    {
        if (companyId is null)
        {
            return currentCompany.AccessibleCompanyIds;
        }

        if (!currentCompany.AccessibleCompanyIds.Contains(companyId.Value))
        {
            throw new ForbiddenAccessException("El usuario no tiene acceso a la empresa indicada.");
        }

        return [companyId.Value];
    }

    private async Task<List<SearchResultItem>> SearchAssetsAsync(IReadOnlyCollection<Guid> companyIds, string term, CancellationToken ct)
    {
        var rows = await db.Assets.AsNoTracking()
            .Where(a => companyIds.Contains(a.CompanyId) &&
                (a.InternalFolio.Contains(term) || a.Brand.Contains(term) || a.Model.Contains(term) ||
                 (a.SerialNumber != null && a.SerialNumber.Contains(term))))
            .OrderBy(a => a.InternalFolio)
            .Take(MaxResultsPerType)
            .Select(a => new { a.Id, a.InternalFolio, a.Brand, a.Model })
            .ToListAsync(ct);

        return rows
            .Select(a => new SearchResultItem("Asset", a.Id, a.InternalFolio, $"{a.Brand} {a.Model}", "Asset", a.Id))
            .ToList();
    }

    private async Task<List<SearchResultItem>> SearchMovementsAsync(IReadOnlyCollection<Guid> companyIds, string term, CancellationToken ct)
    {
        var rows = await db.Movements.AsNoTracking()
            .Where(m => companyIds.Contains(m.CompanyId) && m.FolioNumber.Contains(term))
            .OrderByDescending(m => m.EffectiveAtUtc)
            .Take(MaxResultsPerType)
            .Join(db.Assets.AsNoTracking(), m => m.AssetId, a => a.Id,
                (m, a) => new { m.Id, m.FolioNumber, m.Type, m.AssetId, a.InternalFolio })
            .ToListAsync(ct);

        // Formats the raw Type enum only after materializing — comparing/formatting an enum mapped to
        // nvarchar inside a Select translated to SQL Server is the exact bug ADR 0012 documents.
        return rows
            .Select(m => new SearchResultItem("Movement", m.Id, m.FolioNumber, $"{m.Type} — {m.InternalFolio}", "Asset", m.AssetId))
            .ToList();
    }

    private async Task<List<SearchResultItem>> SearchMaintenanceOrdersAsync(IReadOnlyCollection<Guid> companyIds, string term, CancellationToken ct)
    {
        var rows = await db.MaintenanceOrders.AsNoTracking()
            .Where(o => companyIds.Contains(o.CompanyId) && (o.Folio.Contains(term) || o.Description.Contains(term)))
            .OrderByDescending(o => o.OpenedAtUtc)
            .Take(MaxResultsPerType)
            .Join(db.Assets.AsNoTracking(), o => o.AssetId, a => a.Id, (o, a) => new { o.Id, o.Folio, a.InternalFolio })
            .ToListAsync(ct);

        return rows
            .Select(o => new SearchResultItem("MaintenanceOrder", o.Id, o.Folio, $"Activo {o.InternalFolio}", "MaintenanceOrder", o.Id))
            .ToList();
    }

    private async Task<List<SearchResultItem>> SearchWarrantiesAsync(IReadOnlyCollection<Guid> companyIds, string term, CancellationToken ct)
    {
        var rows = await db.Warranties.AsNoTracking()
            .Where(w => companyIds.Contains(w.CompanyId) && w.Provider.Contains(term))
            .Take(MaxResultsPerType)
            .Join(db.Assets.AsNoTracking(), w => w.AssetId, a => a.Id, (w, a) => new { w.Id, w.Provider, a.InternalFolio })
            .ToListAsync(ct);

        return rows
            .Select(w => new SearchResultItem("Warranty", w.Id, w.Provider, $"Activo {w.InternalFolio}", "Warranty", w.Id))
            .ToList();
    }

    private async Task<List<SearchResultItem>> SearchSparePartsAsync(IReadOnlyCollection<Guid> companyIds, string term, CancellationToken ct)
    {
        var rows = await db.SpareParts.AsNoTracking()
            .Where(p => companyIds.Contains(p.CompanyId) &&
                (p.Name.Contains(term) || p.SerialNumber.Contains(term) || (p.PartNumber != null && p.PartNumber.Contains(term))))
            .Take(MaxResultsPerType)
            .Select(p => new { p.Id, p.Name, p.SerialNumber })
            .ToListAsync(ct);

        return rows
            .Select(p => new SearchResultItem("SparePart", p.Id, p.Name, $"Serie {p.SerialNumber}", "SparePart", p.Id))
            .ToList();
    }

    private async Task<List<SearchResultItem>> SearchConsumablesAsync(IReadOnlyCollection<Guid> companyIds, string term, CancellationToken ct)
    {
        var rows = await db.Consumables.AsNoTracking()
            .Where(c => companyIds.Contains(c.CompanyId) && (c.Name.Contains(term) || (c.Sku != null && c.Sku.Contains(term))))
            .Take(MaxResultsPerType)
            .Select(c => new { c.Id, c.Name, c.Sku })
            .ToListAsync(ct);

        return rows.Select(c => new SearchResultItem("Consumable", c.Id, c.Name, c.Sku, "Consumable", c.Id)).ToList();
    }

    private async Task<List<SearchResultItem>> SearchInternalRequestsAsync(IReadOnlyCollection<Guid> companyIds, string term, CancellationToken ct)
    {
        var rows = await db.InternalRequests.AsNoTracking()
            .Where(r => companyIds.Contains(r.CompanyId) && r.Justification.Contains(term))
            .OrderByDescending(r => r.RequestedAtUtc)
            .Take(MaxResultsPerType)
            .Join(db.Assets.AsNoTracking(), r => r.AssetId, a => a.Id, (r, a) => new { r.Id, r.Type, r.Justification, a.InternalFolio })
            .ToListAsync(ct);

        return rows
            .Select(r => new SearchResultItem(
                "InternalRequest", r.Id, $"{r.Type} — {r.InternalFolio}", Truncate(r.Justification, 120), "InternalRequest", r.Id))
            .ToList();
    }

    private async Task<List<SearchResultItem>> SearchUsersAsync(string term, CancellationToken ct)
    {
        // Users has no CompanyId — global identity, same criterion the /users admin screen already uses.
        var rows = await db.Users.AsNoTracking()
            .Where(u => u.DisplayName.Contains(term) || u.Email.Contains(term))
            .OrderBy(u => u.DisplayName)
            .Take(MaxResultsPerType)
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToListAsync(ct);

        return rows.Select(u => new SearchResultItem("User", u.Id, u.DisplayName, u.Email, "User", u.Id)).ToList();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? string.Concat(value.AsSpan(0, maxLength - 1), "…") : value;
}
