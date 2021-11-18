using System;

using Netptune.Core.Enums;

namespace Netptune.Repositories.RowMaps;

public class BoardViewModelRowMap
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Identifier { get; set; }

    public int Project_Id { get; set; }

    public BoardType Board_Type { get; set; }

    public DateTime Created_At { get; set; }

    public DateTimeOffset? Updated_At { get; set; }

    public string Meta_Info { get; set; }

    public string Firstname { get; set; }

    public string Lastname { get; set; }

    public string Project_Name { get; set; }
}