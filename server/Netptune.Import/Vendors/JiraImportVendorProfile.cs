using Netptune.Transfer.Enums;
using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Mapping;

using static Netptune.Import.Vendors.VendorMappingBuilder;

namespace Netptune.Import.Vendors;

// Jira's CSV export. Its distinguishing quirk is repeated headers: "Sprint" and "Labels" appear
// once per value, so each is folded into a single collection binding.
public sealed class JiraImportVendorProfile : IImportVendorProfile
{
    private static readonly string[] Required = ["Issue key", "Summary"];

    private static readonly string[] Optional =
        ["Issue Type", "Status", "Assignee", "Reporter", "Sprint", "Labels", "Story Points", "Parent", "Description", "Due Date"];

    public ImportVendorProfile Vendor => ImportVendorProfile.Jira;

    public double Fingerprint(ImportSourceProfile profile)
    {
        return VendorMappingBuilder.Fingerprint(profile, Required, Optional);
    }

    public ImportMappingModel BuildMapping(ImportSourceProfile profile)
    {
        var bindings = Compact(
            Bind(profile, TaskFieldKeys.SystemId, "Issue key"),
            Bind(profile, TaskFieldKeys.Name, "Summary"),
            Bind(profile, TaskFieldKeys.Description, "Description"),
            Bind(profile, TaskFieldKeys.Status, "Status"),
            Bind(profile, TaskFieldKeys.Priority, "Priority"),
            Bind(profile, TaskFieldKeys.EstimateValue, "Story Points"),
            Bind(profile, TaskFieldKeys.DueDate, "Due Date"),
            Bind(profile, TaskFieldKeys.CreatedAt, "Created"),
            Bind(profile, TaskFieldKeys.Sprint, "Sprint"),
            BindAny(profile, TaskFieldKeys.Assignees, ["Assignee", "Assignee Id"]),
            Bind(profile, TaskFieldKeys.CreatedBy, "Reporter"),
            Bind(profile, TaskFieldKeys.Tags, "Labels"));

        return Build(TransferRecordTypes.Task, bindings, UpsertOn(TaskFieldKeys.SystemId));
    }
}
