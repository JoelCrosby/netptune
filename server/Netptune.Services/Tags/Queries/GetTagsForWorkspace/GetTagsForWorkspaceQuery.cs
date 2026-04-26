using Mediator;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services.Tags.Queries;

public sealed record GetTagsForWorkspaceQuery : IRequest<List<TagViewModel>?>;
