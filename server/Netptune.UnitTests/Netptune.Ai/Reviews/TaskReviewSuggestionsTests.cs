using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution.Reviews;
using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Tags.Commands;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai.Reviews;

public class TaskReviewSuggestionsTests
{
    private const int WorkspaceId = 7;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly TaskReviewSuggestions Suggestions;
    private readonly TaskViewModel Task = new() { Id = 42, SystemId = "netp-42", ProjectId = 1, Tags = [] };

    public TaskReviewSuggestionsTests()
    {
        Suggestions = new TaskReviewSuggestions(UnitOfWork, Mediator);

        UnitOfWork.RelationTypes
            .GetViewModelsForWorkspace(WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(new List<RelationTypeViewModel>());
        UnitOfWork.Tags
            .GetTagsInWorkspace(WorkspaceId, Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<Tag>());
        Mediator
            .Send(Arg.Any<DeleteTasksCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Success);
    }

    [Fact]
    public async Task Split_ShouldTakeBackTheTasksItMade_WhenALaterOneFails()
    {
        Mediator
            .Send(Arg.Any<CreateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(
                ClientResponse<TaskViewModel>.Success(new TaskViewModel { Id = 100 }),
                ClientResponse<TaskViewModel>.Success(new TaskViewModel { Id = 101 }),
                ClientResponse<TaskViewModel>.Failed("The project was archived."));

        var suggestion = new AiReviewSplitSuggestion
        {
            Tasks = [new() { Name = "One" }, new() { Name = "Two" }, new() { Name = "Three" }],
        };

        var result = await Suggestions.Apply(CreateRequest(suggestion), Task, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await Mediator.Received(1).Send(
            Arg.Is<DeleteTasksCommand>(command => command.Ids.SequenceEqual(new[] { 100, 101 })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Metadata_ShouldNotCreateATag_ThatThePermissionCheckCountedAsExisting()
    {
        var suggestion = new AiReviewMetadataSuggestion { Tags = [new() { Name = "Backend", IsNew = false }] };
        var request = CreateRequest(suggestion);

        TaskReviewSuggestions.PermissionsFor(request).Should().NotContain(NetptunePermissions.Tags.Create);

        var result = await Suggestions.Apply(request, Task, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Backend");
        await Mediator.DidNotReceive().Send(Arg.Any<CreateTagCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Metadata_ShouldCreateANewTag_WhenThePermissionCheckCoveredIt()
    {
        Mediator
            .Send(Arg.Any<CreateTagCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TagViewModel>.Success(new TagViewModel { Id = 5, Name = "Backend" }));
        Mediator
            .Send(Arg.Any<UpdateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Success(Task));

        var suggestion = new AiReviewMetadataSuggestion { Tags = [new() { Name = "Backend", IsNew = true }] };
        var request = CreateRequest(suggestion);

        TaskReviewSuggestions.PermissionsFor(request).Should().Contain(NetptunePermissions.Tags.Create);

        var result = await Suggestions.Apply(request, Task, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await Mediator.Received(1).Send(Arg.Any<CreateTagCommand>(), Arg.Any<CancellationToken>());
    }

    private static AiReviewApplyRequest CreateRequest(object suggestion)
    {
        return new AiReviewApplyRequest
        {
            WorkspaceId = WorkspaceId,
            TargetId = 42,
            Suggestion = JsonSerializer.SerializeToElement(suggestion, JsonOptions.Default),
            Decision = new DecideAiReviewFindingRequest { Decision = AiReviewDecision.Accept },
        };
    }
}
