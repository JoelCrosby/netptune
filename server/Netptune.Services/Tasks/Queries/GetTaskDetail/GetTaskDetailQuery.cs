using Mediator;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Services.Tasks.Queries;

public sealed record GetTaskDetailQuery(string SystemId) : IRequest<TaskViewModel?>;
