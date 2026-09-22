using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.Infrastructure.Common;

public sealed class FrontendLinkBuilder(string baseUrl) : IFrontendLinkBuilder
{
    private readonly string _baseUrl = baseUrl.TrimEnd('/');

    public string MyAssignmentUrl(Guid assignmentId) => $"{_baseUrl}/my-assignments/{assignmentId}";

    public string AssignmentUrl(Guid assignmentId) => $"{_baseUrl}/assignments/{assignmentId}";
}
