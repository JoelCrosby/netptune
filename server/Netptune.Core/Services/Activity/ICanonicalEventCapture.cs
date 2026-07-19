namespace Netptune.Core.Services.Activity;

public interface ICanonicalEventCapture
{
    void Record(int workspaceId, string subjectType, string subjectId);

    bool Contains(int workspaceId, string subjectType, string subjectId);
}
