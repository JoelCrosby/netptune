using Netptune.Core.Enums;

// ReSharper disable InconsistentNaming

namespace Netptune.Repositories.RowMaps;

public sealed class RoadmapSprintRowMap
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public DateTime Start_Date { get; init; }

    public DateTime End_Date { get; init; }

    public SprintStatus Status { get; init; }

    public int Project_Id { get; init; }
}
