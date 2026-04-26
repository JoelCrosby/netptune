using Mediator;
using Netptune.Core.Responses.Common;

namespace Netptune.Services.Comments.Commands;

public sealed record DeleteCommentCommand(int Id) : IRequest<ClientResponse>;
