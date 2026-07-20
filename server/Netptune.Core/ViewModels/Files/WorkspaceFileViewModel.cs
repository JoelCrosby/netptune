using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Files;

public sealed record WorkspaceFileViewModel
{
    public int Id { get; init; }

    public string OriginalName { get; init; } = null!;

    public string ContentType { get; init; } = null!;

    public string ContentTypeGroup { get; init; } = null!;

    public long SizeBytes { get; init; }

    public WorkspaceFilePurpose Purpose { get; init; }

    public DateTime CreatedAt { get; init; }

    public string? UploadedByUserId { get; init; }

    public string? UploadedByDisplayName { get; init; }

    public string? UploadedByPictureUrl { get; init; }

    public bool UploadedByIsServiceAccount { get; init; }

    public int? TaskId { get; init; }

    public string? TaskSystemId { get; init; }

    public string? TaskName { get; init; }

    public required string ContentUrl { get; init; }

    public bool CanDelete { get; init; }
}

public sealed record WorkspaceStorageUsageViewModel
{
    public required long UsedBytes { get; init; }

    public required long LimitBytes { get; init; }

    public required long AvailableBytes { get; init; }

    public required double Percentage { get; init; }

    public required int FileCount { get; init; }
}

public sealed record FileUploadResult
{
    public required string FileName { get; init; }

    public required bool IsSuccess { get; init; }

    public WorkspaceFileViewModel? File { get; init; }

    public string? Error { get; init; }
}
