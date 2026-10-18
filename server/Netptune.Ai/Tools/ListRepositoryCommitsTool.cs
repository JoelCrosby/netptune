using System.Text.Json;

using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.SourceControl.Reading;

namespace Netptune.Ai.Tools;

public sealed class ListRepositoryCommitsTool : IAiTool
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 50;

    private readonly ISourceControlReader Reader;
    private readonly IIdentityService Identity;

    public ListRepositoryCommitsTool(ISourceControlReader reader, IIdentityService identity)
    {
        Reader = reader;
        Identity = identity;
    }

    public string Name => "list_repository_commits";

    public string Description =>
        "List the most recent commits on a branch of a linked repository: SHA, first line of the message, author and date.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions => AiRepositoryRead.Permissions;

    public bool IsAvailable(IReadOnlySet<string> permissions)
    {
        return Reader.HasProviders && RequiredPermissions.All(permissions.Contains);
    }

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        $$"""
        {
          {{AiRepositoryRead.RepositoryProperty}},
          "branch": { "type": "string", "description": "Defaults to the default branch." },
          "limit": { "type": "integer", "description": "How many commits, up to 50. Defaults to 20." }
        }
        """,
        AiRepositoryRead.RepositoryArgument);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var repository = AiRepositoryRead.Repository(arguments);

        if (repository is null)
        {
            return AiRepositoryRead.MissingRepository();
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var branch = AiToolSchema.GetString(arguments, "branch");
        var limit = Math.Clamp(AiToolSchema.GetInt(arguments, "limit") ?? DefaultLimit, 1, MaxLimit);
        var result = await Reader.ListCommits(workspaceId, repository, branch, limit, cancellationToken);

        if (!result.IsSuccess)
        {
            return AiRepositoryRead.Failed(result);
        }

        var rows = result.Payload!.Select(commit => new
        {
            sha = commit.Sha,
            message = commit.Message.Split('\n', 2)[0].Trim(),
            author = commit.Author.Username,
            authoredAt = commit.AuthoredAt,
            url = commit.WebUrl,
        });

        var fitted = AiResultBudget.Fit(rows, AiRepositoryRead.JsonOptions);

        return AiToolExecution.Success(fitted.Rows.ToJsonString(), fitted.Omitted > 0);
    }
}
