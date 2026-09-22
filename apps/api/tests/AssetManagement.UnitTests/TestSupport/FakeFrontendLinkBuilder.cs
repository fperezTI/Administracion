using AssetManagement.Application.Common.Interfaces;

namespace AssetManagement.UnitTests.TestSupport;

internal sealed class FakeFrontendLinkBuilder : IFrontendLinkBuilder
{
    public string MyAssignmentUrl(Guid assignmentId) => $"https://test.invalid/my-assignments/{assignmentId}";
}
