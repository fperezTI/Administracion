using AssetManagement.Domain.ImportExport;
using AssetManagement.Domain.SharedKernel;
using FluentAssertions;
using Xunit;

namespace AssetManagement.UnitTests.Domain.ImportExport;

public class ImportBatchTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();

    private static ImportBatch CreateBatch() => ImportBatch.Create(Guid.NewGuid(), "activos.csv", "imports/x/y.csv", Now, UserId);

    [Fact]
    public void Create_starts_Queued()
    {
        var batch = CreateBatch();

        batch.Status.Should().Be(ImportBatchStatus.Queued);
    }

    [Fact]
    public void Create_rejects_empty_file_name()
    {
        var act = () => ImportBatch.Create(Guid.NewGuid(), "  ", "path.csv", Now, UserId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void BeginValidation_from_Queued_succeeds()
    {
        var batch = CreateBatch();

        batch.BeginValidation(Now);

        batch.Status.Should().Be(ImportBatchStatus.Validating);
    }

    [Fact]
    public void BeginValidation_twice_throws()
    {
        var batch = CreateBatch();
        batch.BeginValidation(Now);

        var act = () => batch.BeginValidation(Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CompleteValidation_from_Validating_succeeds_with_counts()
    {
        var batch = CreateBatch();
        batch.BeginValidation(Now);

        batch.CompleteValidation(10, 8, 2, "[]", Now);

        batch.Status.Should().Be(ImportBatchStatus.Validated);
        batch.TotalRows.Should().Be(10);
        batch.ValidRows.Should().Be(8);
        batch.InvalidRows.Should().Be(2);
    }

    [Fact]
    public void CompleteValidation_without_BeginValidation_throws()
    {
        var batch = CreateBatch();

        var act = () => batch.CompleteValidation(10, 10, 0, "[]", Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FailValidation_from_Validating_moves_to_Failed()
    {
        var batch = CreateBatch();
        batch.BeginValidation(Now);

        batch.FailValidation("No se pudo leer el archivo.", Now);

        batch.Status.Should().Be(ImportBatchStatus.Failed);
        batch.ErrorMessage.Should().Be("No se pudo leer el archivo.");
    }

    [Fact]
    public void RequestCommit_from_Validated_with_no_invalid_rows_succeeds()
    {
        var batch = CreateBatch();
        batch.BeginValidation(Now);
        batch.CompleteValidation(5, 5, 0, "[]", Now);

        batch.RequestCommit(ImportCommitMode.AllOrNothing, Now, UserId);

        batch.Status.Should().Be(ImportBatchStatus.Processing);
        batch.CommitMode.Should().Be(ImportCommitMode.AllOrNothing);
    }

    [Fact]
    public void RequestCommit_AllOrNothing_with_invalid_rows_throws()
    {
        var batch = CreateBatch();
        batch.BeginValidation(Now);
        batch.CompleteValidation(5, 3, 2, "[]", Now);

        var act = () => batch.RequestCommit(ImportCommitMode.AllOrNothing, Now, UserId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RequestCommit_ValidRowsOnly_with_invalid_rows_succeeds()
    {
        var batch = CreateBatch();
        batch.BeginValidation(Now);
        batch.CompleteValidation(5, 3, 2, "[]", Now);

        batch.RequestCommit(ImportCommitMode.ValidRowsOnly, Now, UserId);

        batch.Status.Should().Be(ImportBatchStatus.Processing);
    }

    [Fact]
    public void RequestCommit_before_Validated_throws()
    {
        var batch = CreateBatch();

        var act = () => batch.RequestCommit(ImportCommitMode.ValidRowsOnly, Now, UserId);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CompleteProcessing_with_zero_failed_rows_moves_to_Completed()
    {
        var batch = CreateBatch();
        batch.BeginValidation(Now);
        batch.CompleteValidation(5, 5, 0, "[]", Now);
        batch.RequestCommit(ImportCommitMode.AllOrNothing, Now, UserId);

        batch.CompleteProcessing(5, 0, "[]", Now);

        batch.Status.Should().Be(ImportBatchStatus.Completed);
        batch.SucceededRows.Should().Be(5);
    }

    [Fact]
    public void CompleteProcessing_with_failed_rows_moves_to_CompletedWithErrors()
    {
        var batch = CreateBatch();
        batch.BeginValidation(Now);
        batch.CompleteValidation(5, 3, 2, "[]", Now);
        batch.RequestCommit(ImportCommitMode.ValidRowsOnly, Now, UserId);

        batch.CompleteProcessing(3, 2, "[]", Now);

        batch.Status.Should().Be(ImportBatchStatus.CompletedWithErrors);
    }

    [Fact]
    public void FailProcessing_moves_to_Failed_and_keeps_report()
    {
        var batch = CreateBatch();
        batch.BeginValidation(Now);
        batch.CompleteValidation(5, 5, 0, "[]", Now);
        batch.RequestCommit(ImportCommitMode.AllOrNothing, Now, UserId);

        batch.FailProcessing("El estado cambió desde la vista previa.", "[{\"RowNumber\":1}]", Now);

        batch.Status.Should().Be(ImportBatchStatus.Failed);
        batch.ReportJson.Should().Be("[{\"RowNumber\":1}]");
    }

    [Theory]
    [InlineData(ImportBatchStatus.Queued)]
    [InlineData(ImportBatchStatus.Validating)]
    [InlineData(ImportBatchStatus.Validated)]
    public void Cancel_from_non_terminal_pre_commit_statuses_succeeds(ImportBatchStatus status)
    {
        var batch = CreateBatch();
        if (status is ImportBatchStatus.Validating or ImportBatchStatus.Validated)
        {
            batch.BeginValidation(Now);
        }

        if (status == ImportBatchStatus.Validated)
        {
            batch.CompleteValidation(1, 1, 0, "[]", Now);
        }

        batch.Cancel(Now, UserId);

        batch.Status.Should().Be(ImportBatchStatus.Cancelled);
    }

    [Fact]
    public void Cancel_after_commit_requested_throws()
    {
        var batch = CreateBatch();
        batch.BeginValidation(Now);
        batch.CompleteValidation(1, 1, 0, "[]", Now);
        batch.RequestCommit(ImportCommitMode.AllOrNothing, Now, UserId);

        var act = () => batch.Cancel(Now, UserId);

        act.Should().Throw<DomainException>();
    }
}
