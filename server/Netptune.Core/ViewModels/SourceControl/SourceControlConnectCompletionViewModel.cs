namespace Netptune.Core.ViewModels.SourceControl;

public sealed record SourceControlConnectCompletionViewModel
{
    public SourceControlConnectionViewModel? Connection { get; init; }

    public SourceControlConnectChoiceViewModel? Choice { get; init; }
}
