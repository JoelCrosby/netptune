using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiSpendService : IAiSpendService
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public AiSpendService(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async Task<AiSpendStatus> GetStatus(Workspace workspace, CancellationToken cancellationToken = default)
    {
        var cap = workspace.AssistantSpendCap;

        if (cap is null)
        {
            return new AiSpendStatus();
        }

        var periodStart = CurrentPeriodStart();
        var tokens = await UnitOfWork.AiConversations.GetModelTokens(workspace.Id, periodStart, cancellationToken);
        var monthToDate = tokens.Sum(CostOf);

        return new AiSpendStatus
        {
            MonthToDate = monthToDate,
            Cap = cap,
        };
    }

    public async Task<AiSpendViewModel> GetSummary(Workspace workspace, CancellationToken cancellationToken = default)
    {
        var periodStart = CurrentPeriodStart();
        var slices = await UnitOfWork.AiConversations.GetSpendSlices(workspace.Id, periodStart, cancellationToken);
        var counts = await UnitOfWork.AiConversations.GetConversationCounts(workspace.Id, periodStart, cancellationToken);
        var usage = TotalUsage(slices);
        var daily = DailySeries(slices, periodStart);
        var members = MemberRows(slices, counts);

        return new AiSpendViewModel
        {
            MonthToDate = usage.Cost,
            Cap = workspace.AssistantSpendCap,
            Projected = Project(usage.Cost, periodStart),
            PeriodStart = periodStart,
            PeriodEnd = periodStart.AddMonths(1),
            Conversations = counts.Sum(count => count.Count),
            Usage = usage,
            Daily = daily,
            Members = members,
        };
    }

    private static AiTokenUsageViewModel TotalUsage(List<AiSpendSlice> slices)
    {
        return new AiTokenUsageViewModel
        {
            InputTokens = slices.Sum(slice => slice.InputTokens),
            OutputTokens = slices.Sum(slice => slice.OutputTokens),
            CacheReadTokens = slices.Sum(slice => slice.CacheReadTokens),
            CacheCreationTokens = slices.Sum(slice => slice.CacheCreationTokens),
            Cost = slices.Sum(CostOf),
        };
    }

    private static List<AiSpendDayViewModel> DailySeries(List<AiSpendSlice> slices, DateTime periodStart)
    {
        var costByDay = slices
            .GroupBy(slice => slice.Day.Date)
            .ToDictionary(group => group.Key, group => group.Sum(CostOf));

        var today = DateTime.UtcNow.Date;
        var days = (int)(today - periodStart.Date).TotalDays + 1;

        return Enumerable
            .Range(0, days)
            .Select(offset => periodStart.Date.AddDays(offset))
            .Select(day => new AiSpendDayViewModel
            {
                Day = day,
                Cost = costByDay.GetValueOrDefault(day),
            })
            .ToList();
    }

    private static List<AiSpendMemberViewModel> MemberRows(
        List<AiSpendSlice> slices,
        List<AiConversationCount> counts)
    {
        var conversationsByUser = counts.ToDictionary(count => count.UserId, count => count.Count);

        return slices
            .GroupBy(slice => slice.UserId)
            .Select(group => new AiSpendMemberViewModel
            {
                UserId = group.Key,
                UserDisplayName = group.First().UserDisplayName,
                Conversations = conversationsByUser.GetValueOrDefault(group.Key),
                Usage = new AiTokenUsageViewModel
                {
                    InputTokens = group.Sum(slice => slice.InputTokens),
                    OutputTokens = group.Sum(slice => slice.OutputTokens),
                    CacheReadTokens = group.Sum(slice => slice.CacheReadTokens),
                    CacheCreationTokens = group.Sum(slice => slice.CacheCreationTokens),
                    Cost = group.Sum(CostOf),
                },
            })
            .OrderByDescending(member => member.Usage.Cost)
            .ThenBy(member => member.UserDisplayName)
            .ToList();
    }

    private static decimal Project(decimal monthToDate, DateTime periodStart)
    {
        var today = DateTime.UtcNow.Date;
        var elapsedDays = (int)(today - periodStart.Date).TotalDays + 1;
        var daysInMonth = DateTime.DaysInMonth(periodStart.Year, periodStart.Month);
        var projected = monthToDate / elapsedDays * daysInMonth;

        return decimal.Round(projected, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CostOf(AiSpendSlice slice)
    {
        return AiModelPricing.Cost(
            slice.Model,
            slice.InputTokens,
            slice.OutputTokens,
            slice.CacheReadTokens,
            slice.CacheCreationTokens);
    }

    private static decimal CostOf(AiModelTokens tokens)
    {
        return AiModelPricing.Cost(
            tokens.Model,
            tokens.InputTokens,
            tokens.OutputTokens,
            tokens.CacheReadTokens,
            tokens.CacheCreationTokens);
    }

    private static DateTime CurrentPeriodStart()
    {
        var today = DateTime.UtcNow;

        return new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
