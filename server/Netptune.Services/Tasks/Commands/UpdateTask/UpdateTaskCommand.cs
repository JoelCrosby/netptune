using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Services.Tasks.Commands;

public sealed record UpdateTaskCommand(UpdateProjectTaskRequest Request) : IRequest<ClientResponse<TaskViewModel>>;
