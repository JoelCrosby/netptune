namespace Netptune.Core.Exceptions;

public sealed class UniqueConstraintException : Exception
{
    public UniqueConstraintException(string? constraintName, Exception inner)
        : base($"A unique constraint was violated: {constraintName ?? "unknown"}.", inner)
    {
        ConstraintName = constraintName;
    }

    public string? ConstraintName { get; }
}
