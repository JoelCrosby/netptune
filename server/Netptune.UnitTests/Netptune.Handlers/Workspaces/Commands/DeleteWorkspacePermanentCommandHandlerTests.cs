using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Models.Search;
using Netptune.Core.Models.Workspaces;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Workspaces.Commands;

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Workspaces.Commands;

public class DeleteWorkspacePermanentCommandHandlerTests
{
    private readonly DeleteWorkspacePermanentCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IWorkspaceCache WorkspaceCache = Substitute.For<IWorkspaceCache>();
    private readonly IWorkspaceUserCache WorkspaceUserCache = Substitute.For<IWorkspaceUserCache>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly IStorageService Storage = Substitute.For<IStorageService>();

    public DeleteWorkspacePermanentCommandHandlerTests()
    {
        Handler = new(
            UnitOfWork,
            WorkspaceCache,
            WorkspaceUserCache,
            PermissionCache,
            EventPublisher,
            Storage,
            NullLogger<DeleteWorkspacePermanentCommandHandler>.Instance);

        UnitOfWork.InvokeTransaction<DeletedWorkspaceReferences>();
    }

    [Fact]
    public async Task DeletePermanent_ShouldReturnSuccess_WhenValidId()
    {
        var workspace = GivenWorkspace(new DeletedWorkspaceReferences());

        var result = await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();

        await UnitOfWork.Workspaces
            .Received(1)
            .DeleteWorkspacePermanent(workspace.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotDeleteAnything_WhenInvalidId()
    {
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>(), cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        await UnitOfWork.Workspaces
            .DidNotReceive()
            .DeleteWorkspacePermanent(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotOpenATransaction_WhenInvalidId()
    {
        UnitOfWork.Workspaces.GetBySlug(Arg.Any<string>(), cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        await UnitOfWork.DidNotReceive().Transaction(Arg.Any<Func<Task<DeletedWorkspaceReferences>>>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task DeletePermanent_ShouldClearEveryMembersCachedAccess_SoAReusedSlugStartsClean()
    {
        var workspace = GivenWorkspace(new DeletedWorkspaceReferences { MemberUserIds = ["owner", "member"] });

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        WorkspaceCache.Received(1).Remove(workspace.Slug);

        foreach (var userId in new[] { "owner", "member" })
        {
            var key = new WorkspaceUserKey { UserId = userId, WorkspaceKey = workspace.Slug };

            WorkspaceUserCache.Received(1).Remove(key);
            PermissionCache.Received(1).Remove(key);
        }
    }

    [Fact]
    public async Task DeletePermanent_ShouldRemoveEverySearchDocument_SoAReusedSlugFindsNothing()
    {
        var workspace = GivenWorkspace(new DeletedWorkspaceReferences
        {
            TaskIds = [1],
            ProjectIds = [2],
            BoardIds = [3],
            SprintIds = [4],
            PageIds = [5],
        });

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        await ShouldRemoveSearchDocuments(SearchIndexEntityTypes.Task, 1, workspace.Slug);
        await ShouldRemoveSearchDocuments(SearchIndexEntityTypes.Project, 2, workspace.Slug);
        await ShouldRemoveSearchDocuments(SearchIndexEntityTypes.Board, 3, workspace.Slug);
        await ShouldRemoveSearchDocuments(SearchIndexEntityTypes.Sprint, 4, workspace.Slug);
        await ShouldRemoveSearchDocuments(SearchIndexEntityTypes.Page, 5, workspace.Slug);
    }

    [Fact]
    public async Task DeletePermanent_ShouldDeleteTheStoredFiles_WhenTheWorkspaceHadAny()
    {
        GivenWorkspace(new DeletedWorkspaceReferences { StorageKeys = ["file", "export"] });

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        await Storage.Received(1).DeleteFilesAsync(
            Arg.Is<IReadOnlyCollection<string>>(keys => keys.SequenceEqual(new[] { "file", "export" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePermanent_ShouldNotCallStorage_WhenTheWorkspaceHadNoFiles()
    {
        GivenWorkspace(new DeletedWorkspaceReferences());

        await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        await Storage.DidNotReceive().DeleteFilesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePermanent_ShouldStillSucceed_WhenStorageDeletionFails()
    {
        GivenWorkspace(new DeletedWorkspaceReferences { StorageKeys = ["file"] });
        Storage.DeleteFilesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("storage unavailable"));

        var result = await Handler.Handle(new DeleteWorkspacePermanentCommand("workspace"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    private Workspace GivenWorkspace(DeletedWorkspaceReferences references)
    {
        var workspace = AutoFixtures.Workspace;

        UnitOfWork.Workspaces.GetBySlug("workspace", cancellationToken: TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.Workspaces.DeleteWorkspacePermanent(workspace.Id, Arg.Any<CancellationToken>()).Returns(references);

        return workspace;
    }

    private async Task ShouldRemoveSearchDocuments(string entityType, int entityId, string workspaceSlug)
    {
        await EventPublisher.Received(1).Dispatch(Arg.Is<SearchIndexEvent>(searchEvent =>
            searchEvent.Operation == SearchIndexOperation.Delete &&
            searchEvent.EntityType == entityType &&
            searchEvent.EntityIds.SequenceEqual(new[] { entityId }) &&
            searchEvent.WorkspaceSlug == workspaceSlug));
    }
}
