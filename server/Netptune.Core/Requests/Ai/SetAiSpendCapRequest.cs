namespace Netptune.Core.Requests.Ai;

public sealed record SetAiSpendCapRequest
{
    public decimal? Cap { get; init; }
}
