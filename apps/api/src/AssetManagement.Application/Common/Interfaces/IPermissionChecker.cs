namespace AssetManagement.Application.Common.Interfaces;

public interface IPermissionChecker
{
    public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken);
}
