namespace Netptune.SourceControl.Providers.GitHub.Dtos;

internal sealed record GitHubInstallationRefDto
{
    public long Id { get; init; }
}

internal sealed record GitHubEnvelopeDto
{
    public string? Action { get; init; }

    public GitHubInstallationRefDto? Installation { get; init; }
}

internal sealed record GitHubPushAuthorDto
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? Username { get; init; }
}

internal sealed record GitHubPushCommitDto
{
    public string Id { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateTimeOffset Timestamp { get; init; }

    public string Url { get; init; } = string.Empty;

    public GitHubPushAuthorDto Author { get; init; } = new();
}

internal sealed record GitHubPushPayload
{
    public string Ref { get; init; } = string.Empty;

    public bool Deleted { get; init; }

    public bool Forced { get; init; }

    public List<GitHubPushCommitDto> Commits { get; init; } = [];

    public GitHubRepositoryDto Repository { get; init; } = new();

    public GitHubAccountDto? Sender { get; init; }
}

internal sealed record GitHubRefPayload
{
    public string Ref { get; init; } = string.Empty;

    public string RefType { get; init; } = string.Empty;

    public GitHubRepositoryDto Repository { get; init; } = new();

    public GitHubAccountDto? Sender { get; init; }
}

internal sealed record GitHubPullRequestPayload
{
    public string Action { get; init; } = string.Empty;

    public GitHubPullRequestDto PullRequest { get; init; } = new();

    public GitHubRepositoryDto Repository { get; init; } = new();

    public GitHubAccountDto? Sender { get; init; }
}

internal sealed record GitHubCheckRunPayload
{
    public string Action { get; init; } = string.Empty;

    public GitHubCheckRunDto CheckRun { get; init; } = new();

    public GitHubRepositoryDto Repository { get; init; } = new();

    public GitHubAccountDto? Sender { get; init; }
}

internal sealed record GitHubInstallationPayload
{
    public string Action { get; init; } = string.Empty;

    public GitHubInstallationDto Installation { get; init; } = new();

    public GitHubAccountDto? Sender { get; init; }
}

internal sealed record GitHubInstallationRepositoriesPayload
{
    public string Action { get; init; } = string.Empty;

    public GitHubInstallationDto Installation { get; init; } = new();

    public List<GitHubRepositoryRefDto> RepositoriesAdded { get; init; } = [];

    public List<GitHubRepositoryRefDto> RepositoriesRemoved { get; init; } = [];

    public GitHubAccountDto? Sender { get; init; }
}
