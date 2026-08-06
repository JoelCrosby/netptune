using Netptune.Core.Encoding;
using Netptune.Core.Entities;

namespace Netptune.Transfer;

public static class EntityRefBuilder
{
    public const string UnnamedSegment = "unnamed";

    public static EntityRef ForWorkspace(string slug) => new(EntityRefTypes.Workspace, Key(slug));

    public static EntityRef ForWorkspace(Workspace workspace) => ForWorkspace(workspace.Slug);

    public static EntityRef ForUser(string email) => new(EntityRefTypes.User, Key(email));

    public static EntityRef ForUser(AppUser user) => ForUser(user.NormalizedEmail ?? user.Email ?? user.UserName!);

    public static EntityRef ForStatus(string key) => new(EntityRefTypes.Status, Key(key));

    public static EntityRef ForStatus(Status status) => ForStatus(status.Key);

    public static EntityRef ForTag(string name) => new(EntityRefTypes.Tag, Key(name));

    public static EntityRef ForTag(Tag tag) => ForTag(tag.Name);

    public static EntityRef ForRelationType(string key) => new(EntityRefTypes.RelationType, Key(key));

    public static EntityRef ForRelationType(RelationType relationType) => ForRelationType(relationType.Key);

    public static EntityRef ForProject(string key) => new(EntityRefTypes.Project, Key(key));

    public static EntityRef ForProject(Project project) => ForProject(project.Key);

    public static EntityRef ForBoard(string identifier) => new(EntityRefTypes.Board, Key(identifier));

    public static EntityRef ForBoard(Board board) => ForBoard(board.Identifier);

    public static EntityRef ForBoardGroup(string boardIdentifier, string name)
    {
        return new EntityRef(EntityRefTypes.BoardGroup, $"{Key(boardIdentifier)}/{Slug(name)}");
    }

    public static EntityRef ForBoardGroup(Board board, BoardGroup boardGroup)
    {
        return ForBoardGroup(board.Identifier, boardGroup.Name);
    }

    public static EntityRef ForSprint(string projectKey, string name)
    {
        return new EntityRef(EntityRefTypes.Sprint, $"{Key(projectKey)}/{Slug(name)}");
    }

    public static EntityRef ForSprint(Project project, Sprint sprint)
    {
        return ForSprint(project.Key, sprint.Name);
    }

    public static EntityRef ForTask(string projectKey, int projectScopeId)
    {
        return new EntityRef(EntityRefTypes.Task, $"{Key(projectKey)}-{projectScopeId}");
    }

    public static EntityRef ForTask(Project project, ProjectTask task)
    {
        return ForTask(project.Key, task.ProjectScopeId);
    }

    public static EntityRef ForComment(EntityRef subject, int ordinal)
    {
        return new EntityRef(EntityRefTypes.Comment, $"{subject.Value}#{ordinal}");
    }

    public static EntityRef ForAutomation(string name) => new(EntityRefTypes.Automation, Slug(name));

    public static EntityRef ForAutomation(AutomationRule rule) => ForAutomation(rule.Name);

    public static EntityRef ForWorkspaceFile(string contentId) => new(EntityRefTypes.WorkspaceFile, Key(contentId));

    public static EntityRef ForWorkspaceFile(WorkspaceFile file) => ForWorkspaceFile(file.ContentId);

    private static string Key(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string Slug(string value)
    {
        var slug = value.ToUrlSlug();

        if (slug.Length == 0)
        {
            return UnnamedSegment;
        }

        return slug;
    }
}
