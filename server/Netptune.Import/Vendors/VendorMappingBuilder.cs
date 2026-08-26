using Netptune.Transfer;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Mapping;

namespace Netptune.Import.Vendors;

internal static class VendorMappingBuilder
{
    public static double Fingerprint(ImportSourceProfile profile, IReadOnlyList<string> required, IReadOnlyList<string> optional)
    {
        var headers = profile.Columns.Select(column => Normalize(column.Name)).ToHashSet();
        var hasEveryRequiredHeader = required.All(header => headers.Contains(Normalize(header)));

        if (!hasEveryRequiredHeader)
        {
            return 0;
        }

        var optionalHits = optional.Count(header => headers.Contains(Normalize(header)));
        var optionalShare = optional.Count == 0 ? 0 : (double)optionalHits / optional.Count;

        return Math.Min(1.0, 0.6 + (optionalShare * 0.4));
    }

    public static ImportMappingModel Build(string recordType, IEnumerable<ImportFieldBinding> bindings, ImportDedupeModel? dedupe = null)
    {
        return new ImportMappingModel
        {
            RecordType = recordType,
            Bindings = bindings.OrderBy(binding => binding.ColumnIndex).ToList(),
            Dedupe = dedupe,
        };
    }

    public static ImportFieldBinding? Bind(
        ImportSourceProfile profile,
        string fieldKey,
        string header,
        IReadOnlyList<ImportTransform>? transforms = null)
    {
        var columns = FindColumnsNamed(profile, header);

        if (columns.Count == 0)
        {
            return null;
        }

        return new ImportFieldBinding
        {
            FieldKey = fieldKey,
            ColumnIndex = columns[0],
            AdditionalColumnIndexes = columns.Skip(1).ToList(),
            Confidence = 1.0,
            Origin = ImportBindingOrigin.Vendor,
            Transforms = transforms?.ToList() ?? [],
        };
    }

    public static ImportFieldBinding? BindAny(
        ImportSourceProfile profile,
        string fieldKey,
        IReadOnlyList<string> headers,
        IReadOnlyList<ImportTransform>? transforms = null)
    {
        foreach (var header in headers)
        {
            var binding = Bind(profile, fieldKey, header, transforms);

            if (binding is not null)
            {
                return binding;
            }
        }

        return null;
    }

    public static List<int> FindColumnsNamed(ImportSourceProfile profile, string header)
    {
        var target = Normalize(header);

        return profile.Columns
            .Where(column => Normalize(column.Name) == target)
            .Select(column => column.Index)
            .ToList();
    }

    public static IEnumerable<ImportFieldBinding> Compact(params ImportFieldBinding?[] bindings)
    {
        return bindings.Where(binding => binding is not null).Select(binding => binding!);
    }

    public static string Normalize(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }

    public static ImportDedupeModel UpsertOn(string fieldKey)
    {
        return new ImportDedupeModel
        {
            KeyFieldKey = fieldKey,
            Action = ImportDedupeAction.UpdateExisting,
        };
    }

    public static IReadOnlyList<ImportTransform> SplitOn(string separator)
    {
        return [new ImportTransform { Kind = ImportTransformKind.SplitOn, Argument = separator }];
    }

    public static string TaskField(string name)
    {
        return $"{TransferRecordTypes.Task}.{name}";
    }
}
