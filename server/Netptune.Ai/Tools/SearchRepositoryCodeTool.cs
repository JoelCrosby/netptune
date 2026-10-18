using System.Text.Json;

using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.SourceControl.Reading;

namespace Netptune.Ai.Tools;

public sealed class SearchRepositoryCodeTool : IAiTool
{
    private readonly ISourceControlReader Reader;
    private readonly IIdentityService Identity;

    public SearchRepositoryCodeTool(ISourceControlReader reader, IIdentityService identity)
    {
        Reader = reader;
        Identity = identity;
    }

    public string Name => "search_repository_code";

    public string Description =>
        "Search a linked repository's code for text, such as a class name or an error message, and get the matching "
        + "files with short excerpts. It searches the default branch only. "
        + "Follow up with read_repository_file to read a match in context.";

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
          "query": { "type": "string", "description": "What to search for. Quote a phrase to match it exactly." }
        }
        """,
        AiRepositoryRead.RepositoryArgument,
        "query");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var repository = AiRepositoryRead.Repository(arguments);
        var query = AiToolSchema.GetString(arguments, "query")?.Trim();

        if (repository is null)
        {
            return AiRepositoryRead.MissingRepository();
        }

        if (string.IsNullOrEmpty(query))
        {
            return AiToolExecution.Failed("A query is required.");
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var result = await Reader.SearchCode(workspaceId, repository, query, cancellationToken);

        if (!result.IsSuccess)
        {
            return AiRepositoryRead.Failed(result);
        }

        if (result.Payload!.Count == 0)
        {
            return AiToolExecution.Success($"Nothing in {repository} matches {query}.");
        }

        var rows = result.Payload.Select(match => new { path = match.Path, excerpts = match.Fragments, url = match.WebUrl });
        var fitted = AiResultBudget.Fit(rows, AiRepositoryRead.JsonOptions);

        return AiToolExecution.Success(fitted.Rows.ToJsonString(), fitted.Omitted > 0);
    }
}
