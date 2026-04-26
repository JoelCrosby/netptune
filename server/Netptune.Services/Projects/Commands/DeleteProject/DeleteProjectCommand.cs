using Mediator;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Projects.Commands;

public sealed record DeleteProjectCommand(int Id) : IRequest<ClientResponse>;
