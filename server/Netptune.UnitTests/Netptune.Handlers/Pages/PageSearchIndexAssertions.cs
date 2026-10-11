using Netptune.Core.Models.Search;
using Netptune.Core.Services.Activity;

using NSubstitute;

namespace Netptune.UnitTests.Netptune.Handlers.Pages;

public static class PageSearchIndexAssertions
{
    public const string WorkspaceSlug = "workspace";

    // Every page command test signs in to the same workspace key, which is the slug the events carry.
    public static Task ReceivedPageIndexEvent(this IEventPublisher publisher, SearchIndexOperation operation, params int[] pageIds)
    {
        return publisher.Received(1).Dispatch(Arg.Is<SearchIndexEvent>(message =>
            message.Operation == operation
            && message.EntityType == SearchIndexEntityTypes.Page
            && message.EntityIds.SequenceEqual(pageIds)
            && message.WorkspaceSlug == WorkspaceSlug));
    }

    public static Task DidNotReceivePageIndexEvent(this IEventPublisher publisher)
    {
        return publisher.DidNotReceive().Dispatch(Arg.Any<SearchIndexEvent>());
    }
}
