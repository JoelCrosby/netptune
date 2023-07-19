namespace Netptune.Core.Events;

public record EventMessage
{
    public required string Type { get; init; }

    public required string Payload { get; init; }
}
