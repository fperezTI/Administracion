using AssetManagement.Application.Common.Events;
using AssetManagement.Application.Common.Interfaces;
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
using AssetManagement.Domain.SharedKernel;
using AssetManagement.Domain.Signature;
using AssetManagement.Domain.SparePartsAndConsumables;
using AssetManagement.Domain.Templates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AssetManagement.Infrastructure.Persistence;

/// <summary>
/// Single EF Core context for the monolith. Logical multi-company isolation is enforced per entity via
/// global query filters as each bounded context adds its DbSets (see docs/multi-company.md) — this is
/// defense in depth on top of the explicit membership checks every handler performs, never a
/// replacement for them.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentCompanyContext companyContext, IPublisher publisher)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<UserCompany> UserCompanies => Set<UserCompany>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<OrgUnitType> OrgUnitTypes => Set<OrgUnitType>();

    public DbSet<OrgUnit> OrgUnits => Set<OrgUnit>();

    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();

    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();

    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<AssetCustomFieldValue> AssetCustomFieldValues => Set<AssetCustomFieldValue>();

    public DbSet<AssetTag> AssetTags => Set<AssetTag>();

    public DbSet<Movement> Movements => Set<Movement>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<Transfer> Transfers => Set<Transfer>();

    public DbSet<SignatureRecord> SignatureRecords => Set<SignatureRecord>();

    public DbSet<ApprovalFlowDefinition> ApprovalFlowDefinitions => Set<ApprovalFlowDefinition>();

    public DbSet<ApprovalInstance> ApprovalInstances => Set<ApprovalInstance>();

    public DbSet<Template> Templates => Set<Template>();

    public DbSet<MaintenanceOrder> MaintenanceOrders => Set<MaintenanceOrder>();

    public DbSet<MaintenanceChecklistDefinition> MaintenanceChecklistDefinitions => Set<MaintenanceChecklistDefinition>();

    public DbSet<Warranty> Warranties => Set<Warranty>();

    public DbSet<SparePart> SpareParts => Set<SparePart>();

    public DbSet<Consumable> Consumables => Set<Consumable>();

    public DbSet<ConsumableStockMovement> ConsumableStockMovements => Set<ConsumableStockMovement>();

    public DbSet<InternalRequest> InternalRequests => Set<InternalRequest>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    /// <summary>Not part of IApplicationDbContext — only EfFolioGenerator (Infrastructure) needs it, via
    /// the raw-SQL atomic increment that generates folios without optimistic-concurrency retries.</summary>
    internal DbSet<FolioSequenceRecord> FolioSequences => Set<FolioSequenceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Membership-based defense in depth: even if a handler's explicit check were ever missing, no
        // query on this context can return rows for a company the caller does not belong to.
        modelBuilder.Entity<OrgUnit>().HasQueryFilter(o => companyContext.AccessibleCompanyIds.Contains(o.CompanyId));
        modelBuilder.Entity<Asset>().HasQueryFilter(a => companyContext.AccessibleCompanyIds.Contains(a.CompanyId));
        modelBuilder.Entity<Movement>().HasQueryFilter(m => companyContext.AccessibleCompanyIds.Contains(m.CompanyId));
        modelBuilder.Entity<Assignment>().HasQueryFilter(a => companyContext.AccessibleCompanyIds.Contains(a.CompanyId));
        modelBuilder.Entity<Loan>().HasQueryFilter(l => companyContext.AccessibleCompanyIds.Contains(l.CompanyId));
        modelBuilder.Entity<SignatureRecord>().HasQueryFilter(s => companyContext.AccessibleCompanyIds.Contains(s.CompanyId));
        modelBuilder.Entity<ApprovalInstance>().HasQueryFilter(i => companyContext.AccessibleCompanyIds.Contains(i.CompanyId));
        // The one aggregate that genuinely belongs to two companies at once (F5) — OR instead of a
        // single-company match, so both the source and destination company can see it.
        modelBuilder.Entity<Transfer>().HasQueryFilter(t =>
            companyContext.AccessibleCompanyIds.Contains(t.FromCompanyId) || companyContext.AccessibleCompanyIds.Contains(t.ToCompanyId));
        // ApprovalFlowDefinition.CompanyId is nullable-as-scope (null = applies tenant-wide), not
        // per-row isolation, so it gets no query filter — same treatment as Company/Role, the other
        // globally-visible entities in this system.
        modelBuilder.Entity<MaintenanceOrder>().HasQueryFilter(o => companyContext.AccessibleCompanyIds.Contains(o.CompanyId));
        modelBuilder.Entity<Warranty>().HasQueryFilter(w => companyContext.AccessibleCompanyIds.Contains(w.CompanyId));
        modelBuilder.Entity<SparePart>().HasQueryFilter(p => companyContext.AccessibleCompanyIds.Contains(p.CompanyId));
        modelBuilder.Entity<Consumable>().HasQueryFilter(c => companyContext.AccessibleCompanyIds.Contains(c.CompanyId));
        modelBuilder.Entity<ConsumableStockMovement>().HasQueryFilter(m => companyContext.AccessibleCompanyIds.Contains(m.CompanyId));
        // MaintenanceChecklistDefinition has no CompanyId (tenant-wide catalog, same treatment as
        // Template) — no query filter needed.
        modelBuilder.Entity<InternalRequest>().HasQueryFilter(r => companyContext.AccessibleCompanyIds.Contains(r.CompanyId));
        modelBuilder.Entity<Document>().HasQueryFilter(d => companyContext.AccessibleCompanyIds.Contains(d.CompanyId));
        // ImportBatchProcessor (a BackgroundService's own DI scope, no HttpContext) reads this with
        // IgnoreQueryFilters() explicitly and trusts the batch's own CompanyId instead — see the F9 plan.
        modelBuilder.Entity<ImportBatch>().HasQueryFilter(b => companyContext.AccessibleCompanyIds.Contains(b.CompanyId));
        // AuditEntry deliberately has NO company-based query filter — same treatment as Company/Role
        // (no membership filter either): Audit.Read is a global, tenant-wide admin permission, not one
        // scoped per company. This also sidesteps a real gap discovered while building F8: nothing in
        // this app ever sends X-Active-Company-Id (every module reads an explicit CompanyId parameter
        // instead, see multi-company.md), so ICurrentCompanyContext.CompanyId is always null in
        // practice — scoping by it here would have put every entry in a company-agnostic bucket anyway,
        // silently defeating a per-company filter. CompanyId is still recorded on the entry itself (from
        // the command's own data, resolved in AuditBehavior) purely as a display/filter convenience.
        // Notification has no CompanyId-based query filter either — self-service data scoped by UserId
        // in the query itself (GetMyNotificationsQuery), not by company membership.

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// F4 is the first phase that actually needs domain events (pedido §10: Approvals publishes
    /// ApprovalCompleted/ApprovalRejected for whichever module requested the approval to react to) — the
    /// <c>Raise</c>/<c>DomainEvents</c> plumbing on <see cref="AggregateRoot{TId}"/> existed since F0 but
    /// nothing dispatched it until now. Events are collected before saving and published only after the
    /// save actually succeeds, so a failed save never fires events for changes that didn't happen.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = ChangeTracker.Entries<IHasDomainEvents>()
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        foreach (var entry in ChangeTracker.Entries<IHasDomainEvents>())
        {
            entry.Entity.ClearDomainEvents();
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in events)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
            await publisher.Publish(notification, cancellationToken);
        }

        return result;
    }
}
