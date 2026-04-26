using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Tags.Commands;

public sealed record DeleteTagsCommand(DeleteTagsRequest Request) : IRequest<ClientResponse>;
