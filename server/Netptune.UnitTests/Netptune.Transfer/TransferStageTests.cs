using FluentAssertions;

using Netptune.Transfer.Enums;

using Xunit;

namespace Netptune.UnitTests.Netptune.Transfer;

public class TransferStageTests
{
    [Theory]
    [InlineData(ImportStage.Uploaded)]
    [InlineData(ImportStage.Inspected)]
    [InlineData(ImportStage.Mapped)]
    [InlineData(ImportStage.Previewed)]
    [InlineData(ImportStage.Failed)]
    public void CanInspect_AcceptsEveryStageThatStillOwnsItsSourceFile(ImportStage stage)
    {
        ImportStages.CanInspect(stage).Should().BeTrue();
    }

    [Theory]
    [InlineData(ImportStage.Committing)]
    [InlineData(ImportStage.Committed)]
    [InlineData(ImportStage.Undone)]
    [InlineData(ImportStage.Abandoned)]
    public void CanInspectAndCanMap_RejectAStageThatHasAlreadyBeenHandedToTheJobServer(ImportStage stage)
    {
        ImportStages.CanInspect(stage).Should().BeFalse();
        ImportStages.CanMap(stage).Should().BeFalse();
    }

    [Fact]
    public void CanMap_RequiresAProfileButNotAMapping()
    {
        ImportStages.CanMap(ImportStage.Uploaded).Should().BeFalse();
        ImportStages.CanMap(ImportStage.Inspected).Should().BeTrue();
    }

    [Theory]
    [InlineData(ImportStage.Mapped)]
    [InlineData(ImportStage.Previewed)]
    [InlineData(ImportStage.Failed)]
    public void CanPreviewAndCanCommit_AcceptEveryMappedStage(ImportStage stage)
    {
        ImportStages.CanPreview(stage).Should().BeTrue();
        ImportStages.CanCommit(stage).Should().BeTrue();
    }

    [Theory]
    [InlineData(ImportStage.Uploaded)]
    [InlineData(ImportStage.Inspected)]
    [InlineData(ImportStage.Committing)]
    [InlineData(ImportStage.Committed)]
    public void CanPreviewAndCanCommit_RejectAStageWithNoMappingToApply(ImportStage stage)
    {
        ImportStages.CanPreview(stage).Should().BeFalse();
        ImportStages.CanCommit(stage).Should().BeFalse();
    }

    [Fact]
    public void CanRunAndCanUndo_EachAcceptExactlyOneStage()
    {
        var runnable = Enum.GetValues<ImportStage>().Where(ImportStages.CanRun);
        var undoable = Enum.GetValues<ImportStage>().Where(ImportStages.CanUndo);

        runnable.Should().Equal(ImportStage.Committing);
        undoable.Should().Equal(ImportStage.Committed);
    }

    [Fact]
    public void ExportJobStatuses_LetAJobRunOnceAndCancelWhileItIsInFlight()
    {
        var runnable = Enum.GetValues<ExportJobStatus>().Where(ExportJobStatuses.CanRun);
        var cancellable = Enum.GetValues<ExportJobStatus>().Where(ExportJobStatuses.CanCancel);
        var downloadable = Enum.GetValues<ExportJobStatus>().Where(ExportJobStatuses.HasArtefact);

        runnable.Should().Equal(ExportJobStatus.Pending);
        cancellable.Should().Equal(ExportJobStatus.Pending, ExportJobStatus.Running);
        downloadable.Should().Equal(ExportJobStatus.Succeeded);
    }
}
