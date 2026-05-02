using Mediator;
using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Tasks.Commands;

public sealed record DeleteTaskCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IActivityLogger Activity;

    public DeleteTaskCommandHandler(INetptuneUnitOfWork unitOfWork, IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await UnitOfWork.Tasks.GetAsync(request.Id, cancellationToken: cancellationToken);

        if (task is null) return ClientResponse.NotFound;

        await UnitOfWork.Tasks.DeletePermanent(task.Id, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        Activity.Log(options =>
        {
            options.EntityId = task.Id;
            options.EntityType = EntityType.Task;
            options.Type = ActivityType.Delete;
        });

        return ClientResponse.Success;
    }
}
