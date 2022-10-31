using System.Collections.Generic;

namespace Netptune.Core.Requests;

public record DeleteTagsRequest
{
    public List<string> Tags { get; set; } = null!;
}
