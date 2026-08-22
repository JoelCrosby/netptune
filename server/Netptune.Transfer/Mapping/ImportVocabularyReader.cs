using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;

namespace Netptune.Transfer.Mapping;

// The workspace values an import mapping is allowed to land on. The heuristic suggester and the
// assistant both need the same picture, so they read it from here rather than each assembling its own.
public static class ImportVocabularyReader
{
    public static async Task<ImportSuggestionVocabulary> Read(
        INetptuneUnitOfWork unitOfWork,
        int workspaceId,
        string workspaceKey,
        CancellationToken cancellationToken)
    {
        var statuses = await unitOfWork.Statuses.GetAllInWorkspace(workspaceId, cancellationToken: cancellationToken);
        var tags = await unitOfWork.Tags.GetTagsInWorkspace(workspaceId, true, cancellationToken);
        var members = await unitOfWork.Users.GetWorkspaceUsers(workspaceKey, true, cancellationToken);
        var projects = await unitOfWork.Projects.GetAllInWorkspace(workspaceId, cancellationToken: cancellationToken);

        return new ImportSuggestionVocabulary
        {
            StatusKeysByName = statuses
                .Where(status => status.EntityType == EntityType.Task)
                .GroupBy(status => status.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.OrdinalIgnoreCase),
            TagNames = tags.Select(tag => tag.Name).ToList(),
            MemberEmails = members.Where(user => user.Email is not null).Select(user => user.Email!).ToList(),
            ProjectKeys = projects.Select(project => project.Key).ToList(),
        };
    }
}
