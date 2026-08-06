using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Transfer.Services;
using Netptune.Transfer.Mapping;
using Netptune.Core.UnitOfWork;

namespace Netptune.Import;

public sealed class ImportContext
{
    private readonly Dictionary<int, double> SortOrders;
    private readonly Dictionary<string, ProjectTask> ExistingByExternalId;
    private readonly Dictionary<string, ProjectTask> ExistingBySystemId;
    private readonly List<BoardGroup> Groups;

    private int ScopeIdCursor;
    private int ScopeIdLimit;

    public ImportContext(
        Project project,
        Board board,
        ImportVocabulary vocabulary,
        IReadOnlyList<Status> statuses,
        List<BoardGroup> groups,
        IReadOnlyList<ProjectTask> existingTasks)
    {
        Project = project;
        Board = board;
        Vocabulary = vocabulary;
        Groups = groups;

        DefaultStatus = ResolveDefaultStatus(project, statuses);
        DefaultBoardGroup = groups.MinBy(group => group.SortOrder);
        SortOrders = groups.ToDictionary(
            group => group.Id,
            group => group.TasksInGroups.Count == 0 ? 0d : group.TasksInGroups.Max(task => task.SortOrder));

        ExistingByExternalId = existingTasks
            .Where(task => task.ExternalId is not null)
            .GroupBy(task => task.ExternalId!.ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.First());

        // Keyed on the destination project, so only its own tasks can carry a matching system id.
        // Keying every task in the workspace would let a row match a task in another project.
        ExistingBySystemId = existingTasks
            .Where(task => task.ProjectId == project.Id)
            .GroupBy(task => $"{project.Key}-{task.ProjectScopeId}".ToLowerInvariant())
            .ToDictionary(group => group.Key, group => group.First());
    }

    public Project Project { get; }

    public Board Board { get; }

    public ImportVocabulary Vocabulary { get; }

    public Status DefaultStatus { get; }

    public BoardGroup? DefaultBoardGroup { get; private set; }

    // Takes a block of task numbers off the project atomically, the same way every other create path
    // does. Handing them out from a cursor seeded at load time would reissue numbers the last import
    // already used and collide with the unique index on (project_id, project_scope_id).
    public async Task ReserveScopeIds(INetptuneUnitOfWork unitOfWork, int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return;
        }

        var firstScopeId = await unitOfWork.Projects.ReserveTaskScopeIds(Project.Id, count, cancellationToken)
            ?? throw new InvalidOperationException("Task numbers could not be reserved for the import.");

        ScopeIdCursor = firstScopeId;
        ScopeIdLimit = firstScopeId + count;
    }

    public int NextScopeId()
    {
        if (ScopeIdCursor >= ScopeIdLimit)
        {
            throw new InvalidOperationException("The import asked for more task numbers than it reserved.");
        }

        var scopeId = ScopeIdCursor;

        ScopeIdCursor++;

        return scopeId;
    }

    public double NextSortOrder(int boardGroupId)
    {
        var current = SortOrders.GetValueOrDefault(boardGroupId);
        var next = current + 1;

        SortOrders[boardGroupId] = next;

        return next;
    }

    // The one id the file carries is matched against both what earlier imports stored and Netptune's
    // own task numbers, so re-importing either a vendor export or a Netptune export finds its rows.
    public ProjectTask? FindExisting(ResolvedTaskRow row)
    {
        if (string.IsNullOrWhiteSpace(row.SourceId))
        {
            return null;
        }

        var sourceId = row.SourceId.Trim().ToLowerInvariant();

        return ExistingByExternalId.GetValueOrDefault(sourceId) ?? ExistingBySystemId.GetValueOrDefault(sourceId);
    }

    public async Task<Tag> CreateTag(
        INetptuneUnitOfWork unitOfWork,
        string name,
        ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        var tag = new Tag
        {
            Name = name.Trim(),
            WorkspaceId = request.WorkspaceId,
            OwnerId = request.UserId,
            CreatedByUserId = request.UserId,
        };

        await unitOfWork.Tags.AddAsync(tag, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        Vocabulary.Register(tag);

        return tag;
    }

    public async Task<BoardGroup> CreateBoardGroup(
        INetptuneUnitOfWork unitOfWork,
        string name,
        ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        var nextOrder = Groups.Count == 0 ? 0 : Groups.Max(group => group.SortOrder) + 1;
        var group = new BoardGroup
        {
            Name = name.Trim(),
            BoardId = Board.Id,
            WorkspaceId = request.WorkspaceId,
            SortOrder = nextOrder,
            OwnerId = request.UserId,
            CreatedByUserId = request.UserId,
        };

        await unitOfWork.BoardGroups.AddAsync(group, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        Groups.Add(group);
        SortOrders[group.Id] = 0;
        Vocabulary.Register(group);
        DefaultBoardGroup ??= group;

        return group;
    }

    private static Status ResolveDefaultStatus(Project project, IReadOnlyList<Status> statuses)
    {
        var taskStatuses = statuses.Where(status => status.EntityType == EntityType.Task).ToList();

        if (project.DefaultStatusId.HasValue)
        {
            var projectDefault = taskStatuses.FirstOrDefault(status => status.Id == project.DefaultStatusId.Value);

            if (projectDefault is not null)
            {
                return projectDefault;
            }
        }

        var newStatus = taskStatuses.FirstOrDefault(status => status.Category == StatusCategory.Todo);

        return newStatus ?? taskStatuses.First();
    }
}
