namespace Netptune.Core.SourceControl.Webhooks;

public sealed record ScmWebhookRequest
{
    public required ReadOnlyMemory<byte> Body { get; init; }

    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    public string? GetHeader(string name)
    {
        foreach (var (key, value) in Headers)
        {
            var matches = string.Equals(key, name, StringComparison.OrdinalIgnoreCase);

            if (matches)
            {
                return value;
            }
        }

        return null;
    }
}
