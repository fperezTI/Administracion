namespace AssetManagement.Application.Common.Interfaces;

/// <summary>Issues the next folio for a company + document type, atomically (see
/// Infrastructure's EfFolioGenerator — a per-company, per-document-type monotonic counter, pedido §8
/// "prefijos y secuencias de folios").</summary>
public interface IFolioGenerator
{
    public Task<string> NextAsync(Guid companyId, string documentType, CancellationToken cancellationToken);
}
