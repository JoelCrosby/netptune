using System.Text.Json.Serialization;

namespace Netptune.Core.Events;

public record EventMessage
{
    public required string Type { get; init; }

    public required string Payload { get; init; }
}

[JsonSerializable(typeof(EventMessage))]
// ReSharper disable once UnusedType.Global
public partial class NatsJsonContext : JsonSerializerContext;
