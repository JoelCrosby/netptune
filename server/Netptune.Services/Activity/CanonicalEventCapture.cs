using Netptune.Core.Services.Activity;

namespace Netptune.Services.Activity;

public sealed class CanonicalEventCapture : ICanonicalEventCapture
{
    private readonly HashSet<(int WorkspaceId, string SubjectType, string SubjectId)> Subjects = [];

    public void Record(int workspaceId, string subjectType, string subjectId)
    {
        Subjects.Add((workspaceId, subjectType, subjectId));
    }

    public bool Contains(int workspaceId, string subjectType, string subjectId)
    {
        return Subjects.Contains((workspaceId, subjectType, subjectId));
    }
}
