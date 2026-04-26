using Mediator;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Services.Comments.Queries;

public sealed record GetCommentsForTaskQuery(string SystemId) : IRequest<List<CommentViewModel>?>;
