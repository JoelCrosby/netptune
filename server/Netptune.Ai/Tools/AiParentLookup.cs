using System.Text.Json;

using Mediator;

using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Queries;

namespace Netptune.Ai.Tools;

internal sealed record AiParent
{
    public required string Name { get; init; }

    public int? Id { get; init; }

    public bool IsPending => Id is null;
}

internal sealed record AiParentResult(AiParent? Parent, string? Error)
{
    public static AiParentResult Found(AiParent parent)
    {
        return new AiParentResult(parent, null);
    }

    public static AiParentResult Failed(string error)
    {
        return new AiParentResult(null, error);
    }
}

internal static class AiParentLookup
{
    public static async Task<AiParentResult> Project(
        IMediator mediator,
        IAiChangeSetBuilder changeSet,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var projectRef = AiPendingReference.Read(arguments, "projectRef");

        if (projectRef is not null)
        {
            var pending = AiPendingReference.Find(changeSet, projectRef, "project");

            if (pending is null)
            {
                return AiParentResult.Failed(AiPendingReference.Missing(projectRef, "project"));
            }

            return AiParentResult.Found(new AiParent { Name = ProposedName(pending) });
        }

        var projectId = AiToolSchema.GetInt(arguments, "projectId");

        if (!projectId.HasValue)
        {
            return AiParentResult.Failed("A projectId is required, or a projectRef for a project proposed in this change set.");
        }

        var projects = await mediator.Send(new GetProjectsQuery(), cancellationToken);
        var project = projects.FirstOrDefault(item => item.Id == projectId.Value);

        if (project is null)
        {
            return AiParentResult.Failed($"Project {projectId} is not in this workspace.");
        }

        return AiParentResult.Found(new AiParent { Name = project.Name, Id = project.Id });
    }

    public static AiParentResult Board(
        IAiChangeSetBuilder changeSet,
        JsonElement arguments,
        int? existingBoardId,
        string? existingBoardName)
    {
        var boardRef = AiPendingReference.Read(arguments, "boardRef");

        if (boardRef is not null)
        {
            var pending = AiPendingReference.Find(changeSet, boardRef, "board");

            if (pending is null)
            {
                return AiParentResult.Failed(AiPendingReference.Missing(boardRef, "board"));
            }

            return AiParentResult.Found(new AiParent { Name = ProposedName(pending) });
        }

        if (!existingBoardId.HasValue)
        {
            return AiParentResult.Failed("A boardId is required, or a boardRef for a board proposed in this change set.");
        }

        return AiParentResult.Found(new AiParent
        {
            Name = existingBoardName ?? string.Empty,
            Id = existingBoardId,
        });
    }

    private static string ProposedName(AiChangeDraft draft)
    {
        var name = draft.Fields.FirstOrDefault(field => string.Equals(field.Name, "name", StringComparison.Ordinal));

        return name?.After ?? draft.Summary;
    }
}
