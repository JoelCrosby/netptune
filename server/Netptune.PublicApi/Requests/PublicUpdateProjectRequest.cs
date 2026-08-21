using Netptune.Core.Requests;

namespace Netptune.PublicApi.Requests;

public sealed record PublicUpdateProjectRequest
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? RepositoryUrl { get; init; }

    public string? Key { get; init; }

    public int? DefaultStatusId { get; init; }

    public UpdateProjectRequest ToRequest(int id)
    {
        return new UpdateProjectRequest
        {
            Id = id,
            Name = Name,
            Description = Description,
            RepositoryUrl = RepositoryUrl,
            Key = Key,
            DefaultStatusId = DefaultStatusId,
        };
    }
}
