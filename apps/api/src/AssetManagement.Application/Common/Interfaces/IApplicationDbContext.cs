using AssetManagement.Domain.Approvals;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Audit;
using AssetManagement.Domain.Documents;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.ImportExport;
using AssetManagement.Domain.Inventory;
using AssetManagement.Domain.Maintenance;
using AssetManagement.Domain.Notifications;
using AssetManagement.Domain.Organization;
using AssetManagement.Domain.Requests;
using AssetManagement.Domain.Signature;
using AssetManagement.Domain.SparePartsAndConsumables;
using AssetManagement.Domain.Templates;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Application.Common.Interfaces;

/// <summary>
/// Application's view of persistence: enough to express queries and track aggregates, without
/// Application depending on the concrete EF Core DbContext or any Infrastructure type. Writes still go
/// through aggregate methods (see docs/architecture/overview.md) — this is not a generic repository
/// escape hatch, it is how Application reaches the aggregates and read models it owns.
/// </summary>
public interface IApplicationDbContext
{
    public DbSet<User> Users { get; }

    public DbSet<UserRole> UserRoles { get; }

    public DbSet<UserCompany> UserCompanies { get; }

    public DbSet<Role> Roles { get; }

    public DbSet<RolePermission> RolePermissions { get; }

    public DbSet<Permission> Permissions { get; }

    public DbSet<Company> Companies { get; }

    public DbSet<OrgUnitType> OrgUnitTypes { get; }

    public DbSet<OrgUnit> OrgUnits { get; }

    public DbSet<AssetCategory> AssetCategories { get; }

    public DbSet<CustomFieldDefinition> CustomFieldDefinitions { get; }

    public DbSet<Asset> Assets { get; }

    public DbSet<AssetCustomFieldValue> AssetCustomFieldValues { get; }

    public DbSet<AssetTag> AssetTags { get; }

    public DbSet<Movement> Movements { get; }

    public DbSet<Assignment> Assignments { get; }

    public DbSet<Loan> Loans { get; }

    public DbSet<Transfer> Transfers { get; }

    public DbSet<SignatureRecord> SignatureRecords { get; }

    public DbSet<ApprovalFlowDefinition> ApprovalFlowDefinitions { get; }

    public DbSet<ApprovalInstance> ApprovalInstances { get; }

    public DbSet<Template> Templates { get; }

    public DbSet<MaintenanceOrder> MaintenanceOrders { get; }

    public DbSet<MaintenanceChecklistDefinition> MaintenanceChecklistDefinitions { get; }

    public DbSet<Warranty> Warranties { get; }

    public DbSet<SparePart> SpareParts { get; }

    public DbSet<Consumable> Consumables { get; }

    public DbSet<ConsumableStockMovement> ConsumableStockMovements { get; }

    public DbSet<InternalRequest> InternalRequests { get; }

    public DbSet<Document> Documents { get; }

    public DbSet<Notification> Notifications { get; }

    public DbSet<AuditEntry> AuditEntries { get; }

    public DbSet<ImportBatch> ImportBatches { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
