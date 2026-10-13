using Netptune.Core.Models.Ai;
using Netptune.Core.Requests.Ai;

namespace Netptune.Core.Services.Ai;

public interface IAiInlineWriter
{
    IAsyncEnumerable<AiStreamEvent> WriteTaskDescription(string systemId, InlineWriteRequest request, CancellationToken cancellationToken);

    IAsyncEnumerable<AiStreamEvent> WritePage(int pageId, InlineWriteRequest request, CancellationToken cancellationToken);
}
