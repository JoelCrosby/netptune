using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Boards;

public class BoardGroupViewModel
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int BoardId { get; set; }

    public BoardGroupType Type { get; set; }

    public double SortOrder { get; set; }
}