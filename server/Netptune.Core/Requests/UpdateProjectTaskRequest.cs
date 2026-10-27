using System.Text.Json.Serialization;

using Netptune.Core.Enums;

namespace Netptune.Core.Requests;

public record UpdateProjectTaskRequest
{
    private DateOnly? startDate;

    private DateOnly? dueDate;

    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public int? StatusId { get; set; }

    public double? SortOrder { get; set; }

    public string? OwnerId { get; set; }

    public List<string>? AssigneeIds { get; set; }

    public List<string>? Tags { get; set; }

    public TaskPriority? Priority { get; set; }

    public EstimateType? EstimateType { get; set; }

    public decimal? EstimateValue { get; set; }

    // A null priority or estimate means "leave it alone", so taking one away has to be asked for. Server
    // callers only: an undo that puts back a task that had none.
    [JsonIgnore]
    public bool ClearPriority { get; init; }

    [JsonIgnore]
    public bool ClearEstimate { get; init; }

    public DateOnly? StartDate
    {
        get => startDate;
        set
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
        set
        {
            dueDate = value;
            DueDateSpecified = true;
        }
    }

    [JsonIgnore]
    public bool DueDateSpecified { get; private set; }
}
