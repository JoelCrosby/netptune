using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public interface IAiReviewer
{
    IAsyncEnumerable<AiReviewStreamEvent> Review(AiReviewSubject subject, CancellationToken cancellationToken);
}
