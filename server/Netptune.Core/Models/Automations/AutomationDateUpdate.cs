using Netptune.Core.Enums;

namespace Netptune.Core.Models.Automations;

public sealed record AutomationDateUpdate
{
    public AutomationDateUpdateMode Mode { get; init; }

    public DateOnly? Date { get; init; }

    public int? Offset { get; init; }
}
