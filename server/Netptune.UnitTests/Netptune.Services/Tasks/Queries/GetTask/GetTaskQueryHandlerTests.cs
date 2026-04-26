using FluentAssertions;

using Netptune.Core.UnitOfWork;
using Netptune.Services.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Queries;

public class GetTaskQueryHandlerTests
{
    private readonly GetTaskQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetTaskQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var task = AutoFixtures.TaskViewModel;
        UnitOfWork.Tasks.GetTaskViewModel(1).Returns(task);

        var result = await Handler.Handle(new GetTaskQuery(1), CancellationToken.None);

        result.Should().BeEquivalentTo(task);
    }
}
