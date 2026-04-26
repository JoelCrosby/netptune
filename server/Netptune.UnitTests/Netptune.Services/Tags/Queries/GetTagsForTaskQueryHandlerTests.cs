using AutoFixture;

using FluentAssertions;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;
using Netptune.Services.Tags.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tags.Queries;

public class GetTagsForTaskQueryHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly GetTagsForTaskQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetTagsForTaskQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetTagsForTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var tags = new List<TagViewModel> { Fixture.Create<TagViewModel>() };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Tasks.GetTaskInternalId("task-id", "key").Returns(1);
        UnitOfWork.Tags.GetViewModelsForTask(1, Arg.Any<bool>()).Returns(tags);

        var result = await Handler.Handle(new GetTagsForTaskQuery("task-id"), CancellationToken.None);

        result.Should().NotBeEmpty();
        result!.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetTagsForTask_ShouldReturnNull_WhenTaskNotFound()
    {
        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Tasks.GetTaskInternalId("task-id", "key").ReturnsNull();

        var result = await Handler.Handle(new GetTagsForTaskQuery("task-id"), CancellationToken.None);

        result.Should().BeNull();
    }
}
