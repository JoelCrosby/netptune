namespace Netptune.Api.Configuration;

public sealed record ApiTag(string Name, string Description);

public sealed record ApiTagGroup(string Name, IReadOnlyList<ApiTag> Tags);

public static class ApiTags
{
    public static readonly ApiTag Workspace = new("Workspace", "The workspace the credential belongs to.");

    public static readonly ApiTag Assignees = new(
        "Assignees",
        "The members tasks can be assigned to, without their email addresses.");

    public static readonly ApiTag Tasks = new(
        "Tasks",
        "Create, read, update, move and archive tasks. Tasks are identified by their system id, such as `NPT-42`. "
        + "A task's comments, tags and relations are under Comments, Tags and Relations.");

    public static readonly ApiTag Comments = new("Comments", "Comments on tasks, and reactions to them.");

    public static readonly ApiTag Tags = new("Tags", "The workspace's tags, and the tags on each task.");

    public static readonly ApiTag Relations = new(
        "Relations",
        "Relation types, such as blocks, and the links they make between tasks.");

    public static readonly ApiTag Projects = new(
        "Projects",
        "Projects hold tasks under a key, such as `NPT`, that starts every task's system id.");

    public static readonly ApiTag Sprints = new(
        "Sprints",
        "Time-boxed sprints in a project, and the tasks planned into them.");

    public static readonly ApiTag Boards = new("Boards", "Boards, and the board view of their columns and tasks.");

    public static readonly ApiTag BoardGroups = new(
        "Board groups",
        "The columns on a board. Move tasks between them with their ids.");

    public static readonly ApiTag Statuses = new("Statuses", "The workflow statuses tasks move through.");

    public static readonly ApiTag Pages = new(
        "Pages",
        "The workspace's pages: documents and folders, their versions, and the links between them.");

    public static readonly ApiTag Search = new("Search", "Search across tasks, projects, boards, sprints and pages.");

    public static readonly ApiTag Reports = new("Reports", "Flow, workload, sprint burndown and velocity reports.");

    public static readonly IReadOnlyList<ApiTagGroup> Groups =
    [
        new("Workspace", [Workspace, Assignees]),
        new("Work", [Tasks, Comments, Tags, Relations]),
        new("Planning", [Projects, Sprints, Boards, BoardGroups, Statuses]),
        new("Knowledge", [Pages]),
        new("Insight", [Search, Reports]),
    ];
}
