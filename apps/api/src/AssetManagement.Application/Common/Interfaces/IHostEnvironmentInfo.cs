namespace AssetManagement.Application.Common.Interfaces;

/// <summary>Thin port over the hosting environment name, so Application does not depend on ASP.NET Core.</summary>
public interface IHostEnvironmentInfo
{
    public string EnvironmentName { get; }
}
