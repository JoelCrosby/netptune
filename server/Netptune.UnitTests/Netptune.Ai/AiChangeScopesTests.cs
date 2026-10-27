using System.Text.Json;

using FluentAssertions;

using Netptune.Ai.Execution;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiChangeScopesTests
{
    [Fact]
    public void For_ShouldAddTheCommentScope_ForACommentProposedOnATask()
    {
        var comment = CreateChange(1, "propose_add_comment");
        var rename = CreateChange(2, "propose_update_task");
        var results = new List<AiAppliedChangeResult>
        {
            new() { ChangeId = 1, Status = AiChangeApplyStatus.Applied },
            new() { ChangeId = 2, Status = AiChangeApplyStatus.Applied },
        };

        var scopes = AiChangeScopes.For([comment, rename], results);

        scopes.Should().BeEquivalentTo(["task", "comment"]);
    }

    [Fact]
    public void For_ShouldLeaveOutChangesThatDidNotApply()
    {
        var comment = CreateChange(1, "propose_add_comment");
        var results = new List<AiAppliedChangeResult> { new() { ChangeId = 1, Status = AiChangeApplyStatus.Failed } };

        AiChangeScopes.For([comment], results).Should().BeEmpty();
    }

    private static AiProposedChange CreateChange(long id, string toolName)
    {
        return new AiProposedChange
        {
            Id = id,
            ChangeSetId = Guid.NewGuid(),
            Sequence = (int)id,
            ToolName = toolName,
            EntityType = "task",
            Summary = "Change",
            Payload = JsonDocument.Parse("{}"),
            ValidationStatus = AiChangeValidationStatus.Valid,
            ApplyStatus = AiChangeApplyStatus.Applied,
        };
    }
}
