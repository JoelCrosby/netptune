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
    private readonly DataContext Context;

    public ReportingRepository(DataContext context)
    {
        Context = context;
    }

    public async Task<FlowReport> GetFlow(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken = default)
    {
        var workspaceId = scope.WorkspaceId;

        var (from, to) = ResolveRange(filter);

        var coverageStart = await CoverageStart(
            workspaceId,
            scope.ProjectIds,
            filter.ProjectId,
            cancellationToken);

        var events = await QueryEvents(
            workspaceId,
            scope.ProjectIds,
            filter.ProjectId,
            coverageStart ?? from,
            to,
            cancellationToken);

        var facts = new List<FlowFact>();

        foreach (var record in events.Where(record => record.SubjectType == "task"))
        {

            if (record.SubjectId is null)
            {
                continue;
            }

            if (record.EventKey == EventKeys.EntityCreated)
            {
                facts.Add(new FlowFact(record.SubjectId, record.OccurredAt, FlowFactType.Created));

                if (ReadString(record.Payload, "statusCategory") == nameof(StatusCategory.Done))
                {
                    facts.Add(new FlowFact(record.SubjectId, record.OccurredAt, FlowFactType.Done));
                }
            }
            else if (record.EventKey == EventKeys.EntityFieldTransitioned && ReadString(record.Payload, "field") == "status")
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

        return FlowMetricCalculator.Calculate(
            facts,
            timeZone,
            from,
            to,
            coverageStart);
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

    public async Task<SprintBurndownReport?> GetBurndown(ReportingScope scope, int sprintId, ReportingUnit unit, string timeZone, CancellationToken cancellationToken = default)
    {
        var workspaceId = scope.WorkspaceId;
        var sprint = await Context.Sprints
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == sprintId && item.WorkspaceId == workspaceId && !item.IsDeleted, cancellationToken);

        if (sprint is null || !scope.CanAccessProject(sprint.ProjectId))
        {
            return null;
        }

        if (sprint.StartedAt is null)
        {
            throw new InvalidReportingFilterException("This sprint has no recorded start baseline.");
        }

        var end = sprint.CompletedAt ?? DateTime.UtcNow;
        var zone = ResolveTimeZone(timeZone);

        var records = await Context.EventRecords.AsNoTracking()
            .Include(record => record.References)
            .Where(record => record.WorkspaceId == workspaceId && record.OccurredAt >= sprint.StartedAt && record.OccurredAt <= end)
            .Where(record => (record.SubjectType == "sprint" && record.SubjectId == sprintId.ToString()) ||
                record.References.Any(reference => reference.EntityType == "sprint" && reference.EntityId == sprintId.ToString()))
            .OrderBy(record => record.OccurredAt).ThenBy(record => record.Id)
            .ToListAsync(cancellationToken);

        var start = records.FirstOrDefault(record => record.SubjectType == "sprint" && ReadString(record.Payload, "state") == "started")
            ?? throw new InvalidReportingFilterException("This sprint predates reporting coverage.");

        var members = ReadCommitment(start.Payload);

        var committed = members.Count;
        var added = 0;
        var removed = 0;
        var missing = members.Values.Count(member => unit != ReportingUnit.Tasks && Value(member.Unit, member.Value, unit) is null);

        var points = new List<BurndownPoint>();
        var processed = new HashSet<long>();

        var localStart = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(sprint.StartedAt.Value, DateTimeKind.Utc), zone));
        var localEnd = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(end, DateTimeKind.Utc), zone));
        var dates = EachDate(localStart, localEnd);

        foreach (var date in dates)
        {
            var boundary = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Unspecified), zone);

            foreach (var record in records.Where(record => record.OccurredAt <= boundary && !processed.Contains(record.Id)))
            {
                processed.Add(record.Id);

                if (record.EventKey == EventKeys.ScopeMemberChanged)
                {
                    var taskId = ReadString(record.Payload, "memberId");

                    if (taskId is null)
                    {
                        continue;
                    }

                    if (ReadString(record.Payload, "change") == "added")
                    {
                        members[taskId] = ReadMember(record.Payload);
                        added++;
                    }
                    else if (ReadString(record.Payload, "change") == "removed")
                    {
                        members.Remove(taskId);
                        removed++;
                    }
                }
                else if (record.EventKey == EventKeys.ScopeMemberAttributeChanged)
                {
                    var taskId = ReadString(record.Payload, "memberId");

                    if (taskId is not null && members.TryGetValue(taskId, out var member) && ReadString(record.Payload, "field") == "estimate")
                    {
                        members[taskId] = member with
                        {
                            Unit = ReadString(record.Payload, "newUnit"),
                            Value = decimal.TryParse(ReadString(record.Payload, "newNumericValue"), out var estimate) ? estimate : null,
                        };
                    }
                }
                else if (record.SubjectType == "task" && ReadString(record.Payload, "field") == "status" && record.SubjectId is not null && members.TryGetValue(record.SubjectId, out var member))
                {
                    members[record.SubjectId] = member with { Done = ReadString(record.Payload, "newCategory") == nameof(StatusCategory.Done) };
                }
            }

            var total = members.Values.Sum(member => Value(member.Unit, member.Value, unit) ?? 0);
            var remaining = members.Values.Where(member => !member.Done).Sum(member => Value(member.Unit, member.Value, unit) ?? 0);
            var elapsed = Math.Max(0, date.DayNumber - localStart.DayNumber);
            var duration = Math.Max(1, localEnd.DayNumber - localStart.DayNumber);

            points.Add(new BurndownPoint
            {
                Date = date,
                Remaining = remaining,
                TotalScope = total,
                Ideal = total * Math.Max(0, duration - elapsed) / duration,
            });
        }

        return new SprintBurndownReport
        {
            SprintId = sprint.Id,
            SprintName = sprint.Name,
            Unit = unit,
            Points = points,
            CommittedCount = committed,
            AddedCount = added,
            RemovedCount = removed,
            MissingEstimateCount = missing,
            Coverage = new ReportingCoverage(start.OccurredAt, false),
        };
    }

    public async Task<VelocityReport> GetVelocity(ReportingScope scope, int projectId, ReportingUnit unit, int take, CancellationToken cancellationToken = default)
    {
        var workspaceId = scope.WorkspaceId;
        take = Math.Clamp(take, 1, 20);

        var sprints = await Context.Sprints.AsNoTracking()
            .Where(sprint => sprint.WorkspaceId == workspaceId && sprint.ProjectId == projectId && sprint.CompletedAt != null && !sprint.IsDeleted)
            .OrderByDescending(sprint => sprint.CompletedAt).Take(take)
            .ToListAsync(cancellationToken);

        var results = new List<VelocityPoint>();
        var excluded = 0;

        foreach (var sprint in sprints.OrderBy(item => item.CompletedAt))
        {
            var lifecycle = await Context.EventRecords
                .AsNoTracking()
                .Where(record => record.WorkspaceId == workspaceId && record.SubjectType == "sprint" && record.SubjectId == sprint.Id.ToString())
                .OrderBy(record => record.OccurredAt)
                .ThenBy(record => record.Id)
                .ToListAsync(cancellationToken);

            var started = lifecycle.FirstOrDefault(record => ReadString(record.Payload, "state") == "started");
            var completed = lifecycle.LastOrDefault(record => ReadString(record.Payload, "state") == "completed");

            if (started is null || completed is null)
            {
                excluded++;
                continue;
            }

            var commitment = ReadCommitment(started.Payload).Values;
            var completion = ReadCommitment(completed.Payload).Values;
            var committedValue = commitment.Sum(member => Value(member.Unit, member.Value, unit) ?? 0);
            var completedValue = completion.Where(member => member.Done).Sum(member => Value(member.Unit, member.Value, unit) ?? 0);

            results.Add(new VelocityPoint
            {
                SprintId = sprint.Id,
                SprintName = sprint.Name,
                CompletedAt = sprint.CompletedAt!.Value,
                Committed = committedValue,
                Completed = completedValue,
            });
        }

        var coverage = await CoverageStart(
            workspaceId,
            scope.ProjectIds,
            projectId,
            cancellationToken);

        return new VelocityReport
        {
            Sprints = results,
            Unit = unit,
            ExcludedSprintCount = excluded,
            Coverage = new ReportingCoverage(coverage, excluded > 0),
        };
    }

    private async Task<List<EventRecord>> QueryEvents(int workspaceId, IReadOnlySet<int> permittedProjects, int? projectId, DateTime from, DateTime to, CancellationToken token)
    {
        var visibleProjectIds = permittedProjects.Select(id => id.ToString()).ToList();

        var query = Context.EventRecords
            .AsNoTracking()
            .Include(record => record.References)
            .Where(record => record.WorkspaceId == workspaceId && record.OccurredAt >= from && record.OccurredAt <= to);

        query = projectId.HasValue
            ? query.Where(record => record.References.Any(reference => reference.EntityType == "project" && reference.EntityId == projectId.Value.ToString()))
            : query.Where(record => record.References.Any(reference => reference.EntityType == "project" && visibleProjectIds.Contains(reference.EntityId)));

        var events = await query.OrderBy(record => record.OccurredAt).ThenBy(record => record.Id).ToListAsync(token);

        return events;
    }

    private async Task<DateTime?> CoverageStart(int workspaceId, IReadOnlySet<int> permittedProjects, int? projectId, CancellationToken token)
    {
        var visibleProjectIds = permittedProjects.Select(id => id.ToString()).ToList();
        var query = Context.EventRecords
            .AsNoTracking()
            .Where(record => record.WorkspaceId == workspaceId && record.RetentionClass == EventRetentionClasses.Permanent);

        query = projectId.HasValue
            ? query.Where(record => record.References.Any(reference => reference.EntityType == "project" && reference.EntityId == projectId.Value.ToString()))
            : query.Where(record => record.References.Any(reference => reference.EntityType == "project" && visibleProjectIds.Contains(reference.EntityId)));

        var coverageStart = await query.MinAsync(record => (DateTime?)record.OccurredAt, token);

        return coverageStart;
    }

    private static (DateTime From, DateTime To) ResolveRange(ReportingFilter filter)
    {
        var to = filter.To ?? DateTime.UtcNow;
        var from = filter.From ?? to.AddDays(-30);

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
            throw new InvalidReportingFilterException("The reporting time zone is invalid.");
        }
    }

    private static string? ReadString(JsonDocument document, string property)
    {
        return document.RootElement.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null ? value.ToString() : null;
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
        return Enum.TryParse<EstimateType>(type, out var parsed) ? Value(parsed, value, unit) : unit == ReportingUnit.Tasks ? 1 : null;
    }


    private static Dictionary<string, Member> ReadCommitment(JsonDocument document)
    {

        if (!document.RootElement.TryGetProperty("commitment", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var members = new Dictionary<string, Member>();

        foreach (var item in array.EnumerateArray())
        {
            members[item.GetProperty("taskId").ToString()] = new Member(
                item.TryGetProperty("estimateType", out var unit) ? unit.ToString() : null,
                item.TryGetProperty("estimateValue", out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDecimal() : null,
                item.TryGetProperty("statusCategory", out var status) && status.ToString() == nameof(StatusCategory.Done));
        }

        return members;
    }

    private static Member ReadMember(JsonDocument payload) => new(
        ReadString(payload, "estimateType"),
        decimal.TryParse(ReadString(payload, "estimateValue"), out var value) ? value : null,
        ReadString(payload, "statusCategory") == nameof(StatusCategory.Done));

    private static IEnumerable<DateOnly> EachDate(DateOnly from, DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1)) yield return date;
    }

    private sealed record Member(string? Unit, decimal? Value, bool Done);
}
