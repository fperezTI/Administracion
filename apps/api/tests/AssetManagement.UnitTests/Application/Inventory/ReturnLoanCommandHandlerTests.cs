using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Inventory;
using AssetManagement.Domain.SharedKernel;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Inventory;

public class ReturnLoanCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Returning_an_active_loan_moves_the_asset_back_to_InWarehouse()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000001", "Dell", "Latitude", null, null,
            PhysicalCondition.Good, Now, null);
        asset.ChangeStatus(AssetStatus.OnLoan, Now, null);

        var movement = Movement.Create(
            companyId, asset.Id, MovementType.Loan, "MOV-LOAN-000001", startsCompleted: true,
            null, null, null, Guid.NewGuid(), null, Now, null);
        var loan = Loan.Create(
            companyId, asset.Id, Guid.NewGuid(), movement.Id, DateOnly.FromDateTime(Now.UtcDateTime).AddDays(7), Now, null);

        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Loans.Add(loan);
        await db.SaveChangesAsync();

        var handler = new ReturnLoanCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now.AddDays(3)));
        await handler.Handle(new ReturnLoanCommand(loan.Id, null), CancellationToken.None);

        var reloadedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        reloadedAsset.Status.Should().Be(AssetStatus.InWarehouse);

        var reloadedLoan = await db.Loans.SingleAsync(l => l.Id == loan.Id);
        reloadedLoan.Status.Should().Be(LoanStatus.Returned);
    }

    [Fact]
    public async Task Returning_an_already_returned_loan_throws()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000001", "Dell", "Latitude", null, null,
            PhysicalCondition.Good, Now, null);
        asset.ChangeStatus(AssetStatus.OnLoan, Now, null);
        asset.ChangeStatus(AssetStatus.InWarehouse, Now, null);

        var movement = Movement.Create(
            companyId, asset.Id, MovementType.Loan, "MOV-LOAN-000001", startsCompleted: true,
            null, null, null, Guid.NewGuid(), null, Now, null);
        var loan = Loan.Create(
            companyId, asset.Id, Guid.NewGuid(), movement.Id, DateOnly.FromDateTime(Now.UtcDateTime).AddDays(7), Now, null);
        loan.Return(Guid.NewGuid(), Now, null);

        db.Assets.Add(asset);
        db.Movements.Add(movement);
        db.Loans.Add(loan);
        await db.SaveChangesAsync();

        var handler = new ReturnLoanCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));
        var act = () => handler.Handle(new ReturnLoanCommand(loan.Id, null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
