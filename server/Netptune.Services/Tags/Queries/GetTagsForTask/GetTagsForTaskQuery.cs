using Mediator;

using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services.Tags.Queries.GetTagsForTask;

public sealed record GetTagsForTaskQuery(string SystemId) : IRequest<List<TagViewModel>?>;
