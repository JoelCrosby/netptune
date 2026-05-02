using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Queries;

public class GetTaskDetailQueryHandlerTests
{
    private readonly GetTaskDetailQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetTaskDetailQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetTaskDetail_ShouldReturnCorrectly_WhenInputValid()
    {
        var task = AutoFixtures.TaskViewModel;
        const string systemId = "systemId";
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Tasks.GetTaskViewModel(systemId, workspaceKey, TestContext.Current.CancellationToken).Returns(task);

        var result = await Handler.Handle(new GetTaskDetailQuery(systemId), CancellationToken.None);

        result.Should().BeEquivalentTo(task);
    }
}
