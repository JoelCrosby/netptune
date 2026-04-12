namespace Netptune.Core.Authorization;

public static class WorkspaceRolePermissions
{
    private static readonly IReadOnlySet<string> ViewerPermissions = new HashSet<string>
    {
        NetptunePermissions.Workspace.Read,
        NetptunePermissions.Members.Read,
        NetptunePermissions.Members.UpdateProfile,
        NetptunePermissions.Projects.Read,
        NetptunePermissions.Boards.Read,
        NetptunePermissions.BoardGroups.Read,
        NetptunePermissions.Tasks.Read,
        NetptunePermissions.Comments.Read,
        NetptunePermissions.Tags.Read,
        NetptunePermissions.Activity.Read,
        NetptunePermissions.Storage.UploadProfilePicture,
        NetptunePermissions.Export.ProjectTasks,
    };

    private static readonly IReadOnlySet<string> MemberPermissions = new HashSet<string>(ViewerPermissions)
    {
        NetptunePermissions.Projects.Create,
        NetptunePermissions.Projects.Update,
        NetptunePermissions.Boards.Create,
        NetptunePermissions.Boards.Update,
        NetptunePermissions.BoardGroups.Create,
        NetptunePermissions.BoardGroups.Update,
        NetptunePermissions.BoardGroups.Delete,
        NetptunePermissions.Tasks.Create,
        NetptunePermissions.Tasks.Update,
        NetptunePermissions.Tasks.Delete,
        NetptunePermissions.Tasks.Move,
        NetptunePermissions.Tasks.Reassign,
        NetptunePermissions.Comments.Create,
        NetptunePermissions.Comments.DeleteOwn,
        NetptunePermissions.Tags.Create,
        NetptunePermissions.Tags.Update,
        NetptunePermissions.Tags.Assign,
        NetptunePermissions.Storage.UploadMedia,
    };

    private static readonly IReadOnlySet<string> AdminPermissions = new HashSet<string>(MemberPermissions)
    {
        NetptunePermissions.Workspace.Create,
        NetptunePermissions.Workspace.Update,
        NetptunePermissions.Workspace.Delete,
        NetptunePermissions.Projects.Delete,
        NetptunePermissions.Boards.Delete,
        NetptunePermissions.Tasks.DeleteAny,
        NetptunePermissions.Comments.DeleteAny,
        NetptunePermissions.Tags.Delete,
        NetptunePermissions.Members.Invite,
        NetptunePermissions.Members.Remove,
        NetptunePermissions.Import.ProjectTasks,
    };

    private static readonly IReadOnlySet<string> OwnerPermissions = new HashSet<string>(AdminPermissions)
    {
        NetptunePermissions.Workspace.DeletePermanent,
    };

    public static IReadOnlySet<string> GetDefaultPermissions(WorkspaceRole role) => role switch
    {
        WorkspaceRole.Viewer => ViewerPermissions,
        WorkspaceRole.Member => MemberPermissions,
        WorkspaceRole.Admin => AdminPermissions,
        WorkspaceRole.Owner => OwnerPermissions,
        _ => ViewerPermissions,
    };
}
