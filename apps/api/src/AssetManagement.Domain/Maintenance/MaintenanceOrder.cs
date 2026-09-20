using AssetManagement.Domain.Assets;
using AssetManagement.Domain.SharedKernel;

namespace AssetManagement.Domain.Maintenance;

/// <summary>
/// A work order against an asset (pedido: "preventivo/correctivo... checklists"). Opening one moves the
/// asset to <see cref="AssetStatus.InMaintenance"/> (via <c>Asset.ChangeStatus</c> in the Application
/// handler, never here — this aggregate has no reference to <see cref="Asset"/>). Closing requires a
/// result and notes — "no se cierra sin resultado, condición final y evidencias requeridas". The result is
/// deliberately restricted to <see cref="AssetStatus.InWarehouse"/> or <see cref="AssetStatus.Damaged"/>:
/// <see cref="AssetStatus.PendingDecommission"/> is reachable from <c>InMaintenance</c> in
/// <c>AssetStateMachine</c> but must always go through <c>RequestAssetDecommissionCommand</c> (F4), which
/// also starts the required approval — entering it directly from here would leave the asset "pending
/// decommission" with no approval ever requested. <see cref="AssetStatus.UnderWarranty"/> is left
/// unconnected in V1 for the same reason F3 left <c>InTransit → Assigned</c> unconnected until F5: the
/// pedido does not detail a distinct "sent to vendor under warranty" workflow, so none is invented here —
/// see the F6 plan.
/// </summary>
public sealed class MaintenanceOrder : AuditableAggregateRoot<Guid>
{
    private readonly List<MaintenanceOrderChecklistResult> _checklistResults = [];

    public Guid CompanyId { get; private set; }
    public Guid AssetId { get; private set; }
    public string Folio { get; private set; } = null!;
    public MaintenanceOrderType Type { get; private set; }
    public MaintenanceOrderStatus Status { get; private set; }
    public string Description { get; private set; } = null!;

    /// <summary>Together identify the snapshotted <see cref="MaintenanceChecklistVersion"/> — that entity
    /// has no single surrogate id of its own (composite key, same as <see cref="Templates.TemplateVersion"/>),
    /// so both parts of its key are kept here instead of a single FK.</summary>
    public Guid? ChecklistDefinitionId { get; private set; }
    public int? ChecklistVersionNumber { get; private set; }

    public DateTimeOffset OpenedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public AssetStatus? ResultStatus { get; private set; }
    public string? ResultNotes { get; private set; }

    public IReadOnlyCollection<MaintenanceOrderChecklistResult> ChecklistResults => _checklistResults.AsReadOnly();

    private MaintenanceOrder()
    {
    }

    private MaintenanceOrder(
        Guid id, Guid companyId, Guid assetId, string folio, MaintenanceOrderType type, string description,
        Guid? checklistDefinitionId, int? checklistVersionNumber, IReadOnlyList<string>? checklistItems,
        DateTimeOffset nowUtc, Guid? createdByUserId)
        : base(id, nowUtc, createdByUserId)
    {
        CompanyId = companyId;
        AssetId = assetId;
        Folio = folio;
        Type = type;
        Status = MaintenanceOrderStatus.Open;
        Description = description;
        ChecklistDefinitionId = checklistDefinitionId;
        ChecklistVersionNumber = checklistVersionNumber;
        OpenedAtUtc = nowUtc;

        if (checklistItems is not null)
        {
            for (var i = 0; i < checklistItems.Count; i++)
            {
                _checklistResults.Add(
                    MaintenanceOrderChecklistResult.Create(id, i, checklistItems[i], isCompleted: false, notes: null));
            }
        }
    }

    public static MaintenanceOrder Open(
        Guid companyId, Guid assetId, string folio, MaintenanceOrderType type, string description,
        Guid? checklistDefinitionId, int? checklistVersionNumber, IReadOnlyList<string>? checklistItems,
        DateTimeOffset nowUtc, Guid? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(folio))
        {
            throw new DomainException("El folio de la orden de mantenimiento es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("La descripción de la orden de mantenimiento es obligatoria.");
        }

        return new MaintenanceOrder(
            Guid.NewGuid(), companyId, assetId, folio.Trim(), type, description.Trim(), checklistDefinitionId,
            checklistVersionNumber, checklistItems, nowUtc, createdByUserId);
    }

    /// <summary><paramref name="checklistItemResults"/> is keyed by <see cref="MaintenanceOrderChecklistResult.ItemIndex"/>
    /// — items not present are left as "not completed", matching an unchecked checklist box.</summary>
    public void Close(
        AssetStatus resultStatus, string resultNotes,
        IReadOnlyDictionary<int, (bool IsCompleted, string? Notes)>? checklistItemResults,
        DateTimeOffset nowUtc, Guid? updatedByUserId)
    {
        if (Status != MaintenanceOrderStatus.Open)
        {
            throw new DomainException("Solo una orden de mantenimiento abierta puede cerrarse.");
        }

        if (resultStatus != AssetStatus.InWarehouse && resultStatus != AssetStatus.Damaged)
        {
            throw new DomainException(
                "El resultado de una orden de mantenimiento solo puede ser 'En almacén' (reparado) o 'Dañado' (no se pudo reparar).");
        }

        if (string.IsNullOrWhiteSpace(resultNotes))
        {
            throw new DomainException("Cerrar una orden de mantenimiento requiere describir el resultado (evidencia).");
        }

        if (checklistItemResults is not null)
        {
            foreach (var item in _checklistResults)
            {
                if (checklistItemResults.TryGetValue(item.ItemIndex, out var result))
                {
                    item.RecordResult(result.IsCompleted, result.Notes);
                }
            }
        }

        Status = MaintenanceOrderStatus.Closed;
        ResultStatus = resultStatus;
        ResultNotes = resultNotes.Trim();
        ClosedAtUtc = nowUtc;
        RecordUpdate(nowUtc, updatedByUserId);
    }
}
