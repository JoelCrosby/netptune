using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Query.Views;

namespace Netptune.Handlers.TaskViews.Queries;

public sealed record PreviewTaskQueryQuery(TaskViewQueryRequest Request) : IRequest<ClientResponse<TaskViewResultViewModel>>;

public sealed class PreviewTaskQueryQueryHandler : IRequestHandler<PreviewTaskQueryQuery, ClientResponse<TaskViewResultViewModel>>
{
    private readonly TaskViewQueryRunner Runner;

    public PreviewTaskQueryQueryHandler(TaskViewQueryRunner runner)
    {
        Runner = runner;
    }

    public async ValueTask<ClientResponse<TaskViewResultViewModel>> Handle(PreviewTaskQueryQuery request, CancellationToken cancellationToken)
    {
        var result = await Runner.Run(request.Request, cancellationToken);

        return result;
    }
}
