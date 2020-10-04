using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Models.Repository
{
    public class PageQuery : IPageQuery
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public string Query { get; set; }

        public string Sort { get; set; }

        public bool SortDescending { get; set; }

        public static PageQuery Default => new PageQuery
        {
            PageSize = 30,
            Page = 0
        };
    }
}
