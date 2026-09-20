using System.Data.Common;
using AssetManagement.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence;

/// <summary>
/// Issues folios with a single atomic SQL Server MERGE (upsert-and-increment) rather than a
/// read-modify-write plus optimistic-concurrency retry loop — simpler, and race-free without needing
/// EF Core change-tracking access from outside the DbContext (see docs/multi-company.md, "Folios").
/// </summary>
public sealed class EfFolioGenerator(AppDbContext db) : IFolioGenerator
{
    private const string Sql = """
        MERGE INTO organization.FolioSequences AS target
        USING (SELECT {0} AS CompanyId, {1} AS DocumentType) AS source
        ON target.CompanyId = source.CompanyId AND target.DocumentType = source.DocumentType
        WHEN MATCHED THEN
            UPDATE SET NextValue = target.NextValue + 1
        WHEN NOT MATCHED THEN
            INSERT (Id, CompanyId, DocumentType, NextValue) VALUES (NEWID(), source.CompanyId, source.DocumentType, 1)
        OUTPUT inserted.NextValue;
        """;

    public async Task<string> NextAsync(Guid companyId, string documentType, CancellationToken cancellationToken)
    {
        // A concurrent first-ever folio for the same (company, documentType) pair can, in rare cases,
        // hit SQL Server's documented MERGE race (two sessions both see "not matched"); the unique index
        // on (CompanyId, DocumentType) turns that into a constraint violation instead of a duplicate row,
        // so a short retry is enough — this is not a sustained-contention hot path.
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var results = await db.Database
                    .SqlQueryRaw<int>(Sql, companyId, documentType)
                    .ToListAsync(cancellationToken);

                return $"{documentType}-{results[0]:D6}";
            }
            catch (DbException) when (attempt < 3)
            {
            }
        }

        throw new InvalidOperationException(
            $"No fue posible generar un folio para la empresa {companyId} / tipo {documentType} tras varios intentos.");
    }
}
