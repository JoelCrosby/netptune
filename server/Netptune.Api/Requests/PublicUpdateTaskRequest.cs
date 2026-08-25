using System.Text.Json.Serialization;

using Netptune.Core.Enums;
using Netptune.Core.Requests;

namespace Netptune.Api.Requests;

public sealed record PublicUpdateTaskRequest
{
    private DateOnly? startDate;

    private DateOnly? dueDate;

    public string? Name { get; init; }

    public string? Description { get; init; }

    public int? StatusId { get; init; }

    public double? SortOrder { get; init; }

    public string? OwnerId { get; init; }

    public List<string>? AssigneeIds { get; init; }

    public List<string>? Tags { get; init; }

    public TaskPriority? Priority { get; init; }

    public EstimateType? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }

    public int? BoardGroupId { get; init; }

    public DateOnly? StartDate
    {
        get => startDate;
        init
        {
            startDate = value;
            StartDateSpecified = true;
        }
    }

    [JsonIgnore]
    public bool StartDateSpecified { get; private set; }

    public DateOnly? DueDate
    {
        get => dueDate;
        init
        {
            dueDate = value;
            DueDateSpecified = true;
        }
    }

    [JsonIgnore]
    public bool DueDateSpecified { get; private set; }

    public UpdateProjectTaskRequest ToRequest(int id)
    {
        var request = new UpdateProjectTaskRequest
        {
            Id = id,
            Name = Name,
            Description = Description,
            StatusId = StatusId,
            SortOrder = SortOrder,
            OwnerId = OwnerId,
            AssigneeIds = AssigneeIds,
            Tags = Tags,
            Priority = Priority,
            EstimateType = EstimateType,
            EstimateValue = EstimateValue,
        };

        if (StartDateSpecified)
        {
            request.StartDate = StartDate;
        }

        if (DueDateSpecified)
        {
            request.DueDate = DueDate;
        }

        return request;
    }
}
