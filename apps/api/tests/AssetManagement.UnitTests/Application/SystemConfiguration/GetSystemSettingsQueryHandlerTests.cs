using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.SystemConfiguration;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Application.SystemConfiguration;

public class GetSystemSettingsQueryHandlerTests
{
    private sealed class FakeSystemSettingsProvider(EffectiveGraphSettings settings) : ISystemSettingsProvider
    {
        public Task<EffectiveGraphSettings> GetGraphSettingsAsync(CancellationToken cancellationToken) => Task.FromResult(settings);
    }

    [Fact]
    public async Task Never_includes_the_client_secret_itself()
    {
        var provider = new FakeSystemSettingsProvider(new EffectiveGraphSettings(
            Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "top-secret-value", "buzon@empresa.com"));
        var handler = new GetSystemSettingsQueryHandler(provider);

        var dto = await handler.Handle(new GetSystemSettingsQuery(), CancellationToken.None);

        dto.HasGraphClientSecretConfigured.Should().BeTrue();
        dto.SenderMailbox.Should().Be("buzon@empresa.com");
        typeof(SystemSettingsDto).GetProperties().Select(p => p.Name).Should().NotContain("GraphClientSecret");
    }

    [Fact]
    public async Task Reports_the_secret_as_not_configured_when_it_is_empty()
    {
        var provider = new FakeSystemSettingsProvider(new EffectiveGraphSettings(null, null, null, null));
        var handler = new GetSystemSettingsQueryHandler(provider);

        var dto = await handler.Handle(new GetSystemSettingsQuery(), CancellationToken.None);

        dto.HasGraphClientSecretConfigured.Should().BeFalse();
        dto.SenderMailbox.Should().BeEmpty();
        dto.GraphTenantId.Should().BeEmpty();
        dto.GraphClientId.Should().BeEmpty();
    }
}
