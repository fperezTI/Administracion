namespace AssetManagement.Application.Common.Interfaces;

/// <summary>Builds absolute URLs into the frontend for use outside an HTTP request context (e.g. inside
/// an email body) — Application has no other way to know the frontend's public origin, see
/// <c>Frontend:BaseUrl</c> in Infrastructure/DependencyInjection.cs.</summary>
public interface IFrontendLinkBuilder
{
    public string MyAssignmentUrl(Guid assignmentId);
}
