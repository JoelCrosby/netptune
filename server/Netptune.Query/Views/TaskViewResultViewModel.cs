using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Query.Validation;

namespace Netptune.Query.Views;

// Shaped as a page rather than wrapping one so the client datatable, which reads items and
// totals straight off the response payload, can bind a view without a bespoke adapter.
public sealed record TaskViewResultViewModel
{
    public IReadOnlyList<TaskViewModel> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public IReadOnlyList<QueryValidationError> Errors { get; init; } = [];

    public static TaskViewResultViewModel FromPage(PagedResponse<TaskViewModel> page)
    {
        return new TaskViewResultViewModel
        {
            Items = page.Items,
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
        };
    }
}
