namespace AssetManagement.Domain.ImportExport;

/// <summary>Pedido: "modo transaccional completo o solo-filas-válidas" — chosen by the user when
/// confirming a validated <see cref="ImportBatch"/>, not at upload time.</summary>
public enum ImportCommitMode
{
    AllOrNothing,
    ValidRowsOnly,
}
