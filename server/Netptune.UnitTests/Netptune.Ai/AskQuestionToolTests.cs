using System.Text.Json;

using FluentAssertions;

using Netptune.Ai.Execution;
using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AskQuestionToolTests
{
    private const string TwoOptions =
        """{"question":"Which project?","options":[{"label":"Apollo"},{"label":"Internal tools"}]}""";

    private readonly AiQuestionSink Questions = new();
    private readonly AiChangeSetBuilder ChangeSet = new();

    [Fact]
    public async Task Execute_ShouldRecordTheQuestion_WhenItIsWellFormed()
    {
        var result = await Execute(
            """
            {
              "question":"Which project should this go in?",
              "header":"Project",
              "options":[
                {"label":"Apollo","description":"The customer-facing app"},
                {"label":"Internal tools"}
              ]
            }
            """);

        result.IsError.Should().BeFalse();

        var question = Questions.Pending!;

        question.Text.Should().Be("Which project should this go in?");
        question.Header.Should().Be("Project");
        question.MultiSelect.Should().BeFalse();
        question.Options.Should().HaveCount(2);
        question.Options[0].Description.Should().Be("The customer-facing app");
        question.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Execute_ShouldRefuse_WhenTheTurnHasAlreadyAskedSomething()
    {
        await Execute(TwoOptions);

        var result = await Execute(TwoOptions);

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("already asked");
    }

    [Fact]
    public async Task Execute_ShouldRefuse_WhenTheTurnHasAlreadyProposedChanges()
    {
        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = "propose_create_task",
            EntityType = "task",
            Summary = "Create task",
            Payload = JsonDocument.Parse("{}"),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        var result = await Execute(TwoOptions);

        result.IsError.Should().BeTrue();
        Questions.Pending.Should().BeNull("a turn either asks or proposes, never both");
    }

    [Theory]
    [InlineData("""{"question":"Which project?","options":[{"label":"Apollo"}]}""")]
    [InlineData("""{"question":"Which project?","options":[]}""")]
    [InlineData(
        """{"question":"Which?","options":[{"label":"a"},{"label":"b"},{"label":"c"},{"label":"d"},{"label":"e"}]}""")]
    public async Task Execute_ShouldRefuse_WhenTheOptionCountIsOutOfRange(string arguments)
    {
        var result = await Execute(arguments);

        result.IsError.Should().BeTrue();
        Questions.Pending.Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldRefuse_WhenTwoOptionsReadTheSame()
    {
        var result = await Execute(
            """{"question":"Which project?","options":[{"label":"Apollo"},{"label":"apollo"}]}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("differently");
    }

    [Fact]
    public async Task Execute_ShouldRefuse_WhenTheQuestionIsMissing()
    {
        var result = await Execute("""{"options":[{"label":"Apollo"},{"label":"Internal tools"}]}""");

        result.IsError.Should().BeTrue();
        Questions.Pending.Should().BeNull();
    }

    [Fact]
    public async Task Execute_ShouldTrimTheHeader_RatherThanRefusingOverIt()
    {
        var result = await Execute(
            """
            {
              "question":"Which project?",
              "header":"An extremely long header",
              "options":[{"label":"Apollo"},{"label":"Internal tools"}]
            }
            """);

        result.IsError.Should().BeFalse();
        Questions.Pending!.Header.Should().Be("An extremely");
    }

    private async Task<AiToolExecution> Execute(string arguments)
    {
        var tool = new AskQuestionTool(Questions, ChangeSet);
        var element = JsonDocument.Parse(arguments).RootElement;

        return await tool.Execute(element, TestContext.Current.CancellationToken);
    }
}
