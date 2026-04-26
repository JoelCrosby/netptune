using Mediator;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Services.Tasks.Queries;

public sealed class GetTaskQueryHandler : IRequestHandler<GetTaskQuery, TaskViewModel?>
{
    private readonly INetptuneUnitOfWork UnitOfWork;

    public GetTaskQueryHandler(INetptuneUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
    }

    public ValueTask<TaskViewModel?> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        return new ValueTask<TaskViewModel?>(UnitOfWork.Tasks.GetTaskViewModel(request.Id));
    }
}
