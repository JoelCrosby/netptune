using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Projects;

namespace Netptune.Services.Projects.Commands;

public sealed record CreateProjectCommand(AddProjectRequest Request) : IRequest<ClientResponse<ProjectViewModel>>;
