using System.Collections.Generic;

namespace Netptune.Core.Requests
{
    public class DeleteTagsRequest
    {
        public string Workspace { get; set; }

        public List<string> Tags { get; set; }
    }
}
