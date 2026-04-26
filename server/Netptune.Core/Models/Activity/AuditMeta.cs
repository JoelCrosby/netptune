namespace Netptune.Core.Models.Activity;

public record AuditMeta
{
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public object? Before { get; init; }
    public object? After { get; init; }
}
