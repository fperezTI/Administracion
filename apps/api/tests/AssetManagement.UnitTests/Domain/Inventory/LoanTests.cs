using AssetManagement.Domain.Inventory;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.Inventory;

public class LoanTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_active()
    {
        var loan = Loan.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(7), Now, null);

        loan.Status.Should().Be(LoanStatus.Active);
    }

    [Fact]
    public void Create_rejects_an_expected_return_date_in_the_past()
    {
        var act = () => Loan.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1), Now, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Return_marks_the_loan_returned()
    {
        var loan = Loan.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(7), Now, null);
        var returnMovementId = Guid.NewGuid();

        loan.Return(returnMovementId, Now.AddDays(3), null);

        loan.Status.Should().Be(LoanStatus.Returned);
        loan.ReturnMovementId.Should().Be(returnMovementId);
    }

    [Fact]
    public void Return_twice_throws()
    {
        var loan = Loan.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(7), Now, null);
        loan.Return(Guid.NewGuid(), Now, null);

        var act = () => loan.Return(Guid.NewGuid(), Now, null);

        act.Should().Throw<DomainException>();
    }
}
