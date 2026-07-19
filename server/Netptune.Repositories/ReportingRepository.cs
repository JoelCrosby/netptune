using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Repositories;
using Netptune.Core.Services.Reporting;
using Netptune.Entities.Contexts;

namespace Netptune.Repositories;

public sealed class ReportingRepository : IReportingRepository
{
    private sealed record ReportingEventScope(ReportingScope Scope, int? ProjectId);

    private sealed record ReportingEventRange(DateTime From, DateTime To);

    private readonly DataContext Context;

    public ReportingRepository(DataContext context)
    {
        Context = context;
    }

    public async Task<FlowReport> GetFlow(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken = default)
    {
        var workspaceId = scope.WorkspaceId;

        var (from, to) = ResolveRange(filter);
        var eventScope = new ReportingEventScope(scope, filter.ProjectId);

        var coverageStart = await CoverageStart(eventScope, cancellationToken);
        var eventRange = new ReportingEventRange(coverageStart ?? from, to);

        var events = await QueryEvents(eventScope, eventRange, cancellationToken);

        var facts = new List<FlowFact>();

        foreach (var record in events.Where(record => record.SubjectType == "task"))
        {
            if (record.SubjectId is null)
            {
                continue;
            }

            var field = ReadString(record.Payload, "field");
            var isTaskCreation = record.EventKey == EventKeys.EntityCreated;
            var isStatusTransition = record.EventKey == EventKeys.EntityFieldTransitioned &&
                field == "status";

            if (isTaskCreation)
            {
                facts.Add(new FlowFact(record.SubjectId, record.OccurredAt, FlowFactType.Created));
                var wasCreatedInDone = ReadString(record.Payload, "statusCategory") == nameof(StatusCategory.Done);

                if (wasCreatedInDone)
                {
                    facts.Add(new FlowFact(record.SubjectId, record.OccurredAt, FlowFactType.Done));
                }
            }
            else if (isStatusTransition)
            {
                var category = ReadString(record.Payload, "newCategory");

                if (category == nameof(StatusCategory.Active))
                {
                    facts.Add(new FlowFact(record.SubjectId, record.OccurredAt, FlowFactType.Active));
                }

                if (category == nameof(StatusCategory.Done))
                {
                    facts.Add(new FlowFact(record.SubjectId, record.OccurredAt, FlowFactType.Done));
                }
            }
        }

        var timeZone = ResolveTimeZone(filter.TimeZone);

        var calculation = new FlowCalculationInput
        {
            Facts = facts,
            TimeZone = timeZone,
            From = from,
            To = to,
            CoverageStart = coverageStart,
            Grouping = filter.Grouping,
        };
        var report = FlowMetricCalculator.Calculate(calculation);
        var visibleProjectIds = scope.ProjectIds.ToList();
        var currentOpenTaskCount = await Context.ProjectTasks
            .AsNoTracking()
            .Where(task => task.WorkspaceId == workspaceId && !task.IsDeleted)
            .Where(task => task.ProjectId != null && visibleProjectIds.Contains(task.ProjectId.Value))
            .Where(task => filter.ProjectId == null || task.ProjectId == filter.ProjectId)
            .Where(task => task.Status.Category != StatusCategory.Done && task.Status.Category != StatusCategory.Inactive)
            .CountAsync(cancellationToken);

        return report with { CurrentOpenTaskCount = currentOpenTaskCount };
    }

    public async Task<WorkloadReport> GetWorkload(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken = default)
    {
        var workspaceId = scope.WorkspaceId;
        var visibleProjectIds = scope.ProjectIds.ToList();

        var tasks = await Context.ProjectTasks
            .AsNoTracking()
            .Where(task => task.WorkspaceId == workspaceId && !task.IsDeleted)
            .Where(task => task.ProjectId != null && visibleProjectIds.Contains(task.ProjectId.Value))
            .Where(task => filter.ProjectId == null || task.ProjectId == filter.ProjectId)
            .Where(task => task.Status.Category != StatusCategory.Done && task.Status.Category != StatusCategory.Inactive)
            .Select(task => new
            {
                task.Id,
                task.EstimateType,
                task.EstimateValue,
                Assignees = task.ProjectTaskAppUsers.Select(link => new { link.UserId, link.User.DisplayName }).ToList(),
            })
            .ToListAsync(cancellationToken);

        var missing = tasks.Count(task => filter.Unit != ReportingUnit.Tasks && Value(task.EstimateType, task.EstimateValue, filter.Unit) is null);

        var rows = tasks
            .SelectMany(task => task.Assignees.Count == 0
                ? new[] { new { UserId = (string?)null, DisplayName = "Unassigned", Task = task } }
                : task.Assignees.Select(user => new { UserId = (string?)user.UserId, user.DisplayName, Task = task }))
            .GroupBy(item => new { item.UserId, item.DisplayName })
            .Select(group => new WorkloadRow
            {
                UserId = group.Key.UserId,
                DisplayName = group.Key.DisplayName,
                TaskCount = group.Count(),
                Value = group.Sum(item => Value(item.Task.EstimateType, item.Task.EstimateValue, filter.Unit) ?? 0),
            })
            .OrderByDescending(row => row.Value)
            .ThenBy(row => row.DisplayName)
            .ToList();

        return new WorkloadReport
        {
            Rows = rows,
            UniqueTaskCount = tasks.Count,
            UnassignedTaskCount = tasks.Count(task => task.Assignees.Count == 0),
            MultiAssignedTaskCount = tasks.Count(task => task.Assignees.Count > 1),
            MissingEstimateCount = missing,
            Unit = filter.Unit,
        };
    }

    public async Task<SprintBurndownReport?> GetBurndown(
        ReportingScope scope,
        SprintBurndownFilter filter,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = scope.WorkspaceId;
        var sprint = await Context.Sprints
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == filter.SprintId && item.WorkspaceId == workspaceId && !item.IsDeleted, cancellationToken);
        var sprintExists = sprint is not null;
        var projectIsVisible = sprintExists && scope.CanAccessProject(sprint!.ProjectId);
        var canReportSprint = sprintExists && projectIsVisible;

        if (!canReportSprint)
        {
            return null;
        }

        var reportableSprint = sprint!;

        if (reportableSprint.StartedAt is null)
        {
            throw new InvalidReportingFilterException("This sprint has no recorded start baseline.");
        }

        var end = reportableSprint.CompletedAt ?? DateTime.UtcNow;
        var zone = ResolveTimeZone(filter.TimeZone);

        var sprintKey = filter.SprintId.ToString();

        // Split the subject/reference match into a UNION rather than an OR: a single OR forces the
        // planner to scan every event in the workspace, while each branch here uses its own index
        // (subject sequence / reference reverse-lookup). The replay below reads only payload/subject
        // fields, so no reference include is needed.
        var bySubject = Context.EventRecords.AsNoTracking()
            .Where(record => record.WorkspaceId == workspaceId &&
                record.OccurredAt >= reportableSprint.StartedAt && record.OccurredAt <= end &&
                record.SubjectType == "sprint" && record.SubjectId == sprintKey);

        var byReference = Context.EventRecords.AsNoTracking()
            .Where(record => record.WorkspaceId == workspaceId &&
                record.OccurredAt >= reportableSprint.StartedAt && record.OccurredAt <= end &&
                record.References.Any(reference => reference.EntityType == "sprint" && reference.EntityId == sprintKey));

        var records = await bySubject.Union(byReference)
            .OrderBy(record => record.OccurredAt).ThenBy(record => record.Id)
            .ToListAsync(cancellationToken);

        var start = records.FirstOrDefault(record => record.SubjectType == "sprint" && ReadString(record.Payload, "state") == "started")
            ?? throw new InvalidReportingFilterException("This sprint predates reporting coverage.");

        var members = ReadCommitment(start.Payload);

        var committed = members.Count;
        var added = 0;
        var removed = 0;
        var startingCommitment = members.Values.Sum(member => Value(member.Unit, member.Value, filter.Unit) ?? 0);

        var points = new List<BurndownPoint>();
        var processed = new HashSet<long>();

        var localStart = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(reportableSprint.StartedAt.Value, DateTimeKind.Utc),
            zone));
        var localEnd = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(end, DateTimeKind.Utc), zone));
        var plannedEnd = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(reportableSprint.EndDate, DateTimeKind.Utc),
            zone));
        var dates = EachDate(localStart, localEnd);

        foreach (var date in dates)
        {
            var boundary = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Unspecified), zone);

            foreach (var record in records.Where(record => record.OccurredAt <= boundary && !processed.Contains(record.Id)))
            {
                processed.Add(record.Id);
                var isScopeMembershipChange = record.EventKey == EventKeys.ScopeMemberChanged;
                var isMemberAttributeChange = record.EventKey == EventKeys.ScopeMemberAttributeChanged;
                var isTaskStatusChange = record.SubjectType == "task" &&
                    ReadString(record.Payload, "field") == "status";

                if (isScopeMembershipChange)
                {
                    var taskId = ReadString(record.Payload, "memberId");

                    if (taskId is null)
                    {
                        continue;
                    }

                    var change = ReadString(record.Payload, "change");
                    var memberWasAdded = change == "added";
                    var memberWasRemoved = change == "removed";

                    if (memberWasAdded)
                    {
                        members[taskId] = ReadMember(record.Payload);
                        added++;
                    }
                    else if (memberWasRemoved)
                    {
                        members.Remove(taskId);
                        removed++;
                    }
                }
                else if (isMemberAttributeChange)
                {
                    ApplyMemberAttributeChange(record, members);
                }
                else if (isTaskStatusChange)
                {
                    ApplyTaskStatusChange(record, members);
                }
            }

            var total = members.Values.Sum(member => Value(member.Unit, member.Value, filter.Unit) ?? 0);
            var remaining = members.Values.Where(member => !member.Done).Sum(member => Value(member.Unit, member.Value, filter.Unit) ?? 0);
            var elapsed = Math.Max(0, date.DayNumber - localStart.DayNumber);
            var duration = Math.Max(1, plannedEnd.DayNumber - localStart.DayNumber);

            points.Add(new BurndownPoint
            {
                Date = date,
                Remaining = remaining,
                TotalScope = total,
                Ideal = startingCommitment * Math.Max(0, duration - elapsed) / duration,
            });
        }

        var completedCount = members.Values.Count(member => member.Done);
        var completionPercentage = members.Count == 0
            ? 0
            : decimal.Round((decimal)completedCount / members.Count * 100, 1);
        var missing = members.Values.Count(
            member => filter.Unit != ReportingUnit.Tasks && Value(member.Unit, member.Value, filter.Unit) is null);

        return new SprintBurndownReport
        {
            SprintId = reportableSprint.Id,
            SprintName = reportableSprint.Name,
            Unit = filter.Unit,
            Points = points,
            CommittedCount = committed,
            AddedCount = added,
            RemovedCount = removed,
            MissingEstimateCount = missing,
            CompletedCount = completedCount,
            CompletionPercentage = completionPercentage,
            Coverage = new ReportingCoverage(start.OccurredAt, false),
        };
    }

    public async Task<VelocityReport> GetVelocity(
        ReportingScope scope,
        VelocityFilter filter,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = scope.WorkspaceId;
        var take = Math.Clamp(filter.Take, 1, 20);

        var sprints = await Context.Sprints.AsNoTracking()
            .Where(sprint => sprint.WorkspaceId == workspaceId && sprint.ProjectId == filter.ProjectId && sprint.CompletedAt != null && !sprint.IsDeleted)
            .OrderByDescending(sprint => sprint.CompletedAt).Take(take)
            .ToListAsync(cancellationToken);

        var results = new List<VelocityPoint>();
        var excluded = 0;

        foreach (var sprint in sprints.OrderBy(item => item.CompletedAt))
        {
            var sprintId = sprint.Id.ToString();
            var lifecycle = await Context.EventRecords
                .AsNoTracking()
                .Include(record => record.References)
                .Where(record => record.WorkspaceId == workspaceId)
                .Where(record => (record.SubjectType == "sprint" && record.SubjectId == sprintId) ||
                    record.References.Any(reference => reference.EntityType == "sprint" && reference.EntityId == sprintId))
                .OrderBy(record => record.OccurredAt)
                .ThenBy(record => record.Id)
                .ToListAsync(cancellationToken);

            var started = lifecycle.FirstOrDefault(record => ReadString(record.Payload, "state") == "started");
            var completed = lifecycle.LastOrDefault(record => ReadString(record.Payload, "state") == "completed");
            var hasCompleteLifecycle = started is not null && completed is not null;

            if (!hasCompleteLifecycle)
            {
                excluded++;
                continue;
            }

            var sprintStartedAt = started!.OccurredAt;
            var sprintCompletedAt = completed!.OccurredAt;
            var commitment = ReadCommitment(started.Payload);
            var completion = ReadCommitment(completed.Payload);
            var completedTaskIds = lifecycle
                .Where(record => IsTaskCompletionWithinSprint(record, sprintStartedAt, sprintCompletedAt))
                .Select(record => record.SubjectId!)
                .ToHashSet();
            var finalCompletedMembers = completion
                .Where(pair => completedTaskIds.Contains(pair.Key))
                .Select(pair => pair.Value)
                .ToList();
            var committedValue = commitment.Values.Sum(member => Value(member.Unit, member.Value, filter.Unit) ?? 0);
            var completedValue = finalCompletedMembers.Sum(member => Value(member.Unit, member.Value, filter.Unit) ?? 0);
            var missingEstimateCount = filter.Unit == ReportingUnit.Tasks
                ? 0
                : finalCompletedMembers.Count(member => member.Unit is null || member.Value is null);
            var expectedUnit = UnitEstimateType(filter.Unit);
            var differentUnitEstimateCount = expectedUnit is null
                ? 0
                : finalCompletedMembers.Count(member => member.Unit is not null && member.Unit != expectedUnit);

            results.Add(new VelocityPoint
            {
                SprintId = sprint.Id,
                SprintName = sprint.Name,
                CompletedAt = sprint.CompletedAt!.Value,
                Committed = committedValue,
                Completed = completedValue,
                MissingEstimateCount = missingEstimateCount,
                DifferentUnitEstimateCount = differentUnitEstimateCount,
            });
        }

        var eventScope = new ReportingEventScope(scope, filter.ProjectId);
        var coverage = await CoverageStart(eventScope, cancellationToken);

        return new VelocityReport
        {
            Sprints = results,
            Unit = filter.Unit,
            ExcludedSprintCount = excluded,
            Coverage = new ReportingCoverage(coverage, excluded > 0),
        };
    }

    private async Task<List<EventRecord>> QueryEvents(
        ReportingEventScope eventScope,
        ReportingEventRange range,
        CancellationToken token)
    {
        var visibleProjectIds = eventScope.Scope.ProjectIds.Select(id => id.ToString()).ToList();

        var query = Context.EventRecords
            .AsNoTracking()
            .Include(record => record.References)
            .Where(record => record.WorkspaceId == eventScope.Scope.WorkspaceId &&
                record.OccurredAt >= range.From &&
                record.OccurredAt <= range.To);

        query = eventScope.ProjectId.HasValue
            ? query.Where(record => record.References.Any(reference => reference.EntityType == "project" && reference.EntityId == eventScope.ProjectId.Value.ToString()))
            : query.Where(record => record.References.Any(reference => reference.EntityType == "project" && visibleProjectIds.Contains(reference.EntityId)));

        var events = await query.OrderBy(record => record.OccurredAt).ThenBy(record => record.Id).ToListAsync(token);

        return events;
    }

    private async Task<DateTime?> CoverageStart(
        ReportingEventScope eventScope,
        CancellationToken token)
    {
        var visibleProjectIds = eventScope.Scope.ProjectIds.Select(id => id.ToString()).ToList();
        var query = Context.EventRecords
            .AsNoTracking()
            .Where(record => record.WorkspaceId == eventScope.Scope.WorkspaceId && record.RetentionClass == EventRetentionClasses.Permanent);

        query = eventScope.ProjectId.HasValue
            ? query.Where(record => record.References.Any(reference => reference.EntityType == "project" && reference.EntityId == eventScope.ProjectId.Value.ToString()))
            : query.Where(record => record.References.Any(reference => reference.EntityType == "project" && visibleProjectIds.Contains(reference.EntityId)));

        var coverageStart = await query.MinAsync(record => (DateTime?)record.OccurredAt, token);

        return coverageStart;
    }

    private static void ApplyMemberAttributeChange(
        EventRecord record,
        Dictionary<string, Member> members)
    {
        var taskId = ReadString(record.Payload, "memberId");
        var isEstimateChange = ReadString(record.Payload, "field") == "estimate";
        var memberExists = members.TryGetValue(taskId ?? string.Empty, out var member);
        var canApplyEstimateChange = taskId is not null && isEstimateChange && memberExists;

        if (!canApplyEstimateChange)
        {
            return;
        }

        var hasEstimateValue = decimal.TryParse(
            ReadString(record.Payload, "newNumericValue"),
            out var estimate);

        members[taskId!] = member! with
        {
            Unit = ReadString(record.Payload, "newUnit"),
            Value = hasEstimateValue ? estimate : null,
        };
    }

    private static void ApplyTaskStatusChange(
        EventRecord record,
        Dictionary<string, Member> members)
    {
        var taskId = record.SubjectId;
        var memberExists = members.TryGetValue(taskId ?? string.Empty, out var member);
        var canApplyStatusChange = taskId is not null && memberExists;

        if (!canApplyStatusChange)
        {
            return;
        }

        var movedToDone = ReadString(record.Payload, "newCategory") == nameof(StatusCategory.Done);

        members[taskId!] = member! with { Done = movedToDone };
    }

    private static bool IsTaskCompletionWithinSprint(
        EventRecord record,
        DateTime sprintStartedAt,
        DateTime sprintCompletedAt)
    {
        var occurredAfterStart = record.OccurredAt >= sprintStartedAt;
        var occurredBeforeCompletion = record.OccurredAt <= sprintCompletedAt;
        var occurredDuringSprint = occurredAfterStart && occurredBeforeCompletion;
        var isIdentifiableTask = record.SubjectType == "task" && record.SubjectId is not null;
        var isStatusTransition = record.EventKey == EventKeys.EntityFieldTransitioned &&
            ReadString(record.Payload, "field") == "status";
        var transitionedToDone = isStatusTransition &&
            ReadString(record.Payload, "newCategory") == nameof(StatusCategory.Done);
        var isTaskCreation = record.EventKey == EventKeys.EntityCreated;
        var wasCreatedInDone = isTaskCreation &&
            ReadString(record.Payload, "statusCategory") == nameof(StatusCategory.Done);
        var isCompletion = transitionedToDone || wasCreatedInDone;

        return occurredDuringSprint && isIdentifiableTask && isCompletion;
    }

    private static (DateTime From, DateTime To) ResolveRange(ReportingFilter filter)
    {
        var to = filter.To ?? DateTime.UtcNow;
        var from = filter.From ?? to.AddDays(-90);

        if (from > to)
        {
            throw new InvalidReportingFilterException("From must be before To.");
        }

        if ((to - from).TotalDays > 366)
        {
            throw new InvalidReportingFilterException("Flow reports are limited to 366 days.");
        }

        return (from, to);
    }

    private static TimeZoneInfo ResolveTimeZone(string value)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(value);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    private static string? ReadString(JsonDocument document, string property)
    {
        var propertyExists = document.RootElement.TryGetProperty(property, out var value);
        var propertyHasValue = propertyExists && value.ValueKind != JsonValueKind.Null;

        return propertyHasValue ? value.ToString() : null;
    }


    private static decimal? Value(EstimateType? type, decimal? value, ReportingUnit unit) => unit switch
    {
        ReportingUnit.Tasks => 1,
        ReportingUnit.StoryPoints when type == EstimateType.StoryPoints => value,
        ReportingUnit.Hours when type == EstimateType.Hours => value,
        _ => null,
    };

    private static decimal? Value(string? type, decimal? value, ReportingUnit unit)
    {
        var hasEstimateType = Enum.TryParse<EstimateType>(type, out var parsed);

        if (hasEstimateType)
        {
            return Value(parsed, value, unit);
        }

        var usesTaskCount = unit == ReportingUnit.Tasks;

        return usesTaskCount ? 1 : null;
    }

    private static string? UnitEstimateType(ReportingUnit unit) => unit switch
    {
        ReportingUnit.StoryPoints => nameof(EstimateType.StoryPoints),
        ReportingUnit.Hours => nameof(EstimateType.Hours),
        _ => null,
    };


    private static Dictionary<string, Member> ReadCommitment(JsonDocument document)
    {
        var commitmentExists = document.RootElement.TryGetProperty("commitment", out var array);
        var commitmentIsArray = commitmentExists && array.ValueKind == JsonValueKind.Array;

        if (!commitmentIsArray)
        {
            return [];
        }

        var members = new Dictionary<string, Member>();

        foreach (var item in array.EnumerateArray())
        {
            var hasEstimateValue = item.TryGetProperty("estimateValue", out var value) &&
                value.ValueKind == JsonValueKind.Number;
            var isDone = item.TryGetProperty("statusCategory", out var status) &&
                status.ToString() == nameof(StatusCategory.Done);

            members[item.GetProperty("taskId").ToString()] = new Member(
                item.TryGetProperty("estimateType", out var unit) ? unit.ToString() : null,
                hasEstimateValue ? value.GetDecimal() : null,
                isDone);
        }

        return members;
    }

    private static Member ReadMember(JsonDocument payload) => new(
        ReadString(payload, "estimateType"),
        decimal.TryParse(ReadString(payload, "estimateValue"), out var value) ? value : null,
        ReadString(payload, "statusCategory") == nameof(StatusCategory.Done));

    private static IEnumerable<DateOnly> EachDate(DateOnly from, DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            yield return date;
        }
    }

    private sealed record Member(string? Unit, decimal? Value, bool Done);
}
