namespace Netptune.Core.SourceControl.Models;

public sealed record ScmFileContent
{
    public required string Path { get; init; }

    public long Size { get; init; }

    public string? Text { get; init; }

    public bool IsBinary { get; init; }

    public bool IsTooLarge { get; init; }

    public required string WebUrl { get; init; }
}
