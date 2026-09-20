using AssetManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AssetManagement.Infrastructure.Security;

public sealed class HttpContextCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public Guid? UserId => httpContextAccessor.HttpContext?.Items[RequestContextKeys.LocalUserId] as Guid?;

    public Guid? EntraObjectId => httpContextAccessor.HttpContext?.Items[RequestContextKeys.EntraObjectId] as Guid?;

    public string? DisplayName => httpContextAccessor.HttpContext?.Items[RequestContextKeys.DisplayName] as string;

    public string? Email => httpContextAccessor.HttpContext?.Items[RequestContextKeys.Email] as string;

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? CorrelationId => httpContextAccessor.HttpContext?.TraceIdentifier;
}
