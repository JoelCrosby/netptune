using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;

namespace Netptune.Transfer.Services;

public sealed record ImportMappingSuggestion
{
    public required ImportMappingModel Mapping { get; init; }

    public ImportVendorProfile Vendor { get; init; } = ImportVendorProfile.None;

    public double VendorConfidence { get; init; }

    public IReadOnlyList<int> UnmappedColumns { get; init; } = [];
}

public interface IImportMappingAdvisor
{
    ImportMappingSuggestion Suggest(
        string recordType,
        ImportSourceProfile profile,
        ImportSuggestionVocabulary? vocabulary = null);
}
