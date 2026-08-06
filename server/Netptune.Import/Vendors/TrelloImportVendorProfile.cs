using Netptune.Transfer.Enums;
using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Mapping;

using static Netptune.Import.Vendors.VendorMappingBuilder;

namespace Netptune.Import.Vendors;

// Trello's JSON export, read as the `cards[]` array. Lists map to board groups and labels to tags,
// so the JSON reader's flattened key names are what this profile binds.
public sealed class TrelloImportVendorProfile : IImportVendorProfile
{
    private static readonly string[] Required = ["name", "idList"];

    private static readonly string[] Optional = ["desc", "due", "closed", "labels", "idMembers", "shortLink", "dateLastActivity"];

    public ImportVendorProfile Vendor => ImportVendorProfile.Trello;

    public double Fingerprint(ImportSourceProfile profile)
    {
        var isJson = profile.Kind is ImportSourceKind.Json or ImportSourceKind.Ndjson;

        if (!isJson)
        {
            return 0;
        }

        return VendorMappingBuilder.Fingerprint(profile, Required, Optional);
    }

    public ImportMappingModel BuildMapping(ImportSourceProfile profile)
    {
        var bindings = Compact(
            Bind(profile, TaskFieldKeys.SystemId, "shortLink"),
            Bind(profile, TaskFieldKeys.Name, "name"),
            Bind(profile, TaskFieldKeys.Description, "desc"),
            Bind(profile, TaskFieldKeys.BoardGroup, "idList"),
            Bind(profile, TaskFieldKeys.DueDate, "due"),
            Bind(profile, TaskFieldKeys.Tags, "labels"),
            Bind(profile, TaskFieldKeys.Assignees, "idMembers"));

        return Build(TransferRecordTypes.Task, bindings, UpsertOn(TaskFieldKeys.SystemId));
    }
}

// Netptune's own CSV export, so an export round-trips back through import unchanged.
public sealed class NetptuneImportVendorProfile : IImportVendorProfile
{
    private static readonly string[] Required = ["System id", "Name"];

    private static readonly string[] Optional =
        ["Description", "Status", "Priority", "Estimate", "Start date", "Due date", "Project", "Sprint", "Board group", "Assignees", "Tags", "Created by", "Created at", "Updated at"];

    public ImportVendorProfile Vendor => ImportVendorProfile.Netptune;

    public double Fingerprint(ImportSourceProfile profile)
    {
        return VendorMappingBuilder.Fingerprint(profile, Required, Optional);
    }

    public ImportMappingModel BuildMapping(ImportSourceProfile profile)
    {
        var bindings = TransferFieldCatalog.Task.Fields
            .Select(field => Bind(profile, field.Key, field.Name, field.IsCollection ? SplitOn("|") : null))
            .Where(binding => binding is not null)
            .Select(binding => binding!);

        return Build(TransferRecordTypes.Task, bindings, UpsertOn(TaskFieldKeys.SystemId));
    }
}
