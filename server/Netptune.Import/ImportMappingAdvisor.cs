using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;
using Netptune.Transfer.Services;

namespace Netptune.Import;

public sealed class ImportMappingAdvisor : IImportMappingAdvisor
{
    public const double MinimumVendorConfidence = 0.6;

    private readonly IEnumerable<IImportVendorProfile> Vendors;
    private readonly ImportMappingSuggester Suggester;

    public ImportMappingAdvisor(IEnumerable<IImportVendorProfile> vendors, ImportMappingSuggester suggester)
    {
        Vendors = vendors;
        Suggester = suggester;
    }

    public ImportMappingSuggestion Suggest(
        string recordType,
        ImportSourceProfile profile,
        ImportSuggestionVocabulary? vocabulary = null)
    {
        var matched = Vendors
            .Select(vendor => new { Vendor = vendor, Confidence = vendor.Fingerprint(profile) })
            .Where(candidate => candidate.Confidence >= MinimumVendorConfidence)
            .MaxBy(candidate => candidate.Confidence);

        if (matched is not null)
        {
            var vendorMapping = matched.Vendor.BuildMapping(profile);

            return new ImportMappingSuggestion
            {
                Mapping = vendorMapping,
                Vendor = matched.Vendor.Vendor,
                VendorConfidence = Math.Round(matched.Confidence, 2),
                UnmappedColumns = UnmappedColumns(profile, vendorMapping),
            };
        }

        var suggested = Suggester.Suggest(recordType, profile, vocabulary);

        return new ImportMappingSuggestion
        {
            Mapping = suggested.Mapping,
            Vendor = ImportVendorProfile.None,
            UnmappedColumns = suggested.UnmappedColumns,
        };
    }

    private static List<int> UnmappedColumns(ImportSourceProfile profile, ImportMappingModel mapping)
    {
        var bound = mapping.Bindings
            .SelectMany(binding => binding.AdditionalColumnIndexes.Append(binding.ColumnIndex ?? -1))
            .ToHashSet();

        return profile.Columns
            .Select(column => column.Index)
            .Where(index => !bound.Contains(index))
            .ToList();
    }
}
