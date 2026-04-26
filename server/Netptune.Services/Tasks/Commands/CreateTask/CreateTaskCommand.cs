using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Services.Tasks.Commands;

public sealed record CreateTaskCommand(AddProjectTaskRequest Request) : IRequest<ClientResponse<TaskViewModel>>;
