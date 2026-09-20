using AssetManagement.Application.Inventory;
using AssetManagement.Domain.Assets;
using AssetManagement.Domain.Identity;
using AssetManagement.UnitTests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AssetManagement.UnitTests.Application.Inventory;

public class CreateLoanCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Creates_a_loan_and_moves_the_asset_to_OnLoan_immediately()
    {
        var companyId = Guid.NewGuid();
        var companyContext = new FakeCurrentCompanyContext { AccessibleCompanyIds = [companyId] };
        using var db = InMemoryAppDbContextFactory.Create(companyContext);
        var asset = Asset.Create(
            companyId, Guid.NewGuid(), "ASSET-000001", "Dell", "Latitude", null, null,
            PhysicalCondition.Good, Now, null);
        var borrower = User.Provision(Guid.NewGuid(), "Ada Lovelace", "ada@example.com", Now);
        borrower.GrantCompanyAccess(companyId, Now);
        db.Assets.Add(asset);
        db.Users.Add(borrower);
        await db.SaveChangesAsync();

        var handler = new CreateLoanCommandHandler(
            db, companyContext, new FakeCurrentUserContext(), new FakeFolioGenerator(), new FakeClock(Now));

        var expectedReturn = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(7);
        var result = await handler.Handle(new CreateLoanCommand(asset.Id, borrower.Id, expectedReturn, null), CancellationToken.None);

        var reloadedAsset = await db.Assets.SingleAsync(a => a.Id == asset.Id);
        reloadedAsset.Status.Should().Be(AssetStatus.OnLoan);

        var loan = await db.Loans.SingleAsync(l => l.Id == result.LoanId);
        loan.ExpectedReturnDate.Should().Be(expectedReturn);
    }
}
