using System;

using Netptune.Core.Enums;

namespace Netptune.Repositories.RowMaps;

public class BoardViewModelRowMap
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Identifier { get; set; } = null!;

    public int Project_Id { get; set; }

    public BoardType Board_Type { get; set; }

    public DateTime Created_At { get; set; }

    public DateTimeOffset? Updated_At { get; set; }

    public string? Meta_Info { get; set; }

    public string Firstname { get; set; } = null!;

    public string Lastname { get; set; } = null!;

    public string Project_Name { get; set; } = null!;
}
