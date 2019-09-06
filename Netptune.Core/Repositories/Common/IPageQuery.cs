namespace Netptune.Core.Repositories.Common
{
    public interface IPageQuery
    {
        int Page { get; }

        int PageSize { get; }

        string Query { get; }

        string Sort { get; set; }

        bool SortDescending { get; set; }
    }
}
