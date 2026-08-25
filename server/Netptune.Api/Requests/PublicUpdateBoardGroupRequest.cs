using Netptune.Core.Requests;

namespace Netptune.Api.Requests;

public sealed record PublicUpdateBoardGroupRequest
{
    public string? Name { get; init; }

    public double? SortOrder { get; init; }

    public int? StatusId { get; init; }

    public bool ClearStatus { get; init; }

    public UpdateBoardGroupRequest ToRequest(int id)
    {
        return new UpdateBoardGroupRequest
        {
            BoardGroupId = id,
            Name = Name,
            SortOrder = SortOrder,
            StatusId = StatusId,
            ClearStatus = ClearStatus,
        };
    }
}
