using System.Collections.Generic;

using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Models.Repository
{
    public class PagedResult<T> : IPagedResult<T>
    {
        public IList<T> Results { get; set; }

        public int CurrentPage { get; set; }

        public int PageCount { get; set; }

        public int PageSize { get; set; }

        public int RowCount { get; set; }
    }
}
