using System.Net;
using AssetManagement.Domain.Identity;
using AssetManagement.Domain.Notifications;
using AssetManagement.Domain.Organization;
using AssetManagement.Infrastructure.DataRetention;
using AssetManagement.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AssetManagement.IntegrationTests;

/// <summary>F12's privacy/retention mechanisms (see docs/privacy-retention.md, ADR 0014) against real SQL
/// Server: anonymizing a user end-to-end via the real endpoint, and the notification retention purge's
/// actual delete query.</summary>
public class PrivacyRetentionTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public async Task Anonymizing_a_user_scrubs_their_profile_and_deactivates_them()
    {
        var (client, _, targetUserId) = await ArrangeAsync("Users.Update");

        var response = await client.PostAsync($"/api/v1/users/{targetUserId}/anonymize", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.FindAsync(targetUserId);
        user!.DisplayName.Should().Be("Usuario eliminado");
        user.IsActive.Should().BeFalse();
        user.AnonymizedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Anonymizing_the_same_user_twice_is_rejected()
    {
        var (client, _, targetUserId) = await ArrangeAsync("Users.Update");
        (await client.PostAsync($"/api/v1/users/{targetUserId}/anonymize", content: null)).EnsureSuccessStatusCode();

        var response = await client.PostAsync($"/api/v1/users/{targetUserId}/anonymize", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Notification_retention_purge_deletes_only_notifications_past_the_configured_window()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userId = Guid.NewGuid(); // no FK on Notification.UserId — a bare id is enough for this purge test
        var old = Notification.Create(userId, "Test", "Vieja", "Cuerpo", null, Now.AddDays(-120));
        var recent = Notification.Create(userId, "Test", "Reciente", "Cuerpo", null, Now.AddDays(-10));
        db.Notifications.AddRange(old, recent);
        await db.SaveChangesAsync();

        var service = new NotificationRetentionBackgroundService(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
            scope.ServiceProvider.GetRequiredService<ILogger<NotificationRetentionBackgroundService>>());

        await service.PurgeAsync(retentionDays: 90, CancellationToken.None);

        var remainingIds = db.Notifications.Where(n => n.Id == old.Id || n.Id == recent.Id).Select(n => n.Id).ToList();
        remainingIds.Should().NotContain(old.Id);
        remainingIds.Should().Contain(recent.Id);
    }

    private async Task<(HttpClient Client, Guid CompanyId, Guid TargetUserId)> ArrangeAsync(params string[] permissionCodes)
    {
        var entraObjectId = Guid.NewGuid();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Oid", entraObjectId.ToString());
        client.DefaultRequestHeaders.Add("Test-Name", "Privacy Tester");
        client.DefaultRequestHeaders.Add("Test-Email", $"{entraObjectId}@example.com");

        (await client.GetAsync("/api/v1/me")).EnsureSuccessStatusCode();

        Guid companyId;
        Guid targetUserId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.EntraObjectId == entraObjectId);

            var company = Company.Create(
                $"Privacidad {entraObjectId}", "Empresa Privacidad", $"TAX-PRIV-{entraObjectId}", "MXN", "America/Mexico_City", Now);
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            companyId = company.Id;

            user.GrantCompanyAccess(companyId, Now);

            var permissionIds = await db.Permissions
                .Where(p => permissionCodes.Contains(p.Module + "." + p.Action))
                .Select(p => p.Id)
                .ToListAsync();
            var role = Role.Create($"Rol Privacidad {entraObjectId}", null, Now);
            role.SetPermissions(permissionIds);
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            user.AssignRole(role.Id, null, Now);
            await db.SaveChangesAsync();

            var target = User.Provision(Guid.NewGuid(), "Persona De Prueba", "persona-prueba@example.com", Now);
            db.Users.Add(target);
            await db.SaveChangesAsync();
            targetUserId = target.Id;
        }

        return (client, companyId, targetUserId);
    }
}
