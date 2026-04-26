using Mediator;

using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services.Tags.Queries.GetTagsForWorkspace;

public sealed record GetTagsForWorkspaceQuery : IRequest<List<TagViewModel>?>;
