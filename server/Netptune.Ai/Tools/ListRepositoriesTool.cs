using System.Text.Json;

using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.SourceControl.Reading;

namespace Netptune.Ai.Tools;

public sealed class ListRepositoriesTool : IAiTool
{
    private readonly ISourceControlReader Reader;
    private readonly IIdentityService Identity;

    public ListRepositoriesTool(ISourceControlReader reader, IIdentityService identity)
    {
        Reader = reader;
        Identity = identity;
    }

    public string Name => "list_repositories";

    public string Description =>
        "List the source code repositories linked to this workspace, with their default branch and the projects "
        + "whose repository URL points at them. Use these tools, not web_fetch, to read a linked repository's code: "
        + "search_repository_code to find where something lives, list_repository_files for the layout, "
        + "read_repository_file for contents, list_repository_commits and get_pull_request for history.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions => AiRepositoryRead.Permissions;

    public bool IsAvailable(IReadOnlySet<string> permissions)
    {
        return Reader.HasProviders && RequiredPermissions.All(permissions.Contains);
    }

    public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var repositories = await Reader.ListRepositories(workspaceId, cancellationToken);

        if (repositories.Count == 0)
        {
            return AiToolExecution.Success("No repositories are linked to this workspace.");
        }

        var rows = repositories.Select(repository => new
        {
            repository = repository.FullName,
            provider = repository.Provider.ToString(),
            repository.DefaultBranch,
            repository.IsPrivate,
            projects = repository.ProjectKeys,
            url = repository.WebUrl,
        });

        return AiToolExecution.Success(JsonSerializer.Serialize(rows, AiRepositoryRead.JsonOptions));
    }
}
