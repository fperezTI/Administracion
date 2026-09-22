using AssetManagement.Domain.Configuration;
using AssetManagement.Infrastructure.Configuration;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AssetManagement.UnitTests.Infrastructure.Configuration;

public class SystemSettingsProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);

    private static IConfiguration BuildConfiguration(string? tenantId, string? clientId, string? clientSecret, string? senderMailbox) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MicrosoftGraph:TenantId"] = tenantId,
            ["MicrosoftGraph:ClientId"] = clientId,
            ["MicrosoftGraph:ClientSecret"] = clientSecret,
            ["MicrosoftGraph:SenderMailbox"] = senderMailbox,
        }).Build();

    [Fact]
    public async Task Falls_back_to_configuration_when_no_row_exists()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var configuration = BuildConfiguration("config-tenant", "config-client", "config-secret", "config@empresa.com");
        var provider = new SystemSettingsProvider(db, new FakeSecretProtector(), configuration);

        var result = await provider.GetGraphSettingsAsync(CancellationToken.None);

        result.TenantId.Should().Be("config-tenant");
        result.ClientId.Should().Be("config-client");
        result.ClientSecret.Should().Be("config-secret");
        result.SenderMailbox.Should().Be("config@empresa.com");
    }

    [Fact]
    public async Task Database_value_overrides_configuration_field_by_field()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var settings = SystemSettings.CreateEmpty(Now);
        settings.Update("db@empresa.com", null, null, "protected:db-secret", null, Now);
        db.SystemSettings.Add(settings);
        await db.SaveChangesAsync(CancellationToken.None);

        var configuration = BuildConfiguration("config-tenant", "config-client", "config-secret", "config@empresa.com");
        var provider = new SystemSettingsProvider(db, new FakeSecretProtector(), configuration);

        var result = await provider.GetGraphSettingsAsync(CancellationToken.None);

        result.SenderMailbox.Should().Be("db@empresa.com");
        result.ClientSecret.Should().Be("db-secret");
        // Left empty in the database row, so these two still fall back to configuration.
        result.TenantId.Should().Be("config-tenant");
        result.ClientId.Should().Be("config-client");
    }

    [Fact]
    public async Task Treats_an_undecryptable_secret_as_not_configured_instead_of_throwing()
    {
        using var db = InMemoryAppDbContextFactory.Create();
        var settings = SystemSettings.CreateEmpty(Now);
        settings.Update(null, null, null, "not-something-the-fake-protector-can-decrypt", null, Now);
        db.SystemSettings.Add(settings);
        await db.SaveChangesAsync(CancellationToken.None);

        var configuration = BuildConfiguration(null, null, null, null);
        var provider = new SystemSettingsProvider(db, new FakeSecretProtector(), configuration);

        var result = await provider.GetGraphSettingsAsync(CancellationToken.None);

        result.ClientSecret.Should().BeNull();
    }
}
