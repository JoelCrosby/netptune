using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution;
using Netptune.Ai.Execution.Handlers;
using Netptune.Ai.Tools;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Core.ViewModels.Usage;
using Netptune.Handlers.RelationTypes.Commands;
using Netptune.Handlers.RelationTypes.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class RelationTypeChangeToolTests
{
    private const int BlocksId = 7;
    private const int RelatesId = 8;

    private readonly IMediator Mediator = Substitute.For<IMediator>();
    private readonly AiChangeSetBuilder ChangeSet = new();

    public RelationTypeChangeToolTests()
    {
        var relationTypes = new List<RelationTypeViewModel>
        {
            new()
            {
                Id = BlocksId,
                Name = "Blocks",
                InverseName = "Is Blocked By",
                Key = "blocks",
                Description = "Must finish first",
                Color = "red",
                Category = RelationCategory.Dependency,
            },
            new() { Id = RelatesId, Name = "Relates To", InverseName = "Relates To", Key = "relates-to", Category = RelationCategory.Related },
        };

        Mediator
            .Send(Arg.Any<GetRelationTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(relationTypes);

        Mediator
            .Send(Arg.Any<UpdateRelationTypeCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<RelationTypeViewModel>.Success(relationTypes[0]));
    }

    [Fact]
    public async Task Update_ShouldProposeOnlyTheChangedFields()
    {
        var result = await Execute($$"""{"action":"update","relationTypeId":{{BlocksId}},"name":"Blocks","inverseName":"Waits On"}""");

        result.IsError.Should().BeFalse();

        var change = ChangeSet.Changes.Should().ContainSingle().Subject;

        change.ToolName.Should().Be(UpdateRelationTypeAction.Change);
        change.EntityId.Should().Be(BlocksId);
        change.Fields.Select(field => field.Name).Should().Equal("inverseName");
        change.Payload.RootElement.TryGetProperty("name", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldFail_WhenAnotherTypeHasTheName()
    {
        var result = await Execute($$"""{"action":"update","relationTypeId":{{BlocksId}},"name":"relates to"}""");

        result.IsError.Should().BeTrue();
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldFail_WhenTheTypeIsInUse()
    {
        GivenUsage(BlocksId, "Blocks", canDelete: false, "Relation type is used by 3 relations and cannot be deleted.");

        var result = await Execute($$"""{"action":"delete","relationTypeId":{{BlocksId}}}""");

        result.IsError.Should().BeTrue();
        result.Content.Should().Contain("3 relations");
        ChangeSet.Changes.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_ShouldPropose_WhenTheTypeIsUnused()
    {
        GivenUsage(RelatesId, "Relates To", canDelete: true, null);

        var result = await Execute($$"""{"action":"delete","relationTypeId":{{RelatesId}}}""");

        result.IsError.Should().BeFalse();
        ChangeSet.Changes.Should().ContainSingle().Which.ToolName.Should().Be(DeleteRelationTypeAction.Change);
    }

    [Fact]
    public async Task ApplyUpdate_ShouldKeepTheFieldsTheChangeLeavesAlone()
    {
        var handler = new UpdateRelationTypeChangeHandler(Mediator);
        var context = CreateContext(UpdateRelationTypeAction.Change, $$"""{"relationTypeId":{{BlocksId}},"inverseName":"Waits On"}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);

        var request = Mediator.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<UpdateRelationTypeCommand>()
            .Should().ContainSingle().Subject.Request;

        request.Name.Should().Be("Blocks");
        request.InverseName.Should().Be("Waits On");
        request.Description.Should().Be("Must finish first");
        request.Color.Should().Be("red");
    }

    [Fact]
    public async Task ApplyDelete_ShouldReportWhyTheCommandRefused()
    {
        Mediator
            .Send(Arg.Any<DeleteRelationTypeCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse.Failed("Relation type is used by 1 relation and cannot be deleted."));

        var handler = new DeleteRelationTypeChangeHandler(Mediator);
        var context = CreateContext(DeleteRelationTypeAction.Change, $$"""{"relationTypeId":{{BlocksId}}}""");
        var result = await handler.Apply(context, TestContext.Current.CancellationToken);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        result.Error.Should().Contain("1 relation");
        await Mediator.Received(1).Send(Arg.Is<DeleteRelationTypeCommand>(command => command.Id == BlocksId), Arg.Any<CancellationToken>());
    }

    private async Task<AiToolExecution> Execute(string json)
    {
        var tool = new RelationTypeChangeTool(Mediator, ChangeSet);
        var arguments = JsonDocument.Parse(json).RootElement;

        return await tool.Execute(arguments, TestContext.Current.CancellationToken);
    }

    private void GivenUsage(int id, string name, bool canDelete, string? blockedReason)
    {
        var usage = new EntityUsageViewModel
        {
            Id = id,
            Kind = UsageSubjectKind.RelationType,
            Name = name,
            CanDelete = canDelete,
            BlockedReason = blockedReason,
        };

        Mediator
            .Send(Arg.Is<GetRelationTypeUsageQuery>(query => query.Id == id), Arg.Any<CancellationToken>())
            .Returns(usage);
    }

    private static AiChangeApplyContext CreateContext(string toolName, string payload)
    {
        var change = new AiProposedChange
        {
            Id = 1,
            ChangeSetId = Guid.NewGuid(),
            Sequence = 1,
            ToolName = toolName,
            EntityType = "relationType",
            Summary = "Change a relation type",
            Payload = JsonDocument.Parse(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
            ApplyStatus = AiChangeApplyStatus.Pending,
        };

        return new AiChangeApplyContext
        {
            Change = change,
            ResolvedRefs = new Dictionary<string, int>(StringComparer.Ordinal),
        };
    }
}
