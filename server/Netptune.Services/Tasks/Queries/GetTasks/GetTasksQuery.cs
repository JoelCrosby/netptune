using Mediator;

using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Services.Tasks.Queries.GetTasks;

public sealed record GetTasksQuery : IRequest<List<TaskViewModel>>;
