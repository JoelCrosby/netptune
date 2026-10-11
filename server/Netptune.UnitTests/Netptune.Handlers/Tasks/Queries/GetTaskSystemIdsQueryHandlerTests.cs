using FluentAssertions;

using Netptune.Core.Models.Search;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Queries;

public class GetTaskSystemIdsQueryHandlerTests
{
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    [Fact]
    public async Task Handle_ShouldLookUpTheTasksInTheCallersWorkspace()
    {
        Identity.GetWorkspaceKey().Returns("acme");
        UnitOfWork.Tasks
            .GetTaskSearchReferences(Arg.Any<IEnumerable<int>>(), "acme", Arg.Any<CancellationToken>())
            .Returns([new TaskSearchReference(7, "NPT-7", "NPT")]);

        var handler = new GetTaskSystemIdsQueryHandler(UnitOfWork, Identity);
        var result = await handler.Handle(new GetTaskSystemIdsQuery([7, 8]), TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(new Dictionary<int, string> { [7] = "NPT-7" });
        await UnitOfWork.Tasks.Received(1).GetTaskSearchReferences(
            Arg.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new List<int> { 7, 8 })),
            "acme",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotQuery_WhenThereAreNoTasks()
    {
        var handler = new GetTaskSystemIdsQueryHandler(UnitOfWork, Identity);
        var result = await handler.Handle(new GetTaskSystemIdsQuery([]), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
        await UnitOfWork.Tasks.DidNotReceive().GetTaskSearchReferences(
            Arg.Any<IEnumerable<int>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
