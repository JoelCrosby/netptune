using AutoFixture;

using FluentAssertions;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Tags;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class TagServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly TagService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public TagServiceTests()
    {
        Service = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<AddTagRequest>()
            .Create();

        var viewModel = Fixture.Create<TagViewModel>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Tags.Exists(Arg.Any<string>(), Arg.Any<int>()).Returns(false);
        UnitOfWork.Tags.GetViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>()).Returns(1);

        var result = await Service.Create(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture
            .Build<AddTagRequest>()
            .Create();

        var viewModel = Fixture.Create<TagViewModel>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Tags.Exists(Arg.Any<string>(), Arg.Any<int>()).Returns(false);
        UnitOfWork.Tags.GetViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>()).Returns(1);

        await Service.Create(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture
            .Build<AddTagRequest>()
            .Create();

        var viewModel = Fixture.Create<TagViewModel>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Tags.Exists(Arg.Any<string>(), Arg.Any<int>()).Returns(false);
        UnitOfWork.Tags.GetViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>()).ReturnsNull();

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenTagAlreadyExists()
    {
        var request = Fixture
            .Build<AddTagRequest>()
            .Create();

        var viewModel = Fixture.Create<TagViewModel>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Tags.Exists(Arg.Any<string>(), Arg.Any<int>()).Returns(true);
        UnitOfWork.Tags.GetViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>()).Returns(1);

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AddToTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<AddTagToTaskRequest>()
            .Create();

        var tag = AutoFixtures.Tag;
        var viewModel = Fixture.Create<TagViewModel>();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Tags.Exists(Arg.Any<string>(), Arg.Any<int>()).Returns(false);
        UnitOfWork.Tags.GetViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.Tags.GetByValue(Arg.Any<string>(), Arg.Any<int>()).Returns(tag);
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>()).Returns(1);
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>()).Returns(1);

        UnitOfWork.Transaction(Arg.Any<Func<Task<ClientResponse<TagViewModel>>>>())
            .Returns(x => x.Arg<Func<Task<ClientResponse<TagViewModel>>>>()
                .Invoke());

        var result = await Service.AddToTask(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }
}
