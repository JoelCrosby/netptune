using Mediator;

using Netptune.Core.ViewModels.Tags;
using Netptune.Handlers.Tags.Queries;

using NSubstitute;

namespace Netptune.UnitTests.Netptune.Ai;

public static class AiTestTags
{
    public static void Given(IMediator mediator, IReadOnlyList<TagViewModel> tags)
    {
        mediator
            .Send(Arg.Any<GetTagsByNameQuery>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var names = call.Arg<GetTagsByNameQuery>().Names.ToHashSet(StringComparer.OrdinalIgnoreCase);

                return tags.Where(tag => names.Contains(tag.Name)).ToList();
            });
    }
}
