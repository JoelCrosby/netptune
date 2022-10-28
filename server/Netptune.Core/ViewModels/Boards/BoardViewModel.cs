using System;
using System.Collections.Generic;

using Netptune.Core.Enums;
using Netptune.Core.Meta;

namespace Netptune.Core.ViewModels.Boards;

public class BoardViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Identifier { get; set; } = null!;

    public string ProjectName { get; set; } = null!;

    public int ProjectId { get; set; }

    public BoardType BoardType { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string OwnerUsername { get; set; } = null!;

    public BoardMeta? MetaInfo { get; set; }
}

public class BoardsViewModel
{
    public int ProjectId { get; set; }

    public string ProjectName { get; set; } = null!;

    public List<BoardViewModel> Boards { get; set; } = null!;
}
