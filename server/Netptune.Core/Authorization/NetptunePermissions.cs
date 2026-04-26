namespace Netptune.Core.Authorization;

public static class NetptunePermissions
{
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
        public const string Move = "tasks.move";
        public const string Reassign = "tasks.reassign";
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

    public static class Activity
    {
        public const string Read = "activity.read";
    }

    public static class Audit
    {
        public const string Read = "audit.read";
        public const string Export = "audit.export";
        public const string Anonymise = "audit.anonymise";
    }

    public static class Notifications
    {
        public const string Read = "notifications.read";
        public const string Update = "notifications.update";
    }

    public static class Storage
    {
        public const string UploadProfilePicture = "storage.upload_profile_picture";
        public const string UploadMedia = "storage.upload_media";
    }

    public static class Export
    {
        public const string ProjectTasks = "export.tasks";
    }

    public static class Import
    {
        public const string ProjectTasks = "import.tasks";
    }
}
