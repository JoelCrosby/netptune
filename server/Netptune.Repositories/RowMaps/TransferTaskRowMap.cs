using Netptune.Core.Enums;
using Netptune.Transfer.Repositories;

namespace Netptune.Repositories.RowMaps;

// Dapper matches column names case-insensitively but does not bridge underscores, so the property
// names here mirror get_transfer_tasks.sql column for column. Mapping straight onto TransferTaskRow's
// PascalCase properties silently left every snake_case column null.
public sealed class TransferTaskRowMap
{
    public int Id { get; init; }

    public long Total_Count { get; init; }

    public string? Project_Key { get; init; }

    public int Project_Scope_Id { get; init; }

    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public string? Status_Key { get; init; }

    public TaskPriority? Priority { get; init; }

    public EstimateType? Estimate_Type { get; init; }

    public decimal? Estimate_Value { get; init; }

    public DateOnly? Start_Date { get; init; }

    public DateOnly? Due_Date { get; init; }

    public string? Sprint_Name { get; init; }

    public string? Sprint_Project_Key { get; init; }

    public string? Board_Identifier { get; init; }

    public string? Board_Group_Name { get; init; }

    public string? Created_By_Email { get; init; }

    public DateTime Created_At { get; init; }

    public DateTime? Updated_At { get; init; }

    public string[] Assignee_Emails { get; init; } = [];

    public string[] Tag_Names { get; init; } = [];

    public TransferTaskRow ToRow()
    {
        return new TransferTaskRow
        {
            Id = Id,
            TotalCount = Total_Count,
            ProjectKey = Project_Key,
            ProjectScopeId = Project_Scope_Id,
            Name = Name,
            Description = Description,
            StatusKey = Status_Key,
            Priority = Priority,
            EstimateType = Estimate_Type,
            EstimateValue = Estimate_Value,
            StartDate = Start_Date,
            DueDate = Due_Date,
            SprintName = Sprint_Name,
            SprintProjectKey = Sprint_Project_Key,
            BoardIdentifier = Board_Identifier,
            BoardGroupName = Board_Group_Name,
            CreatedByEmail = Created_By_Email,
            CreatedAt = Created_At,
            UpdatedAt = Updated_At,
            AssigneeEmails = Assignee_Emails,
            TagNames = Tag_Names,
        };
    }
}
