namespace Netptune.SourceControl.Providers.GitHub.Dtos;

internal sealed record GitHubAccountDto
{
    public long Id { get; init; }

    public string Login { get; init; } = string.Empty;

    public string? Type { get; init; }

    public string? AvatarUrl { get; init; }
}

internal sealed record GitHubRepositoryDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public GitHubAccountDto Owner { get; init; } = new();

    public string? DefaultBranch { get; init; }

    public string HtmlUrl { get; init; } = string.Empty;

    public bool Private { get; init; }

    public bool Archived { get; init; }
}

internal sealed record GitHubRepositoryRefDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;
}

internal sealed record GitHubInstallationRepositoriesDto
{
    public List<GitHubRepositoryDto> Repositories { get; init; } = [];
}

internal sealed record GitHubInstallationDto
{
    public long Id { get; init; }

    public GitHubAccountDto Account { get; init; } = new();

    public Dictionary<string, string>? Permissions { get; init; }

    public DateTimeOffset? SuspendedAt { get; init; }
}

internal sealed record GitHubOrganisationMembershipDto
{
    public string? State { get; init; }

    public string? Role { get; init; }
}

internal sealed record GitHubAccessTokenDto
{
    public string Token { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }
}

internal sealed record GitHubOAuthTokenRequestDto
{
    public required string ClientId { get; init; }

    public required string ClientSecret { get; init; }

    public required string Code { get; init; }
}

internal sealed record GitHubOAuthTokenDto
{
    public string? AccessToken { get; init; }

    public string? Error { get; init; }

    public string? ErrorDescription { get; init; }
}

internal sealed record GitHubCommitShaDto
{
    public string Sha { get; init; } = string.Empty;
}

internal sealed record GitHubBranchDto
{
    public string Name { get; init; } = string.Empty;

    public GitHubCommitShaDto Commit { get; init; } = new();
}

internal sealed record GitHubGitActorDto
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public DateTimeOffset? Date { get; init; }
}

internal sealed record GitHubCommitDetailDto
{
    public string Message { get; init; } = string.Empty;

    public GitHubGitActorDto? Author { get; init; }
}

internal sealed record GitHubCommitDto
{
    public string Sha { get; init; } = string.Empty;

    public string HtmlUrl { get; init; } = string.Empty;

    public GitHubCommitDetailDto Commit { get; init; } = new();

    public GitHubAccountDto? Author { get; init; }
}

internal sealed record GitHubCompareDto
{
    public List<GitHubCommitDto> Commits { get; init; } = [];
}

internal sealed record GitHubBranchRefDto
{
    public string Ref { get; init; } = string.Empty;

    public string? Sha { get; init; }
}

internal sealed record GitHubPullRequestDto
{
    public long Id { get; init; }

    public int Number { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Body { get; init; }

    public string State { get; init; } = string.Empty;

    public bool Draft { get; init; }

    public bool? Merged { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset? MergedAt { get; init; }

    public DateTimeOffset? ClosedAt { get; init; }

    public string HtmlUrl { get; init; } = string.Empty;

    public GitHubBranchRefDto Head { get; init; } = new();

    public GitHubBranchRefDto Base { get; init; } = new();

    public GitHubAccountDto? User { get; init; }
}

internal sealed record GitHubPullRequestNumberDto
{
    public int Number { get; init; }
}

internal sealed record GitHubCheckRunDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string? Conclusion { get; init; }

    public string HeadSha { get; init; } = string.Empty;

    public string HtmlUrl { get; init; } = string.Empty;

    public DateTimeOffset? CompletedAt { get; init; }

    public List<GitHubPullRequestNumberDto>? PullRequests { get; init; }
}

internal sealed record GitHubCheckRunsDto
{
    public List<GitHubCheckRunDto> CheckRuns { get; init; } = [];
}

internal sealed record GitHubErrorDto
{
    public string? Message { get; init; }
}

internal sealed record GitHubTreeDto
{
    public string Sha { get; init; } = string.Empty;

    public List<GitHubTreeEntryDto> Tree { get; init; } = [];

    public bool Truncated { get; init; }
}

internal sealed record GitHubTreeEntryDto
{
    public string Path { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public long? Size { get; init; }
}

internal sealed record GitHubContentDto
{
    public string Type { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public long Size { get; init; }

    public string? Content { get; init; }

    public string? Encoding { get; init; }

    public string HtmlUrl { get; init; } = string.Empty;
}

internal sealed record GitHubCodeSearchDto
{
    public List<GitHubCodeSearchItemDto> Items { get; init; } = [];
}

internal sealed record GitHubCodeSearchItemDto
{
    public string Path { get; init; } = string.Empty;

    public string HtmlUrl { get; init; } = string.Empty;

    public List<GitHubTextMatchDto>? TextMatches { get; init; }
}

internal sealed record GitHubTextMatchDto
{
    public string? Fragment { get; init; }
}

internal sealed record GitHubPullRequestFileDto
{
    public string Filename { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int Additions { get; init; }

    public int Deletions { get; init; }

    public string? Patch { get; init; }
}
