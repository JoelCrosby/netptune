using Netptune.Transfer.Enums;
using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Mapping;

using static Netptune.Import.Vendors.VendorMappingBuilder;

namespace Netptune.Import.Vendors;

public sealed class AsanaImportVendorProfile : IImportVendorProfile
{
    private static readonly string[] Required = ["Task ID", "Name"];

    private static readonly string[] Optional =
        ["Created At", "Completed At", "Section/Column", "Assignee Email", "Due Date", "Tags", "Parent task", "Notes"];

    public ImportVendorProfile Vendor => ImportVendorProfile.Asana;

    public double Fingerprint(ImportSourceProfile profile)
    {
        return VendorMappingBuilder.Fingerprint(profile, Required, Optional);
    }

    public ImportMappingModel BuildMapping(ImportSourceProfile profile)
    {
        var bindings = Compact(
            Bind(profile, TaskFieldKeys.SystemId, "Task ID"),
            Bind(profile, TaskFieldKeys.Name, "Name"),
            Bind(profile, TaskFieldKeys.Description, "Notes"),
            Bind(profile, TaskFieldKeys.BoardGroup, "Section/Column"),
            BindAny(profile, TaskFieldKeys.Assignees, ["Assignee Email", "Assignee"]),
            Bind(profile, TaskFieldKeys.DueDate, "Due Date"),
            Bind(profile, TaskFieldKeys.StartDate, "Start Date"),
            Bind(profile, TaskFieldKeys.CreatedAt, "Created At"),
            Bind(profile, TaskFieldKeys.Tags, "Tags", SplitOn(",")));

        return Build(TransferRecordTypes.Task, bindings, UpsertOn(TaskFieldKeys.SystemId));
    }
}
