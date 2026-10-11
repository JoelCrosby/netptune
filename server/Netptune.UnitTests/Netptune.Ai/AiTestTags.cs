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

        mediator
            .Send(Arg.Any<ResolveTagIdsByNameQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Resolve(call.Arg<ResolveTagIdsByNameQuery>().Names, tags));
    }

    private static TagNameResolution Resolve(IReadOnlyCollection<string?> names, IReadOnlyList<TagViewModel> tags)
    {
        var idsByName = tags.ToDictionary(tag => tag.Name, tag => tag.Id, StringComparer.OrdinalIgnoreCase);
        var requestedNames = names.Select(name => name!).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var unknownNames = requestedNames.Where(name => !idsByName.ContainsKey(name)).ToList();

        if (unknownNames.Count > 0)
        {
            var error = ResolveTagIdsByNameQueryHandler.UnknownNamesMessage(unknownNames);

            return new TagNameResolution { UnknownNames = unknownNames, Error = error };
        }

        var ids = requestedNames.Select(name => idsByName[name]).ToList();

        return new TagNameResolution { Ids = ids };
    }
}
