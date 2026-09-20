using AssetManagement.Application.Common.Behaviors;
using AssetManagement.Application.Common.Exceptions;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.Common.Security;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AssetManagement.UnitTests.Application.Common;

file sealed record ProtectedRequest : IRequiresPermission
{
    public string PermissionCode => "Assets.Read";
}

file sealed record UnprotectedRequest;

public class AuthorizationBehaviorTests
{
    [Fact]
    public async Task Throws_forbidden_when_caller_is_not_authenticated()
    {
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.IsAuthenticated.Returns(false);
        var permissionChecker = Substitute.For<IPermissionChecker>();
        var behavior = new AuthorizationBehavior<ProtectedRequest, string>(currentUser, permissionChecker);

        var act = () => behavior.Handle(new ProtectedRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Throws_forbidden_when_caller_lacks_the_required_permission()
    {
        var userId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.HasPermissionAsync(userId, "Assets.Read", Arg.Any<CancellationToken>()).Returns(false);
        var behavior = new AuthorizationBehavior<ProtectedRequest, string>(currentUser, permissionChecker);

        var act = () => behavior.Handle(new ProtectedRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Calls_next_when_caller_has_the_required_permission()
    {
        var userId = Guid.NewGuid();
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.IsAuthenticated.Returns(true);
        currentUser.UserId.Returns(userId);
        var permissionChecker = Substitute.For<IPermissionChecker>();
        permissionChecker.HasPermissionAsync(userId, "Assets.Read", Arg.Any<CancellationToken>()).Returns(true);
        var behavior = new AuthorizationBehavior<ProtectedRequest, string>(currentUser, permissionChecker);

        var result = await behavior.Handle(new ProtectedRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Requests_that_do_not_declare_a_permission_pass_through_unchecked()
    {
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.IsAuthenticated.Returns(false);
        var permissionChecker = Substitute.For<IPermissionChecker>();
        var behavior = new AuthorizationBehavior<UnprotectedRequest, string>(currentUser, permissionChecker);

        var result = await behavior.Handle(new UnprotectedRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }
}
