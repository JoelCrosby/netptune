namespace Netptune.SourceControl.Providers.AzureDevOps.Dtos;

internal sealed record AzureDevOpsListDto<T>
{
    public int Count { get; init; }

    public List<T> Value { get; init; } = [];
}

internal sealed record AzureDevOpsConnectionDataDto
{
    public string? InstanceId { get; init; }
}

internal sealed record AzureDevOpsProjectDto
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

internal sealed record AzureDevOpsRepositoryDto
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? DefaultBranch { get; init; }

    public string WebUrl { get; init; } = string.Empty;

    public bool IsDisabled { get; init; }

    public AzureDevOpsProjectDto Project { get; init; } = new();
}

internal sealed record AzureDevOpsErrorDto
{
    public string? Message { get; init; }
}
