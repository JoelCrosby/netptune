namespace Netptune.Transfer.ViewModels;

public sealed record TransferCatalogViewModel
{
    public IReadOnlyList<TransferRecordType> RecordTypes { get; init; } = [];
}
