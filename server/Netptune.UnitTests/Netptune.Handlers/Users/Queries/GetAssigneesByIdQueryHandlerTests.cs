using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;
using Netptune.Handlers.Users.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Users.Queries;

public class GetAssigneesByIdQueryHandlerTests
{
    private readonly GetAssigneesByIdQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetAssigneesByIdQueryHandlerTests()
    {
        Handler = new GetAssigneesByIdQueryHandler(UnitOfWork, Identity);
    }

    [Fact]
    public async Task Handle_ShouldLookTheIdsUpInTheCurrentWorkspace()
    {
        var member = new AssigneeViewModel { Id = "user-150", DisplayName = "Member 150" };

        Identity.GetWorkspaceId().Returns(7);
        UnitOfWork.Users
            .GetWorkspaceAssignees(
                7,
                Arg.Is<IReadOnlyCollection<string>>(ids => ids.Contains("user-150")),
                Arg.Any<CancellationToken>())
            .Returns([member]);

        var query = new GetAssigneesByIdQuery(["user-150"]);
        var result = await Handler.Handle(query, TestContext.Current.CancellationToken);

        result.Should().ContainSingle().Which.Should().Be(member);
    }

    [Fact]
    public async Task Handle_ShouldNotQuery_WhenNoIdsAreGiven()
    {
        var result = await Handler.Handle(new GetAssigneesByIdQuery([]), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
        await UnitOfWork.Users.DidNotReceiveWithAnyArgs().GetWorkspaceAssignees(default, default!, default);
    }
}
