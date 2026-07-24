using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Boards;
using Netptune.Handlers.BoardGroups.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.BoardGroups.Queries;

public sealed class GetBoardGroupOptionsQueryHandlerTests
{
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly GetBoardGroupOptionsQueryHandler Handler;

    public GetBoardGroupOptionsQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task Handle_ShouldReturnWorkspaceBoardGroupOptions()
    {
        var options = new List<BoardGroupOptionViewModel>
        {
            new()
            {
                Id = 12,
                Name = "In progress",
                BoardName = "Delivery",
                ProjectName = "Website",
            },
        };

        Identity.GetWorkspaceId().Returns(7);
        UnitOfWork.BoardGroups.GetOptionsInWorkspace(7, TestContext.Current.CancellationToken).Returns(options);

        var result = await Handler.Handle(
            new GetBoardGroupOptionsQuery(),
            TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(options);
    }
}
