namespace Netptune.Query.Validation;

public sealed record QueryValidationError
{
    public required string Path { get; init; }

    public required string Message { get; init; }

    public string? Field { get; init; }
}
