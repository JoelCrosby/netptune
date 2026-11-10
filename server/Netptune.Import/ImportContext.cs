using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.UnitOfWork;
using Netptune.Transfer.Mapping;
using Netptune.Transfer.Services;

namespace Netptune.Import;

public sealed class ImportContext
{
    private const int DefaultSprintDays = 14;
    private const int MaxSprintNameLength = 256;

    private readonly Dictionary<int, double> SortOrders;
    private readonly Dictionary<string, ProjectTask> ExistingByExternalId;
    private readonly Dictionary<string, ProjectTask> ExistingBySystemId;
    private readonly List<BoardGroup> Groups;
    private readonly List<Status> TaskStatuses;
    private readonly Dictionary<int, Sprint> SprintsById;
    private readonly HashSet<int> HistoricalSprintIds = [];

    private int ScopeIdCursor;
    private int ScopeIdLimit;

    public ImportContext(
        Project project,
        Board board,
        ImportVocabulary vocabulary,
        IReadOnlyList<Status> statuses,
        List<BoardGroup> groups,
        IReadOnlyList<Sprint> sprints,
        IReadOnlyList<ProjectTask> existingTasks)
    {
        Project = project;
        Board = board;
        Vocabulary = vocabulary;
        Groups = groups;
        TaskStatuses = statuses
            .Where(status => status.EntityType == EntityType.Task)
            .OrderBy(status => status.SortOrder)
            .ToList();
        SprintsById = sprints.ToDictionary(sprint => sprint.Id);

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
        return FindExisting(row.SourceId);
    }

    public ProjectTask? FindExisting(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var sourceId = reference.Trim().ToLowerInvariant();

        return ExistingByExternalId.GetValueOrDefault(sourceId) ?? ExistingBySystemId.GetValueOrDefault(sourceId);
    }

    public Status? FindStatusInCategory(string? categoryName)
    {
        var category = ImportStatusCategories.Parse(categoryName);

        if (category is null)
        {
            return null;
        }

        if (DefaultStatus.Category == category)
        {
            return DefaultStatus;
        }

        return TaskStatuses.FirstOrDefault(status => status.Category == category);
    }

    public Sprint? FindSprint(string? name)
    {
        var sprintName = SprintName(name);

        return Vocabulary.FindSprint(sprintName);
    }

    public Sprint? FindAssignableSprint(string? name)
    {
        var sprint = FindSprint(name);
        var isAssignable = sprint is not null && AcceptsTasks(sprint);

        return isAssignable ? sprint : null;
    }

    public Sprint? FindSprintById(int? sprintId)
    {
        if (sprintId is null)
        {
            return null;
        }

        return SprintsById.GetValueOrDefault(sprintId.Value);
    }

    // Closed sprints keep their membership, the same as adding to or removing from one is refused
    // elsewhere. The exception is a sprint this import created closed to carry history.
    public bool AcceptsTasks(Sprint sprint)
    {
        var isOpen = sprint.Status is SprintStatus.Planning or SprintStatus.Active;
        var isImportedHistory = HistoricalSprintIds.Contains(sprint.Id);

        return isOpen || isImportedHistory;
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

    // Dates come from the first row that names the sprint. Only a sprint whose last day has passed is
    // created complete; one still running stays in planning, so the import never starts a sprint.
    public async Task<Sprint> CreateSprint(
        INetptuneUnitOfWork unitOfWork,
        ResolvedTaskRow row,
        ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        var name = SprintName(row.SprintValue)!;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = row.SprintStartDate ?? row.SprintEndDate?.AddDays(-DefaultSprintDays) ?? today;
        var plannedEnd = row.SprintEndDate ?? start.AddDays(DefaultSprintDays);
        var end = plannedEnd < start ? start : plannedEnd;
        var startDate = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDate = end.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var hasEnded = end < today;

        var sprint = new Sprint
        {
            Name = name,
            Identifier = name.ToUrlSlug(appendUniqueId: true, maxLength: Sprint.IdentifierMaxLength),
            StartDate = startDate,
            EndDate = endDate,
            Status = hasEnded ? SprintStatus.Completed : SprintStatus.Planning,
            StartedAt = hasEnded ? startDate : null,
            CompletedAt = hasEnded ? endDate : null,
            ProjectId = Project.Id,
            WorkspaceId = request.WorkspaceId,
            OwnerId = request.UserId,
            CreatedByUserId = request.UserId,
        };

        await unitOfWork.Sprints.AddAsync(sprint, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        Vocabulary.Register(sprint);
        SprintsById[sprint.Id] = sprint;

        if (hasEnded)
        {
            HistoricalSprintIds.Add(sprint.Id);
        }

        return sprint;
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

    // Names are stored cut to the column's length, so lookups cut the same way or a long name would
    // never match the sprint it created and every row would make another.
    private static string? SprintName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var name = value.Trim();
        var fitsColumn = name.Length <= MaxSprintNameLength;

        return fitsColumn ? name : name[..MaxSprintNameLength].TrimEnd();
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
