using Mediator;

using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Tags.Commands.DeleteTagFromTask;

public sealed record DeleteTagFromTaskCommand(DeleteTagFromTaskRequest Request) : IRequest<ClientResponse>;
