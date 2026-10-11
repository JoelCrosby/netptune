using FluentAssertions;

using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Handlers.Transfer.Commands;
using Netptune.Transfer.Archive;
using Netptune.Transfer.Services;
using Netptune.Transfer.ViewModels;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Transfer.Commands;

public class ImportArchiveCommandHandlerTests
{
    private const string WorkspaceSlug = "cloned";

    private readonly IArchiveImporter Importer = Substitute.For<IArchiveImporter>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly ImportArchiveCommandHandler Handler;

    public ImportArchiveCommandHandlerTests()
    {
        Identity.GetCurrentUserId().Returns("user-1");
        Identity.GetWorkspaceId().Returns(1);

        Handler = new ImportArchiveCommandHandler(Importer, Identity, EventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldIndexEveryImportedEntity_InTheImportedWorkspace()
    {
        GivenImported(new ArchiveImportResult
        {
            WorkspaceSlug = WorkspaceSlug,
            TaskIds = [1, 2],
            ProjectIds = [3],
            BoardIds = [4],
            SprintIds = [5],
            PageIds = [6],
        });

        var result = await Send();

        result.IsSuccess.Should().BeTrue();
        await ReceivedIndex(SearchIndexEntityTypes.Task, 1, 2);
        await ReceivedIndex(SearchIndexEntityTypes.Project, 3);
        await ReceivedIndex(SearchIndexEntityTypes.Board, 4);
        await ReceivedIndex(SearchIndexEntityTypes.Sprint, 5);
        await ReceivedIndex(SearchIndexEntityTypes.Page, 6);
    }

    [Fact]
    public async Task Handle_ShouldSkipTypes_ThatTheArchiveDidNotCreate()
    {
        GivenImported(new ArchiveImportResult { WorkspaceSlug = WorkspaceSlug, TaskIds = [1] });

        await Send();

        await EventPublisher.Received(1).Dispatch(Arg.Any<SearchIndexEvent>());
    }

    [Fact]
    public async Task Handle_ShouldIndexInBatches_WhenAnArchiveCreatesManyOfAType()
    {
        var taskIds = Enumerable.Range(1, 2500).ToList();

        GivenImported(new ArchiveImportResult { WorkspaceSlug = WorkspaceSlug, TaskIds = taskIds });

        await Send();

        await ReceivedIndex(SearchIndexEntityTypes.Task, [.. taskIds[..1000]]);
        await ReceivedIndex(SearchIndexEntityTypes.Task, [.. taskIds[1000..2000]]);
        await ReceivedIndex(SearchIndexEntityTypes.Task, [.. taskIds[2000..]]);
    }

    [Fact]
    public async Task Handle_ShouldIndexNothing_WhenTheArchiveIsRefused()
    {
        Importer
            .Import(Arg.Any<ArchiveImportRequest>(), Arg.Any<CancellationToken>())
            .Returns<ArchiveImportResult>(_ => throw new ArchiveSchemaException("Refused."));

        var result = await Send();

        result.IsSuccess.Should().BeFalse();
        await EventPublisher.DidNotReceive().Dispatch(Arg.Any<SearchIndexEvent>());
    }

    private void GivenImported(ArchiveImportResult result)
    {
        Importer.Import(Arg.Any<ArchiveImportRequest>(), Arg.Any<CancellationToken>()).Returns(result);
    }

    private Task ReceivedIndex(string entityType, params int[] ids)
    {
        return EventPublisher.Received(1).Dispatch(Arg.Is<SearchIndexEvent>(message =>
            message.Operation == SearchIndexOperation.Index
            && message.EntityType == entityType
            && message.EntityIds.SequenceEqual(ids)
            && message.WorkspaceSlug == WorkspaceSlug));
    }

    private async Task<ClientResponse<ArchiveImportResultViewModel>> Send()
    {
        var request = new ImportArchiveRequest
        {
            Archive = new MemoryStream(),
            Mode = ArchiveImportMode.Clone,
            TargetSlug = WorkspaceSlug,
        };

        return await Handler.Handle(new ImportArchiveCommand(request), TestContext.Current.CancellationToken);
    }
}
