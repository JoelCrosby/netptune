using Mediator;

using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Services.Tasks.Queries.GetTaskDetail;

public sealed record GetTaskDetailQuery(string SystemId) : IRequest<TaskViewModel?>;
