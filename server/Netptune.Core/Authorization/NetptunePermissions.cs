using System.Collections.Frozen;
using System.Reflection;

namespace Netptune.Core.Authorization;

public static class NetptunePermissions
{
    public static IReadOnlySet<string> All { get; } = typeof(NetptunePermissions)
        .GetNestedTypes(BindingFlags.Public)
        .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
        .Select(field => (string)field.GetRawConstantValue()!)
        .ToFrozenSet();

    public static class Workspace
    {
        public const string Read = "workspace.read";
        public const string Create = "workspace.create";
        public const string Update = "workspace.update";
        public const string Delete = "workspace.delete";
        public const string DeletePermanent = "workspace.delete_permanent";
    }

    public static class Members
    {
        public const string Read = "members.read";
        public const string Invite = "members.invite";
        public const string Remove = "members.remove";
        public const string UpdateProfile = "members.update_profile";
        public const string UpdatePermission = "members.update_permission";
        public const string UpdateRole = "members.update_role";
    }

    public static class Projects
    {
        public const string Read = "projects.read";
        public const string Create = "projects.create";
        public const string Update = "projects.update";
        public const string Delete = "projects.delete";
    }

    public static class Boards
    {
        public const string Read = "boards.read";
        public const string Create = "boards.create";
        public const string Update = "boards.update";
        public const string Delete = "boards.delete";
    }

    public static class BoardGroups
    {
        public const string Read = "board_groups.read";
        public const string Create = "board_groups.create";
        public const string Update = "board_groups.update";
        public const string Delete = "board_groups.delete";
    }

    public static class Tasks
    {
        public const string Read = "tasks.read";
        public const string Create = "tasks.create";
        public const string Update = "tasks.update";
        public const string Delete = "tasks.delete";
        public const string DeleteAny = "tasks.delete_any";
        public const string Restore = "tasks.restore";
        public const string Move = "tasks.move";
        public const string Reassign = "tasks.reassign";
        public const string Export = "tasks.export";
        public const string Import = "tasks.import";
    }

    public static class Sprints
    {
        public const string Read = "sprints.read";
        public const string Create = "sprints.create";
        public const string Update = "sprints.update";
        public const string Delete = "sprints.delete";
        public const string ManageTasks = "sprints.manage_tasks";
    }

    public static class Comments
    {
        public const string Read = "comments.read";
        public const string Create = "comments.create";
        public const string DeleteOwn = "comments.delete_own";
        public const string DeleteAny = "comments.delete_any";
    }

    public static class Tags
    {
        public const string Read = "tags.read";
        public const string Create = "tags.create";
        public const string Update = "tags.update";
        public const string Delete = "tags.delete";
        public const string Assign = "tags.assign";
    }

    public static class Statuses
    {
        public const string Read = "statuses.read";
        public const string Manage = "statuses.manage";
    }

    public static class RelationTypes
    {
        public const string Read = "relation_types.read";
        public const string Manage = "relation_types.manage";
    }

    public static class Activity
    {
        public const string Read = "activity.read";
    }

    public static class Audit
    {
        public const string Read = "audit.read";
        public const string Export = "audit.export";
    }

    public static class Notifications
    {
        public const string Read = "notifications.read";
        public const string Update = "notifications.update";
    }

    public static class Automations
    {
        public const string Read = "automations.read";
        public const string Manage = "automations.manage";
    }

    public static class ServiceAccounts
    {
        public const string Read = "service_accounts.read";
        public const string Create = "service_accounts.create";
        public const string Update = "service_accounts.update";
        public const string Delete = "service_accounts.delete";
        public const string ManageCredentials = "service_accounts.manage_credentials";
    }

    public static class Storage
    {
        public const string UploadProfilePicture = "storage.upload_profile_picture";
        public const string UploadMedia = "storage.upload_media";
        public const string Read = "storage.read";
        public const string Manage = "storage.manage";
    }

    public static class Files
    {
        public const string Read = "files.read";
        public const string Upload = "files.upload";
        public const string DeleteOwn = "files.delete_own";
        public const string DeleteAny = "files.delete_any";
    }
}
