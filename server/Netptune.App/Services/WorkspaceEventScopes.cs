namespace Netptune.App.Services;

// The entity kinds a workspace event can name. The client turns each one into the views it
// invalidates, so these strings match the entity types the assistant already reports.
public static class WorkspaceEventScopes
{
    public const string Task = "task";

    public const string Board = "board";

    public const string Sprint = "sprint";

    public const string Project = "project";

    public const string Tag = "tag";

    public const string Status = "status";

    public const string Comment = "comment";
}
