using System.Text.Json;

using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.SourceControl.Reading;

namespace Netptune.Ai.Tools;

public sealed class ListRepositoryFilesTool : IAiTool
{
    private readonly ISourceControlReader Reader;
    private readonly IIdentityService Identity;

    public ListRepositoryFilesTool(ISourceControlReader reader, IIdentityService identity)
    {
        Reader = reader;
        Identity = identity;
    }

    public string Name => "list_repository_files";

    public string Description =>
        "List the files in a linked repository in one call, optionally only those under a path such as src/app. "
        + "Use it to learn the layout before reading files; pass a narrower path when the listing is cut short.";

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
          "path": { "type": "string", "description": "Only list files under this directory." },
          "ref": { "type": "string", "description": "Branch or commit SHA. Defaults to the default branch." }
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
        var reference = AiToolSchema.GetString(arguments, "ref");
        var result = await Reader.ListFiles(workspaceId, repository, reference, cancellationToken);

        if (!result.IsSuccess)
        {
            return AiRepositoryRead.Failed(result);
        }

        var prefix = NormalizePrefix(AiToolSchema.GetString(arguments, "path"));
        var files = result.Payload!.Tree.Entries
            .Where(entry => !entry.IsDirectory && entry.Path.StartsWith(prefix, StringComparison.Ordinal))
            .Select(entry => new { path = entry.Path, size = entry.Size });

        var fitted = AiResultBudget.Fit(files, AiRepositoryRead.JsonOptions);

        var listing = new
        {
            repository = result.Payload.Repository,
            @ref = result.Payload.Reference,
            commitSha = result.Payload.Tree.CommitSha,
            files = fitted.Rows,
            omitted = fitted.Omitted > 0 ? fitted.Omitted : (int?)null,
            incomplete = result.Payload.Tree.IsTruncated ? true : (bool?)null,
        };

        return AiToolExecution.Success(JsonSerializer.Serialize(listing, AiRepositoryRead.JsonOptions), fitted.Omitted > 0);
    }

    private static string NormalizePrefix(string? path)
    {
        var trimmed = path?.Trim().Trim('/');

        return string.IsNullOrEmpty(trimmed) ? string.Empty : $"{trimmed}/";
    }
}
