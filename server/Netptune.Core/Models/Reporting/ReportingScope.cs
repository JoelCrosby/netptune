namespace Netptune.Core.Models.Reporting;

public sealed record ReportingScope(int WorkspaceId, IReadOnlySet<int> ProjectIds)
{
    public bool CanAccessProject(int projectId) => ProjectIds.Contains(projectId);
}
