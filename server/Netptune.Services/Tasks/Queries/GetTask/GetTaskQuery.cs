using Mediator;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Services.Tasks.Queries;

public sealed record GetTaskQuery(int Id) : IRequest<TaskViewModel?>;
