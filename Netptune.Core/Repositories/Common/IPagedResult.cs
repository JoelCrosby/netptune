using System.Collections.Generic;

namespace Netptune.Core.Repositories.Common
{
    public interface IPagedResult<T>
    {
        int CurrentPage { get; set; }

        int PageCount { get; set; }

        int PageSize { get; set; }

        int RowCount { get; set; }

        IList<T> Results { get; set; }
    }
}
