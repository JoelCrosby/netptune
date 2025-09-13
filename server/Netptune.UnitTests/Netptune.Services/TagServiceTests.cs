using AutoFixture;

using FluentAssertions;

using Netptune.Core.Entities;
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

        UnitOfWork.InvokeTransaction<ClientResponse<TagViewModel>>();

        var result = await Service.AddToTask(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddToTask_ShouldReturnFailure_WhenWorkspaceNotFound()
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
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>()).ReturnsNull();

        UnitOfWork.InvokeTransaction<ClientResponse<TagViewModel>>();

        var result = await Service.AddToTask(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AddToTask_ShouldReturnFailure_WhenTaskNotFound()
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
        UnitOfWork.Tasks.GetTaskInternalId(Arg.Any<string>(), Arg.Any<string>()).ReturnsNull();
        UnitOfWork.Workspaces.GetIdBySlug(Arg.Any<string>()).Returns(1);

        UnitOfWork.InvokeTransaction<ClientResponse<TagViewModel>>();

        var result = await Service.AddToTask(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetTagsForTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var tags = new List<TagViewModel> { Fixture.Create<TagViewModel>() };

        Identity.GetWorkspaceKey().Returns("key");

        UnitOfWork.Tasks.GetTaskInternalId("task-id", "key").Returns(1);
        UnitOfWork.Tags.GetViewModelsForTask(1, Arg.Any<bool>()).Returns(tags);

        var result = await Service.GetTagsForTask("task-id");

        result.Should().NotBeEmpty();
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetTagsForTask_ShouldReturnNull_WhenTaskNotFound()
    {
        Identity.GetWorkspaceKey().Returns("key");

        UnitOfWork.Tasks.GetTaskInternalId("task-id", "key").ReturnsNull();

        var result = await Service.GetTagsForTask("task-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTagsForWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        var tags = new List<TagViewModel> { Fixture.Create<TagViewModel>() };

        Identity.GetWorkspaceKey().Returns("key");

        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetViewModelsForWorkspace(1).Returns(tags);

        var result = await Service.GetTagsForWorkspace();

        result.Should().NotBeEmpty();
        result.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetTagsForWorkspace_ShouldReturnNull_WhenWorkspaceNotFound()
    {
        Identity.GetWorkspaceKey().Returns("key");

        UnitOfWork.Workspaces.GetIdBySlug("key").ReturnsNull();

        var result = await Service.GetTagsForWorkspace();

        result.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        var tagNames = new List<string> { "tag" };
        var tags = new List<Tag> { AutoFixtures.Tag };
        var request = Fixture.Create<DeleteTagsRequest>() with { Tags = tagNames };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetTagsByValueInWorkspace(1, tagNames, Arg.Any<bool>()).Returns(tags);

        var result = await Service.Delete(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var tagNames = new List<string> { "tag" };
        var tags = new List<Tag> { AutoFixtures.Tag };
        var request = Fixture.Create<DeleteTagsRequest>() with { Tags = tagNames };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetTagsByValueInWorkspace(1, tagNames, Arg.Any<bool>()).Returns(tags);

        await Service.Delete(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var tagNames = new List<string> { "tag" };
        var tags = new List<Tag> { AutoFixtures.Tag };
        var request = Fixture.Create<DeleteTagsRequest>() with { Tags = tagNames };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").ReturnsNull();
        UnitOfWork.Tags.GetTagsByValueInWorkspace(1, tagNames, Arg.Any<bool>()).Returns(tags);

        var result = await Service.Delete(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFromTask_ShouldReturnSuccess_WhenValidId()
    {
        var request = Fixture.Create<DeleteTagFromTaskRequest>() with
        {
            SystemId = "task-id",
            Tag = "tag",
        };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, "key").Returns(1);

        var result = await Service.DeleteFromTask(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFromTask_ShouldCallCompleteAsync_WhenValidId()
    {
        var request = Fixture.Create<DeleteTagFromTaskRequest>() with
        {
            SystemId = "task-id",
            Tag = "tag",
        };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, "key").Returns(1);

        await Service.DeleteFromTask(request);

        await UnitOfWork.Received(1).Tags.DeleteTagFromTask(1, 1, request.Tag);
        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task DeleteFromTask_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Create<DeleteTagFromTaskRequest>() with
        {
            SystemId = "task-id",
            Tag = "tag",
        };

        Identity.GetWorkspaceKey().Returns("key");
        UnitOfWork.Workspaces.GetIdBySlug("key").ReturnsNull();
        UnitOfWork.Tasks.GetTaskInternalId(request.SystemId, "key").Returns(1);

        var result = await Service.DeleteFromTask(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateTagRequest>()
            .Create();

        var tag = AutoFixtures.Tag;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);

        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1).Returns(tag);

        var result = await Service.Update(request);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.NewValue);
    }

    [Fact]
    public async Task Update_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateTagRequest>()
            .Create();

        var tag = AutoFixtures.Tag;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);

        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1).Returns(tag);

        await Service.Update(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture
            .Build<UpdateTagRequest>()
            .Create();

        var tag = AutoFixtures.Tag;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);

        UnitOfWork.Workspaces.GetIdBySlug("key").ReturnsNull();
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1).Returns(tag);

        var result = await Service.Update(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenTagNotFound()
    {
        var request = Fixture
            .Build<UpdateTagRequest>()
            .Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);

        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1).ReturnsNull();

        var result = await Service.Update(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldTrimWhitespace_FromNewNameValue()
    {
        var request = Fixture
                .Build<UpdateTagRequest>()
                .Create() with
            {
                NewValue = "  Spaces before and after ",
            };

        var tag = AutoFixtures.Tag;

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUserId().Returns(AutoFixtures.AppUser.Id);

        UnitOfWork.Workspaces.GetIdBySlug("key").Returns(1);
        UnitOfWork.Tags.GetByValue(request.CurrentValue, 1).Returns(tag);

        var result = await Service.Update(request);

        result.Payload!.Name.Should().Be("Spaces before and after");
    }
}
