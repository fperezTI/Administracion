using System.Net;
using System.Net.Http.Json;
using AssetManagement.Application.SystemInfo;
using FluentAssertions;
using Xunit;

namespace AssetManagement.IntegrationTests;

public class SystemInfoEndpointTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task Liveness_health_check_returns_200()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task System_info_endpoint_returns_product_and_environment()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/system/info");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SystemInfoResponse>();
        body.Should().NotBeNull();
        body!.Product.Should().Be("IT Asset & Infrastructure Management");
    }
}
