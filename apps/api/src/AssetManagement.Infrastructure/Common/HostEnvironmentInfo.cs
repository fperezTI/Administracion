using AssetManagement.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace AssetManagement.Infrastructure.Common;

public sealed class HostEnvironmentInfo(IHostEnvironment hostEnvironment) : IHostEnvironmentInfo
{
    public string EnvironmentName => hostEnvironment.EnvironmentName;
}
