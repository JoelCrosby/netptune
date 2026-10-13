using Mediator;

using Netptune.Core.Exceptions;
using Netptune.Core.Models.ProjectTasks;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Comments.Queries;

public sealed record FindTaskCommentsByTextQuery(IReadOnlyCollection<int> TaskIds, TextPattern Pattern, int Take)
    : IRequest<ClientResponse<TaskCommentMatches>>;

public sealed class FindTaskCommentsByTextQueryHandler : IRequestHandler<FindTaskCommentsByTextQuery, ClientResponse<TaskCommentMatches>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public FindTaskCommentsByTextQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<TaskCommentMatches>> Handle(FindTaskCommentsByTextQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();

        try
        {
            var matches = await UnitOfWork.Comments.FindTaskCommentsByText(
                workspaceId,
                request.TaskIds,
                request.Pattern,
                request.Take,
                cancellationToken);

            return ClientResponse<TaskCommentMatches>.Success(matches);
        }
        catch (InvalidTextPatternException exception)
        {
            return ClientResponse<TaskCommentMatches>.Failed(exception.Message);
        }
    }
}
