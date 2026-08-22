namespace Netptune.Query.Validation;

public sealed record QueryValidationResult
{
    public static QueryValidationResult Valid { get; } = new() { Errors = [] };

    public required IReadOnlyList<QueryValidationError> Errors { get; init; }

    public bool IsValid => Errors.Count == 0;

    public string ToMessage()
    {
        return string.Join(" ", Errors.Select(error => error.Message));
    }
}
