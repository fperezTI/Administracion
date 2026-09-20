namespace AssetManagement.Application.Common.Security;

/// <summary>
/// A command implements this (empty marker, same shape as <see cref="IRequiresPermission"/>) to have
/// <c>AuditBehavior</c> write an <c>AuditEntry</c> for every attempt, successful or not. Module/Action are
/// derived from <see cref="IRequiresPermission.PermissionCode"/> when the command also implements it
/// (true for almost every write command) — see the F8 plan, decision 3, for why this is a real,
/// queryable trail rather than a before/after field diff with redaction rules.
/// </summary>
public interface IAuditableCommand
{
}
