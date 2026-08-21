using Netptune.Core.Meta;
using Netptune.Core.Requests;

namespace Netptune.PublicApi.Requests;

public sealed record PublicUpdateBoardRequest
{
    public string? Name { get; init; }

    public string? Identifier { get; init; }

    public BoardMeta? Meta { get; init; }

    public UpdateBoardRequest ToRequest(int id)
    {
        return new UpdateBoardRequest
        {
            Id = id,
            Name = Name,
            Identifier = Identifier,
            Meta = Meta,
        };
    }
}
