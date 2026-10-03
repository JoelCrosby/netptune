using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Queries;

public class GetTaskQueryHandlerTests
{
    private readonly GetTaskQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetTaskQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var task = AutoFixtures.TaskViewModel;
        UnitOfWork.Tasks.GetTaskViewModel(1, TestContext.Current.CancellationToken).Returns(task);
        Identity.GetWorkspaceId().Returns(task.WorkspaceId ?? 0);

        var result = await Handler.Handle(new GetTaskQuery(1), TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo(task);
    }
}
