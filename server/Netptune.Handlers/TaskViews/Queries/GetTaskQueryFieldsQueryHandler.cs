using Mediator;

using Netptune.Core.Constants;
using Netptune.Query.Schema;
using Netptune.Query.Tasks;
using Netptune.Query.ViewModels;

namespace Netptune.Handlers.TaskViews.Queries;

public sealed record GetQueryFieldsQuery : IRequest<QueryCatalogViewModel>;

public sealed class GetQueryFieldsQueryHandler : IRequestHandler<GetQueryFieldsQuery, QueryCatalogViewModel>
{
    public ValueTask<QueryCatalogViewModel> Handle(GetQueryFieldsQuery request, CancellationToken cancellationToken)
    {
        var fields = TaskFieldCatalog.Instance.Fields.Select(ToViewModel).ToList();
        var catalog = new QueryCatalogViewModel
        {
            Fields = fields,
            MaximumDepth = ConditionGroupLimits.MaximumDepth,
            MaximumConditionCount = ConditionGroupLimits.MaximumConditionCount,
        };

        return ValueTask.FromResult(catalog);
    }

    private static QueryFieldViewModel ToViewModel(QueryField field)
    {
        return new QueryFieldViewModel
        {
            Key = field.Key,
            Name = field.Name,
            ValueType = field.ValueType,
            Operators = field.Operators,
            OptionSource = field.OptionSource,
            IsMultiValued = field.IsMultiValued,
            IsSortable = field.IsSortable,
            SortKey = field.SortKey,
        };
    }
}
