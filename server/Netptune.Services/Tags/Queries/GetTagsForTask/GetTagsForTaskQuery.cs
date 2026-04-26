using Mediator;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Services.Tags.Queries;

public sealed record GetTagsForTaskQuery(string SystemId) : IRequest<List<TagViewModel>?>;
