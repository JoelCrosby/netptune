using System.Collections.Generic;

namespace Netptune.Core.Requests;

public class BoardGroupsFilter
{
    public List<string> Users { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public bool Flagged { get; set; }

    public string? Term { get; set; }
}
