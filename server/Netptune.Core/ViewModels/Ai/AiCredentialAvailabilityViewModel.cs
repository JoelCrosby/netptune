using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Ai;

public sealed record AiProviderAvailabilityViewModel
{
    public AiProvider Provider { get; init; }

    public AiCredentialSource Source { get; init; }

    public string? Model { get; init; }
}

public sealed record AiCredentialAvailabilityViewModel
{
    public List<AiProviderAvailabilityViewModel> Providers { get; init; } = [];
}
