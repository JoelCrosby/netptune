using Netptune.Transfer.Mapping;

namespace Netptune.Transfer.ViewModels;

public sealed record ImportSessionStateViewModel
{
    public required ImportSessionViewModel Session { get; init; }

    public ImportSourceProfile? SourceProfile { get; init; }

    public ImportMappingModel? Mapping { get; init; }

    public ImportPreviewResult? PreviewResult { get; init; }
}
