using Mediator;
using Netptune.Core.Encoding;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Boards.Queries;

public sealed record IsBoardIdentifierUniqueQuery(string Identifier) : IRequest<ClientResponse<IsSlugUniqueResponse>>;

public sealed class IsBoardIdentifierUniqueQueryHandler : IRequestHandler<IsBoardIdentifierUniqueQuery, ClientResponse<IsSlugUniqueResponse>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public IsBoardIdentifierUniqueQueryHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public async ValueTask<ClientResponse<IsSlugUniqueResponse>> Handle(IsBoardIdentifierUniqueQuery request, CancellationToken cancellationToken)
    {
        var slugLower = request.Identifier.ToUrlSlug();
        var exists = await UnitOfWork.Boards.Exists(slugLower);

        return ClientResponse<IsSlugUniqueResponse>.Success(new IsSlugUniqueResponse
        {
            Request = request.Identifier,
            Slug = slugLower,
            IsUnique = !exists,
        });
    }
}
