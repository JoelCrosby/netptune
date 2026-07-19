using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Services;
using Netptune.Core.Services.Search;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Search;
using Netptune.Handlers.Search;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Search;

public sealed class SearchQueryHandlerTests
{
    private const string WorkspaceSlug = "workspace";

    private readonly IMeilisearchService Search = Substitute.For<IMeilisearchService>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly SearchQueryHandler Handler;

    public SearchQueryHandlerTests()
    {
        Handler = new SearchQueryHandler(Search, Identity, UnitOfWork);
        Identity.GetWorkspaceKey().Returns(WorkspaceSlug);
    }

    [Fact]
    public async Task Search_ShouldBuildTaskUrlFromCurrentProjectKey()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = SearchResponse(TaskResult(42));

        Search.SearchAsync(Arg.Any<GlobalSearchQuery>(), cancellationToken).Returns(response);
        UnitOfWork.Tasks
            .GetTaskSearchReferences(
                Arg.Is<IEnumerable<int>>(taskIds => taskIds.SequenceEqual(new[] { 42 })),
                WorkspaceSlug,
                cancellationToken)
            .Returns([new TaskSearchReference(42, "NEW-7", "NEW")]);

        var result = await Handler.Handle(new SearchQuery("architecture"), cancellationToken);

        var task = result.Results.Should().ContainSingle().Subject;
        task.Url.Should().Be("/workspace/tasks/NEW-7");
        task.Subtitle.Should().Be("NEW · In Progress");
        task.Metadata["projectKey"].Should().Be("NEW");
    }

    [Fact]
    public async Task Search_ShouldMergeCurrentExactSystemIdWithoutDuplicates()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var response = SearchResponse(TaskResult(42));
        var exactTask = new TaskViewModel
        {
            Id = 42,
            Name = "Current title",
            SystemId = "NEW-7",
            ProjectId = 3,
            ProjectScopeId = 7,
            StatusName = "Done",
            Priority = TaskPriority.High,
        };

        Search.SearchAsync(Arg.Any<GlobalSearchQuery>(), cancellationToken).Returns(response);
        UnitOfWork.Tasks
            .GetTaskSearchReferences(
                Arg.Is<IEnumerable<int>>(taskIds => taskIds.SequenceEqual(new[] { 42 })),
                WorkspaceSlug,
                cancellationToken)
            .Returns([new TaskSearchReference(42, "NEW-7", "NEW")]);
        UnitOfWork.Tasks.GetTaskViewModel("NEW-7", WorkspaceSlug, cancellationToken).Returns(exactTask);

        var result = await Handler.Handle(new SearchQuery("NEW-7"), cancellationToken);

        var task = result.Results.Should().ContainSingle().Subject;
        task.Title.Should().Be("Current title");
        task.Url.Should().Be("/workspace/tasks/NEW-7");
        task.Subtitle.Should().Be("NEW · Done");
    }

    private static SearchResponse SearchResponse(params SearchResultViewModel[] results)
    {
        return new SearchResponse
        {
            Results = [.. results],
            ProcessingTimeMs = 1,
        };
    }

    private static SearchResultViewModel TaskResult(int taskId)
    {
        return new SearchResultViewModel
        {
            Type = "task",
            Id = taskId,
            Title = "Indexed title",
            Subtitle = "In Progress",
            Url = string.Empty,
            Metadata = new Dictionary<string, object?>
            {
                ["status"] = "In Progress",
                ["projectId"] = 3,
                ["projectScopeId"] = 7,
            },
        };
    }
}
