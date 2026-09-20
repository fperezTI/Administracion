using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.SystemInfo;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AssetManagement.UnitTests.SystemInfo;

public class GetSystemInfoQueryHandlerTests
{
    [Fact]
    public async Task Returns_product_name_environment_and_server_time_from_its_ports()
    {
        var fixedTime = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(fixedTime);

        var environmentInfo = Substitute.For<IHostEnvironmentInfo>();
        environmentInfo.EnvironmentName.Returns("Testing");

        var handler = new GetSystemInfoQueryHandler(clock, environmentInfo);

        var result = await handler.Handle(new GetSystemInfoQuery(), CancellationToken.None);

        result.Product.Should().Be("IT Asset & Infrastructure Management");
        result.Environment.Should().Be("Testing");
        result.ServerTimeUtc.Should().Be(fixedTime);
    }
}
