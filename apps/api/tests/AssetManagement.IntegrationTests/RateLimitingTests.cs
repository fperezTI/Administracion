using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>F12's abuse-mitigation rate limiter (see Program.cs) — verified with its own tiny override of
/// <c>RateLimiting:PermitLimit</c> (the shared <see cref="ApiWebApplicationFactory"/> raises it sky-high
/// for every other test in this suite, so bursty tests never trip it; this is the one place it's
/// deliberately tightened back down to actually exercise a 429).</summary>
public class RateLimitingTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    [Fact]
    public async Task Exceeding_the_configured_limit_returns_429_with_retry_after()
    {
        using var tightlyLimitedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:PermitLimit", "3");
            builder.UseSetting("RateLimiting:WindowSeconds", "30");
        });
        var client = tightlyLimitedFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", Guid.NewGuid().ToString());

        HttpResponseMessage? rejected = null;
        for (var i = 0; i < 6 && rejected is null; i++)
        {
            var response = await client.GetAsync("/api/v1/system/info");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rejected = response;
            }
        }

        rejected.Should().NotBeNull("the limiter should reject a burst that exceeds PermitLimit within the window");
        rejected!.Headers.RetryAfter.Should().NotBeNull();
    }
}
