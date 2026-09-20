using AssetManagement.Domain.Audit;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Audit;

public class AuditEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_succeeds_for_a_successful_command()
    {
        var entry = AuditEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Francisco Pérez", "RequestAssetDecommissionCommand", "Assets", "Decommission",
            "{\"AssetId\":\"...\"}", succeeded: true, errorMessage: null, "127.0.0.1", "Mozilla/5.0", "corr-1", Now);

        entry.Succeeded.Should().BeTrue();
        entry.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Create_succeeds_for_a_failed_command_with_an_error_message()
    {
        var entry = AuditEntry.Create(
            null, Guid.NewGuid(), "Francisco Pérez", "RequestAssetDecommissionCommand", "Assets", "Decommission", null,
            succeeded: false, "El activo no existe.", null, null, null, Now);

        entry.Succeeded.Should().BeFalse();
        entry.ErrorMessage.Should().Be("El activo no existe.");
    }

    [Fact]
    public void Create_rejects_empty_command_name()
    {
        var act = () => AuditEntry.Create(null, null, null, "  ", null, null, null, true, null, null, null, null, Now);

        act.Should().Throw<DomainException>();
    }
}
