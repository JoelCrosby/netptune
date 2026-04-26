using Mediator;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Services.Comments.Commands;

public sealed record AddCommentToTaskCommand(AddCommentRequest Request) : IRequest<ClientResponse<CommentViewModel>>;
