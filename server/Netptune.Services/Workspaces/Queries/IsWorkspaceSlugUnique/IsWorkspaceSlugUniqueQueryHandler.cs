using Mediator;
using Netptune.Core.Encoding;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Workspaces.Queries;

public sealed class IsWorkspaceSlugUniqueQueryHandler : IRequestHandler<IsWorkspaceSlugUniqueQuery, ClientResponse<IsSlugUniqueResponse>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public IsWorkspaceSlugUniqueQueryHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async ValueTask<ClientResponse<IsSlugUniqueResponse>> Handle(IsWorkspaceSlugUniqueQuery request, CancellationToken cancellationToken)
    {
        var slugLower = request.Slug.ToUrlSlug();
        var exists = await UnitOfWork.Workspaces.Exists(slugLower);

        return ClientResponse<IsSlugUniqueResponse>.Success(new IsSlugUniqueResponse
        {
            Request = request.Slug,
            Slug = slugLower,
            IsUnique = !exists,
        });
    }
}
