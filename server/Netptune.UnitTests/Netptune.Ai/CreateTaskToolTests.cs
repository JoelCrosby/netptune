using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Projects.Queries;
using Netptune.Handlers.Tags.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class CreateTaskToolTests
{
    private const int ProjectId = 3;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    public CreateTaskToolTests()
    {
        GivenProject();
        GivenTags("bug");
    }

    [Fact]
    public async Task Execute_ShouldProposeTags_WhenTheyExistInTheWorkspace()
    {
        var result = await Execute($$"""{"name":"Fix the login page","projectId":{{ProjectId}},"tags":["bug"]}""");

        result.IsError.Should().BeFalse();

        var field = ChangeSet.Changes.Single().Fields.Single(item => item.Name == "tags");

        field.After.Should().Be("bug");
    }

    [Fact]
    public async Task Execute_ShouldProposeTags_WhenTheyArePendingInTheSameChangeSet()
    {
        var createTag = new CreateTagTool(Mediator, ChangeSet);
        var tagArguments = JsonDocument.Parse("""{"name":"regression"}""").RootElement;

        await createTag.Execute(tagArguments, TestContext.Current.CancellationToken);

        var result = await Execute(
            $$"""{"name":"Fix the login page","projectId":{{ProjectId}},"tags":["regression"]}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Last().Fields.Single(item => item.Name == "tags").After.Should().Be("regression");
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenATagIsNotInTheWorkspace()
    {
        var result = await Execute($$"""{"name":"Fix the login page","projectId":{{ProjectId}},"tags":["chore"]}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("propose_create_tag");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldProposeATask_InAProjectPendingInTheSameChangeSet()
    {
        var projectRef = await GivenProposedProject("Apollo");

        var result = await Execute($$"""{"name":"Draft the brief","projectRef":"{{projectRef}}"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Last();

        change.Fields.Single(item => item.Name == "project").After.Should().Be("Apollo");
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenTheProjectRefIsNotInTheChangeSet()
    {
        var result = await Execute("""{"name":"Draft the brief","projectRef":"ref:nope"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("ref:nope");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenNeitherAProjectIdNorARefIsGiven()
    {
        var result = await Execute("""{"name":"Draft the brief"}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("projectRef");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldFail_WhenAPendingProjectIsPairedWithAnExistingSprint()
    {
        var projectRef = await GivenProposedProject("Apollo");

        var result = await Execute(
            $$"""{"name":"Draft the brief","projectRef":"{{projectRef}}","sprintId":9}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("sprintRef");
    }

    [Fact]
    public void GetRequiredPermissions_ShouldDemandTagAssignment_OnlyWhenTagsAreRequested()
    {
        var tool = new CreateTaskTool(Mediator, ChangeSet);
        var withTags = JsonDocument.Parse("""{"name":"Fix","projectId":3,"tags":["bug"]}""").RootElement;
        var withoutTags = JsonDocument.Parse("""{"name":"Fix","projectId":3}""").RootElement;

        tool.GetRequiredPermissions(withTags).Should().Contain(NetptunePermissions.Tags.Assign);
        tool.GetRequiredPermissions(withoutTags).Should().NotContain(NetptunePermissions.Tags.Assign);
    }

    private async Task<AiToolExecution> Execute(string arguments)
    {
        var tool = new CreateTaskTool(Mediator, ChangeSet);
        var element = JsonDocument.Parse(arguments).RootElement;

        return await tool.Execute(element, TestContext.Current.CancellationToken);
    }

    private async Task<string> GivenProposedProject(string name)
    {
        var createProject = new CreateProjectTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse($$"""{"name":"{{name}}"}""").RootElement;

        await createProject.Execute(arguments, TestContext.Current.CancellationToken);

        return ChangeSet.Changes.Last().RefKey!;
    }

    private void GivenProject()
    {
        var project = new ProjectViewModel { Id = ProjectId, Name = "Netptune" };

        Mediator
            .Send(Arg.Any<GetProjectsQuery>(), Arg.Any<CancellationToken>())
            .Returns([project]);
    }

    private void GivenTags(params string[] names)
    {
        var tags = names.Select(name => new TagViewModel { Name = name }).ToList();

        Mediator
            .Send(Arg.Any<GetTagsForWorkspaceQuery>(), Arg.Any<CancellationToken>())
            .Returns(tags);
    }
}
