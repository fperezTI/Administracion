using AssetManagement.Application.Common.Interfaces;
using MediatR;

namespace AssetManagement.Application.SystemInfo;

/// <summary>
/// Minimal read-only vertical slice used to verify the API -> Application -> Infrastructure wiring
/// end to end during the foundational increment. Not a business feature.
/// </summary>
public sealed record GetSystemInfoQuery : IRequest<SystemInfoResponse>;

public sealed record SystemInfoResponse(string Product, string Environment, DateTimeOffset ServerTimeUtc);

public sealed class GetSystemInfoQueryHandler(IClock clock, IHostEnvironmentInfo environmentInfo)
    : IRequestHandler<GetSystemInfoQuery, SystemInfoResponse>
{
    public Task<SystemInfoResponse> Handle(GetSystemInfoQuery request, CancellationToken cancellationToken)
    {
        var response = new SystemInfoResponse(
            Product: "IT Asset & Infrastructure Management",
            Environment: environmentInfo.EnvironmentName,
            ServerTimeUtc: clock.UtcNow);

        return Task.FromResult(response);
    }
}
