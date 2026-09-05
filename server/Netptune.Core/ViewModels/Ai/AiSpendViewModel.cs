namespace Netptune.Core.ViewModels.Ai;

public sealed record AiSpendDayViewModel
{
    public DateTime Day { get; init; }

    public decimal Cost { get; init; }
}

public sealed record AiSpendMemberViewModel
{
    public required string UserId { get; init; }

    public required string UserDisplayName { get; init; }

    public int Conversations { get; init; }

    public AiTokenUsageViewModel Usage { get; init; } = new();
}

public sealed record AiSpendViewModel
{
    public decimal MonthToDate { get; init; }

    public decimal? Cap { get; init; }

    public decimal Projected { get; init; }

    public DateTime PeriodStart { get; init; }

    public DateTime PeriodEnd { get; init; }

    public int Conversations { get; init; }

    public AiTokenUsageViewModel Usage { get; init; } = new();

    public List<AiSpendDayViewModel> Daily { get; init; } = [];

    public List<AiSpendMemberViewModel> Members { get; init; } = [];
}
